using System.Threading.Tasks;
using HwpToPdf.Models;

namespace HwpToPdf.Services
{
    /// <summary>
    /// PDF 후처리 인터페이스
    /// </summary>
    public interface IPdfPostProcessingService
    {
        Task<PdfOutput> AddWatermarkAsync(PdfOutput pdf, string watermarkText);
        Task<PdfOutput> AddImageWatermarkAsync(PdfOutput pdf, string imagePath);
        Task<PdfOutput> CompressPdfAsync(PdfOutput pdf, int compressionLevel);
        Task<PdfOutput> ExtractPagesAsync(PdfOutput pdf, string pageRange);
        Task<PdfOutput> OptimizePdfAsync(PdfOutput pdf);
    }
} 