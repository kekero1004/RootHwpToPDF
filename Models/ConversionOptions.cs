using System;
using System.IO;

namespace HwpToPdf.Models
{
    /// <summary>
    /// HWP에서 PDF로 변환 옵션을 담는 모델 클래스
    /// </summary>
    public class ConversionOptions
    {
        public string OutputDirectory { get; set; }
        public string Watermark { get; set; }
        public string WatermarkImagePath { get; set; }
        public string PageRange { get; set; }
        public bool OverwriteExisting { get; set; }
        public int CompressionLevel { get; set; }
        public bool OpenAfterConversion { get; set; }
        public bool RecursiveSearch { get; set; }

        public ConversionOptions()
        {
            // 기본 옵션 설정
            OutputDirectory = Environment.CurrentDirectory;
            CompressionLevel = 0; // 기본 압축 없음
            OverwriteExisting = false;
            OpenAfterConversion = false;
            RecursiveSearch = false;
        }
        
        /// <summary>
        /// 옵션 유효성 검사
        /// </summary>
        public bool Validate()
        {
            // 출력 디렉토리 확인
            if (!string.IsNullOrEmpty(OutputDirectory) && !Directory.Exists(OutputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
                catch
                {
                    return false;
                }
            }
            
            // 워터마크 이미지 경로 확인
            if (!string.IsNullOrEmpty(WatermarkImagePath) && !File.Exists(WatermarkImagePath))
                return false;
                
            // 압축 레벨 범위 확인
            if (CompressionLevel < 0 || CompressionLevel > 9)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 페이지 범위 파싱
        /// </summary>
        public (int Start, int End) ParsePageRange(int totalPages)
        {
            if (string.IsNullOrEmpty(PageRange))
                return (1, totalPages);
                
            // 범위 파싱 (예: "1-3", "5-끝")
            string[] parts = PageRange.Split('-');
            if (parts.Length == 2)
            {
                int start = 1;
                int end = totalPages;
                
                if (int.TryParse(parts[0], out int parsedStart))
                    start = parsedStart;
                    
                if (parts[1].Trim() != "끝" && int.TryParse(parts[1], out int parsedEnd))
                    end = parsedEnd;
                    
                // 유효성 검사
                start = Math.Max(1, start);
                end = Math.Min(totalPages, end);
                
                if (start <= end)
                    return (start, end);
            }
            else if (parts.Length == 1 && int.TryParse(parts[0], out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= totalPages)
                    return (singlePage, singlePage);
            }
            
            // 기본값 반환
            return (1, totalPages);
        }
    }
} 