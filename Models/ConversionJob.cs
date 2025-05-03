using System;
using System.Collections.Generic;
using System.Linq;

namespace HwpToPdf.Models
{
    /// <summary>
    /// 변환 작업 정보를 담는 모델 클래스
    /// </summary>
    public class ConversionJob
    {
        public Guid JobId { get; private set; }
        public List<HwpDocument> Documents { get; set; }
        public ConversionOptions Options { get; set; }
        public List<PdfOutput> Results { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public JobStatus Status { get; set; }
        public int TotalFiles { get; set; }
        public int CompletedFiles { get; set; }
        public int FailedFiles { get; set; }
        public string CurrentFile { get; set; } // 현재 처리 중인 파일
        public List<ProcessedFile> ProcessedFiles { get; set; } // 처리된 파일 목록

        public ConversionJob()
        {
            JobId = Guid.NewGuid();
            Documents = new List<HwpDocument>();
            Results = new List<PdfOutput>();
            Options = new ConversionOptions();
            Status = JobStatus.Pending;
            StartTime = DateTime.Now;
            ProcessedFiles = new List<ProcessedFile>();
        }
        
        /// <summary>
        /// 단일 파일 변환 작업 생성
        /// </summary>
        public static ConversionJob CreateSingleJob(string filePath, ConversionOptions options)
        {
            var job = new ConversionJob
            {
                Options = options ?? new ConversionOptions()
            };
            
            try
            {
                var document = new HwpDocument(filePath);
                job.Documents.Add(document);
                job.TotalFiles = 1;
            }
            catch (Exception)
            {
                job.Status = JobStatus.Failed;
            }
            
            return job;
        }
        
        /// <summary>
        /// 배치 변환 작업 생성
        /// </summary>
        public static ConversionJob CreateBatchJob(List<string> filePaths, ConversionOptions options)
        {
            var job = new ConversionJob
            {
                Options = options ?? new ConversionOptions()
            };
            
            foreach (var filePath in filePaths)
            {
                try
                {
                    var document = new HwpDocument(filePath);
                    job.Documents.Add(document);
                }
                catch (Exception)
                {
                    // 개별 파일 로드 실패 로그
                }
            }
            
            job.TotalFiles = job.Documents.Count;
            
            if (job.TotalFiles == 0)
                job.Status = JobStatus.Failed;
                
            return job;
        }

        public double GetProgress()
        {
            if (TotalFiles == 0) return 0;
            return (double)(CompletedFiles + FailedFiles) / TotalFiles;
        }
        
        /// <summary>
        /// 작업 시작 설정
        /// </summary>
        public void Start()
        {
            Status = JobStatus.InProgress;
            StartTime = DateTime.Now;
        }
        
        /// <summary>
        /// 작업 완료 처리
        /// </summary>
        public void Complete()
        {
            Status = CompletedFiles == TotalFiles ? JobStatus.Completed : 
                     FailedFiles == TotalFiles ? JobStatus.Failed : 
                     JobStatus.Completed; // 일부 성공은 완료로 간주
                     
            EndTime = DateTime.Now;
        }
        
        /// <summary>
        /// 작업 취소 처리
        /// </summary>
        public void Cancel()
        {
            Status = JobStatus.Cancelled;
            EndTime = DateTime.Now;
        }
        
        /// <summary>
        /// 파일 완료 처리
        /// </summary>
        public void FileCompleted(PdfOutput result)
        {
            Results.Add(result);
            
            // 처리된 파일 목록에 추가
            ProcessedFiles.Add(new ProcessedFile
            {
                FilePath = result.FilePath,
                OutputPath = result.OutputPath,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage
            });
            
            if (result.Success)
                CompletedFiles++;
            else
                FailedFiles++;
                
            // 현재 처리 중인 파일 정보 초기화
            CurrentFile = null;
        }
        
        /// <summary>
        /// 현재 처리 중인 파일 설정
        /// </summary>
        public void SetCurrentFile(string filePath)
        {
            CurrentFile = filePath;
        }
    }

    public enum JobStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// 처리된 파일 정보
    /// </summary>
    public class ProcessedFile
    {
        public string FilePath { get; set; }
        public string OutputPath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
} 