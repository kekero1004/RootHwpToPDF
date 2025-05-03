using System;
using System.IO;

namespace HwpToPdf.Models
{
    /// <summary>
    /// HWP 문서 정보를 담는 모델 클래스
    /// </summary>
    public class HwpDocument
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Password { get; set; }
        public long FileSize { get; set; }
        public bool IsPasswordProtected { get; set; }
        public bool IsHwpx { get; set; }
        public DateTime LastModified { get; set; }

        public HwpDocument(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
                
            if (!File.Exists(filePath))
                throw new FileNotFoundException("한글 파일을 찾을 수 없습니다.", filePath);

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            
            var fileInfo = new FileInfo(filePath);
            FileSize = fileInfo.Length;
            LastModified = fileInfo.LastWriteTime;
            
            // 확장자 확인으로 hwpx 여부 결정
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            IsHwpx = extension == ".hwpx";
        }
        
        /// <summary>
        /// 파일 유효성 검사
        /// </summary>
        public bool Validate()
        {
            // 기본 검사: 파일 존재 및 크기 확인
            if (!File.Exists(FilePath))
                return false;
                
            if (FileSize == 0)
                return false;
                
            // 확장자 확인
            string extension = Path.GetExtension(FilePath).ToLowerInvariant();
            if (extension != ".hwp" && extension != ".hwpx")
                return false;
                
            return true;
        }
    }
} 