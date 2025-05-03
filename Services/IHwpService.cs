using System;
using System.Threading.Tasks;
using HwpToPdf.Models;

namespace HwpToPdf.Services
{
    /// <summary>
    /// 한글 문서 처리 인터페이스
    /// </summary>
    public interface IHwpService
    {
        Task<HwpDocument> LoadDocumentAsync(string filePath, string password = null);
        Task<bool> ValidateDocumentAsync(HwpDocument document);
        Task<PdfOutput> ConvertToPdfAsync(HwpDocument document, ConversionOptions options);
        Task<bool> IsPasswordProtectedAsync(string filePath);
        Task CloseDocumentAsync(HwpDocument document);
    }
} 