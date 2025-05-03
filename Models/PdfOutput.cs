using System;
using System.IO;

namespace HwpToPdf.Models
{
    /// <summary>
    /// PDF 출력 정보를 담는 모델 클래스
    /// </summary>
    public class PdfOutput
    {
        public string FilePath { get; set; } // 입력 파일 경로
        public string OutputPath { get; set; }
        public string OutputFileName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ConversionTime { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
        public long FileSizeBytes { get; set; }

        public PdfOutput(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException(nameof(outputPath));

            OutputPath = outputPath;
            OutputFileName = Path.GetFileName(outputPath);
            ConversionTime = DateTime.Now;
            Success = false;
        }
        
        /// <summary>
        /// 변환 성공 설정
        /// </summary>
        public void SetSuccess(TimeSpan duration)
        {
            Success = true;
            ProcessingDuration = duration;
            
            // 파일 크기 가져오기
            if (File.Exists(OutputPath))
            {
                try
                {
                    var fileInfo = new FileInfo(OutputPath);
                    FileSizeBytes = fileInfo.Length;
                }
                catch
                {
                    // 파일 접근 오류 무시
                }
            }
        }
        
        /// <summary>
        /// 변환 실패 설정
        /// </summary>
        public void SetFailed(string errorMessage, TimeSpan duration)
        {
            Success = false;
            ErrorMessage = errorMessage;
            ProcessingDuration = duration;
        }
        
        /// <summary>
        /// PDF 파일 열기
        /// </summary>
        public bool OpenFile()
        {
            if (!Success || !File.Exists(OutputPath))
                return false;
                
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OutputPath,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 