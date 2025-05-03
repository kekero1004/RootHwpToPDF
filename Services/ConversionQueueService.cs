using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HwpToPdf.Models;
using HwpToPdf.Utils;

namespace HwpToPdf.Services
{
    /// <summary>
    /// 변환 작업 큐 서비스
    /// </summary>
    public class ConversionQueueService
    {
        private readonly ConcurrentQueue<ConversionJob> _jobQueue;
        private readonly SemaphoreSlim _semaphore;
        private readonly IHwpService _hwpService;
        private readonly IPdfPostProcessingService _pdfService;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly int _maxConcurrentJobs;
        private readonly ConcurrentDictionary<Guid, ConversionJob> _activeJobs;
        private bool _isProcessing;

        public ConversionQueueService(IHwpService hwpService, IPdfPostProcessingService pdfService, int maxConcurrentJobs = 1)
        {
            _jobQueue = new ConcurrentQueue<ConversionJob>();
            _semaphore = new SemaphoreSlim(maxConcurrentJobs);
            _hwpService = hwpService;
            _pdfService = pdfService;
            _maxConcurrentJobs = maxConcurrentJobs;
            _cancellationTokenSource = new CancellationTokenSource();
            _activeJobs = new ConcurrentDictionary<Guid, ConversionJob>();
            _isProcessing = false;
        }

        public event EventHandler<ConversionJob> JobCompleted;
        public event EventHandler<ConversionJob> JobStarted;
        public event EventHandler<ConversionJob> JobProgress;
        public event EventHandler<Exception> ProcessingError;

        public async Task<Guid> EnqueueJobAsync(ConversionJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
                
            // 작업이 이미 큐에 있는지 확인
            if (_activeJobs.ContainsKey(job.JobId))
                return job.JobId;
                
            // 작업 등록
            _jobQueue.Enqueue(job);
            _activeJobs[job.JobId] = job;
            
            // 처리 시작 (아직 시작하지 않았다면)
            if (!_isProcessing)
                await StartProcessingAsync();
                
            return job.JobId;
        }

