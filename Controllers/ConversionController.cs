using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HwpToPdf.Models;
using HwpToPdf.Services;
using HwpToPdf.Utils;

namespace HwpToPdf.Controllers
{
    /// <summary>
    /// 변환 작업을 제어하는 컨트롤러
    /// </summary>
    public class ConversionController
    {
        private readonly ConversionQueueService _queueService;
        private readonly IHwpService _hwpService;
        private readonly IPdfPostProcessingService _pdfService;

        public ConversionController(
            ConversionQueueService queueService,
            IHwpService hwpService,
            IPdfPostProcessingService pdfService)
        {
            _queueService = queueService;
            _hwpService = hwpService;
            _pdfService = pdfService;
            
            // 이벤트 핸들러 등록
            _queueService.JobStarted += QueueService_JobStarted;
            _queueService.JobCompleted += QueueService_JobCompleted;
            _queueService.JobProgress += QueueService_JobProgress;
            _queueService.ProcessingError += QueueService_ProcessingError;
        }

        public event EventHandler<ConversionJob> JobStatusChanged;
        public event EventHandler<string> LogMessage;

        public async Task<Guid> StartSingleConversionAsync(string filePath, ConversionOptions options)
        {
            try
            {
                // 입력 유효성 검사
                ValidateInput(filePath, options);
                
                Log($"단일 파일 변환 시작: {filePath}");
                
                // 작업 생성
                var job = ConversionJob.CreateSingleJob(filePath, options);
                
                if (job.Status == JobStatus.Failed)
                {
                    Log($"작업 생성 실패: {filePath}");
                    throw new InvalidOperationException("작업을 생성할 수 없습니다.");
                }
                
                // 작업 등록
                await _queueService.EnqueueJobAsync(job);
                
                // 큐 처리 시작
                await _queueService.StartProcessingAsync();
                
                return job.JobId;
            }
            catch (Exception ex)
            {
                Log($"변환 시작 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<Guid> StartBatchConversionAsync(List<string> filePaths, ConversionOptions options)
        {
            try
            {
                if (filePaths == null || filePaths.Count == 0)
                    throw new ArgumentException("변환할 파일이 지정되지 않았습니다.");
                    
                if (options == null)
                    options = new ConversionOptions();
                    
                // 옵션 유효성 검사
                if (!options.Validate())
                    throw new ArgumentException("유효하지 않은 변환 옵션입니다.");
                    
                Log($"배치 변환 시작: 파일 {filePaths.Count}개");
                
                // 작업 생성
                var job = ConversionJob.CreateBatchJob(filePaths, options);
                
                if (job.TotalFiles == 0)
                {
                    Log("유효한 HWP 파일이 없습니다.");
                    throw new InvalidOperationException("유효한 HWP 파일이 없습니다.");
                }
                
                // 작업 등록
                await _queueService.EnqueueJobAsync(job);
                
                // 큐 처리 시작
                await _queueService.StartProcessingAsync();
                
                return job.JobId;
            }
            catch (Exception ex)
            {
                Log($"배치 변환 시작 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<Guid> StartFolderConversionAsync(string folderPath, string searchPattern, bool recursive, ConversionOptions options)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("폴더 경로가 지정되지 않았습니다.");
                    
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"지정된 폴더를 찾을 수 없습니다: {folderPath}");
                    
                if (options == null)
                    options = new ConversionOptions();
                    
                // 옵션 유효성 검사
                if (!options.Validate())
                    throw new ArgumentException("유효하지 않은 변환 옵션입니다.");
                
                // 재귀 검색 옵션 설정
                options.RecursiveSearch = recursive;
                
                Log($"폴더 변환 시작: {folderPath} (재귀: {recursive})");
                
                // 파일 목록 가져오기
                var files = FileHelper.GetHwpFiles(folderPath, recursive);
                
                if (files.Count == 0)
                {
                    Log($"폴더에 HWP 파일이 없습니다: {folderPath}");
                    throw new InvalidOperationException($"폴더에 HWP 파일이 없습니다: {folderPath}");
                }
                
                // 작업 생성
                var job = ConversionJob.CreateBatchJob(files, options);
                
                // 작업 등록
                await _queueService.EnqueueJobAsync(job);
                
                // 큐 처리 시작
                await _queueService.StartProcessingAsync();
                
                return job.JobId;
            }
            catch (Exception ex)
            {
                Log($"폴더 변환 시작 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CancelConversionAsync(Guid jobId)
        {
            try
            {
                Log($"작업 취소 요청: {jobId}");
                return await _queueService.CancelJobAsync(jobId);
            }
            catch (Exception ex)
            {
                Log($"작업 취소 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        public async Task<ConversionJob> GetJobStatusAsync(Guid jobId)
        {
            try
            {
                return await _queueService.GetJobStatusAsync(jobId);
            }
            catch (Exception ex)
            {
                Log($"작업 상태 조회 중 오류 발생: {ex.Message}");
                return null;
            }
        }

        private void ValidateInput(string path, ConversionOptions options)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("입력 경로가 지정되지 않았습니다.");
                
            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".hwp" && extension != ".hwpx")
                    throw new ArgumentException("지원되지 않는 파일 형식입니다. HWP 또는 HWPX 파일만 허용됩니다.");
            }
            else if (!Directory.Exists(path) && !path.Contains("*") && !path.Contains("?"))
            {
                throw new FileNotFoundException("지정된 파일 또는 디렉토리를 찾을 수 없습니다.", path);
            }
            
            if (options == null)
                throw new ArgumentNullException(nameof(options));
                
            if (!options.Validate())
                throw new ArgumentException("유효하지 않은 변환 옵션입니다.");
        }
        
        #region 큐 서비스 이벤트 핸들러
        
        private void QueueService_JobStarted(object sender, ConversionJob e)
        {
            OnJobStatusChanged(e);
            Log($"작업 시작: {e.JobId}, 파일 수: {e.TotalFiles}");
        }
        
        private void QueueService_JobCompleted(object sender, ConversionJob e)
        {
            OnJobStatusChanged(e);
            
            string status = e.Status switch
            {
                JobStatus.Completed => "완료",
                JobStatus.Failed => "실패",
                JobStatus.Cancelled => "취소",
                _ => e.Status.ToString()
            };
            
            Log($"작업 {status}: {e.JobId}, 성공: {e.CompletedFiles}/{e.TotalFiles}, 실패: {e.FailedFiles}/{e.TotalFiles}");
        }
        
        private void QueueService_JobProgress(object sender, ConversionJob e)
        {
            OnJobStatusChanged(e);
            Log($"작업 진행 중: {e.JobId}, 진행률: {e.GetProgress():P0}");
        }
        
        private void QueueService_ProcessingError(object sender, Exception e)
        {
            Log($"처리 오류: {e.Message}");
        }
        
        #endregion

        private void OnJobStatusChanged(ConversionJob job)
        {
            JobStatusChanged?.Invoke(this, job);
        }

        private void Log(string message)
        {
            LogHelper.LogInfo(message);
            LogMessage?.Invoke(this, message);
        }
    }
} 