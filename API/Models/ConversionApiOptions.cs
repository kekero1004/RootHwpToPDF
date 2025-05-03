using System.ComponentModel.DataAnnotations;

namespace HwpToPdf.API.Models
{
    /// <summary>
    /// API 요청 변환 옵션
    /// </summary>
    public class ConversionApiOptions
    {
        /// <summary>
        /// 워터마크 텍스트
        /// </summary>
        public string Watermark { get; set; }

        /// <summary>
        /// 변환할 페이지 범위 (예: "1-3,5,7-끝")
        /// </summary>
        public string PageRange { get; set; }

        /// <summary>
        /// PDF 압축 레벨 (0=압축 없음, 9=최대 압축)
        /// </summary>
        [Range(0, 9, ErrorMessage = "압축 레벨은 0에서 9 사이의 값이어야 합니다.")]
        public int Compression { get; set; } = 0;

        /// <summary>
        /// 암호화된 문서의 암호
        /// </summary>
        public string Password { get; set; }
    }
} 