        public Task StartProcessingAsync()
        {
            if (_isProcessing)
                return Task.CompletedTask;
                
            _isProcessing = true;
            
            try
            {
                // 작업 처리 시작
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessQueueAsync(_cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("큐 처리 중 오류 발생", ex);
                        ProcessingError?.Invoke(this, ex);
                    }
                });
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _isProcessing = false;
                LogHelper.LogError("큐 처리 시작 중 오류 발생", ex);
                ProcessingError?.Invoke(this, ex);
                return Task.FromException(ex);
            }
        }

        public Task StopProcessingAsync()
        {
            if (!_isProcessing)
                return Task.CompletedTask;
                
            try
            {
                // 취소 요청
                _cancellationTokenSource.Cancel();
                
                // 새 CancellationTokenSource 생성
                _cancellationTokenSource = new CancellationTokenSource();
                
                // 모든 작업 취소
                foreach (var job in _activeJobs.Values)
                {
                    if (job.Status == JobStatus.InProgress || job.Status == JobStatus.Pending)
                    {
                        job.Cancel();
                        JobCompleted?.Invoke(this, job);
                    }
                }
                
                _isProcessing = false;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("큐 처리 중지 중 오류 발생", ex);
                ProcessingError?.Invoke(this, ex);
                return Task.FromException(ex);
            }
        }

        public Task<ConversionJob> GetJobStatusAsync(Guid jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var job))
                return Task.FromResult(job);
                
            return Task.FromResult<ConversionJob>(null);
        }

        public Task<bool> CancelJobAsync(Guid jobId)
        {
            if (!_activeJobs.TryGetValue(jobId, out var job))
                return Task.FromResult(false);
                
            // 진행 중인 작업은 취소 마킹만 하고 실제로는 완료될 때까지 기다림
            if (job.Status == JobStatus.InProgress)
            {
                job.Cancel();
                return Task.FromResult(true);
            }
            
            // 대기 중인 작업은 큐에서 제거
            if (job.Status == JobStatus.Pending)
            {
                job.Cancel();
                _activeJobs.TryRemove(jobId, out _);
                JobCompleted?.Invoke(this, job);
                return Task.FromResult(true);
            }
            
            // 이미 완료되거나 실패한 작업은 취소 불가
            return Task.FromResult(false);
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            // 최대 동시 처리 작업 수만큼 병렬 처리
            var tasks = new List<Task>();
            
            for (int i = 0; i < _maxConcurrentJobs; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // 큐에서 작업 가져오기
                        if (_jobQueue.TryDequeue(out var job))
                        {
                            await ProcessJobAsync(job, cancellationToken);
                        }
                        else
                        {
                            // 큐가 비어 있으면 잠시 대기
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                }, cancellationToken));
            }
            
            try
            {
                // 모든 태스크 완료 대기
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소는 무시
            }
            catch (Exception ex)
            {
                LogHelper.LogError("작업 처리 스레드 오류", ex);
                ProcessingError?.Invoke(this, ex);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ProcessJobAsync(ConversionJob job, CancellationToken token)
        {
            if (job == null || token.IsCancellationRequested)
                return;
                
            try
            {
                // 작업 시작
                job.Start();
                JobStarted?.Invoke(this, job);
                
                // 순차적으로 파일 처리
                foreach (var document in job.Documents)
                {
                    // 취소 확인
                    if (token.IsCancellationRequested || job.Status == JobStatus.Cancelled)
                    {
                        job.Cancel();
                        break;
                    }
                    
                    try
                    {
                        // 현재 처리 중인 파일 설정
                        job.SetCurrentFile(document.FilePath);
                        JobProgress?.Invoke(this, job);
                        
                        // 문서 검증
                        if (!await _hwpService.ValidateDocumentAsync(document))
                        {
                            var failedOutput = new PdfOutput(Path.Combine(job.Options.OutputDirectory, Path.GetFileNameWithoutExtension(document.FileName) + ".pdf"));
                            failedOutput.SetFailed("유효하지 않은 문서입니다.", TimeSpan.Zero);
                            job.FileCompleted(failedOutput);
                            JobProgress?.Invoke(this, job);
                            continue;
                        }
                        
                        // 한글에서 PDF로 변환
                        var pdfOutput = await _hwpService.ConvertToPdfAsync(document, job.Options);
                        
                        // 변환 성공 시 후처리
                        if (pdfOutput.Success)
                        {
                            // 워터마크 추가 (텍스트)
                            if (!string.IsNullOrEmpty(job.Options.Watermark))
                            {
                                pdfOutput = await _pdfService.AddWatermarkAsync(pdfOutput, job.Options.Watermark);
                            }
                            
                            // 워터마크 추가 (이미지)
                            if (!string.IsNullOrEmpty(job.Options.WatermarkImagePath) && File.Exists(job.Options.WatermarkImagePath))
                            {
                                pdfOutput = await _pdfService.AddImageWatermarkAsync(pdfOutput, job.Options.WatermarkImagePath);
                            }
                            
                            // 페이지 범위 처리
                            if (!string.IsNullOrEmpty(job.Options.PageRange))
                            {
                                pdfOutput = await _pdfService.ExtractPagesAsync(pdfOutput, job.Options.PageRange);
                            }
                            
                            // 압축 처리
                            if (job.Options.CompressionLevel > 0)
                            {
                                pdfOutput = await _pdfService.CompressPdfAsync(pdfOutput, job.Options.CompressionLevel);
                            }
                        }
                        
                        // 결과 추가
                        job.FileCompleted(pdfOutput);
                        
                        // 진행 상황 이벤트 발생
                        JobProgress?.Invoke(this, job);
                        
                        // 문서 닫기
                        await _hwpService.CloseDocumentAsync(document);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError($"파일 처리 중 오류 발생: {document.FilePath}", ex);
                        
                        // 변환 실패 처리
                        var failedOutput = new PdfOutput(Path.Combine(job.Options.OutputDirectory, Path.GetFileNameWithoutExtension(document.FileName) + ".pdf"));
                        failedOutput.SetFailed(ex.Message, TimeSpan.Zero);
                        job.FileCompleted(failedOutput);
                        
                        // 진행 상황 이벤트 발생
                        JobProgress?.Invoke(this, job);
                    }
                }
                
                // 작업 완료
                job.Complete();
                
                // 완료 이벤트 발생
                JobCompleted?.Invoke(this, job);
                
                // 활성 작업 목록에서 제거 (1시간 후)
                _ = Task.Delay(TimeSpan.FromHours(1)).ContinueWith(t => {
                    _activeJobs.TryRemove(job.JobId, out _);
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"작업 처리 중 오류 발생: {job.JobId}", ex);
                job.Status = JobStatus.Failed;
                job.EndTime = DateTime.Now;
                
                // 완료 이벤트 발생
                JobCompleted?.Invoke(this, job);
                
                // 오류 이벤트 발생
                ProcessingError?.Invoke(this, ex);
            }
        }
    }
} 