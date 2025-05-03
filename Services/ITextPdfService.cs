using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using HwpToPdf.Models;
using HwpToPdf.Utils;

namespace HwpToPdf.Services
{
    /// <summary>
    /// iText 라이브러리를 사용한 PDF 후처리 서비스 구현
    /// </summary>
    public class ITextPdfService : IPdfPostProcessingService
    {
        public async Task<PdfOutput> AddWatermarkAsync(PdfOutput pdf, string watermarkText)
        {
            if (string.IsNullOrEmpty(watermarkText) || !File.Exists(pdf.OutputPath))
            {
                return pdf;
            }
            
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                
                try
                {
                    // 임시 파일 경로 생성
                    string tempFilePath = FileHelper.CreateTempFile(".pdf");
                    
                    // 원본 PDF 열기
                    PdfDocument pdfDoc = new PdfDocument(
                        new PdfReader(pdf.OutputPath),
                        new PdfWriter(tempFilePath)
                    );
                    
                    // 워터마크 추가
                    Document document = new Document(pdfDoc);
                    
                    // 문서의 페이지 수
                    int numberOfPages = pdfDoc.GetNumberOfPages();
                    
                    // 모든 페이지에 워터마크 추가
                    for (int i = 1; i <= numberOfPages; i++)
                    {
                        PdfPage page = pdfDoc.GetPage(i);
                        Rectangle pageSize = page.GetPageSize();
                        float width = pageSize.GetWidth();
                        float height = pageSize.GetHeight();
                        
                        // 워터마크 텍스트
                        Paragraph watermark = new Paragraph(watermarkText)
                            .SetFontSize(24)
                            .SetFontColor(new DeviceRgb(211, 211, 211))
                            .SetOpacity(0.5f)
                            .SetRotationAngle(Math.PI / 4) // 45도 회전
                            .SetFixedPosition(width / 4, height / 2, width / 2)
                            .SetTextAlignment(TextAlignment.CENTER);
                        
                        // 페이지에 워터마크 추가
                        new Canvas(page, pageSize)
                            .Add(watermark);
                    }
                    
                    // 문서 닫기
                    document.Close();
                    
                    // 원본 파일 교체
                    File.Delete(pdf.OutputPath);
                    File.Move(tempFilePath, pdf.OutputPath);
                    
                    // 결과 업데이트
                    pdf.SetSuccess(DateTime.Now - startTime);
                    return pdf;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"워터마크 추가 중 오류 발생: {pdf.OutputPath}", ex);
                    pdf.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdf;
                }
            });
        }

        public async Task<PdfOutput> AddImageWatermarkAsync(PdfOutput pdf, string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath) || !File.Exists(pdf.OutputPath))
            {
                return pdf;
            }
            
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                
                try
                {
                    // 임시 파일 경로 생성
                    string tempFilePath = FileHelper.CreateTempFile(".pdf");
                    
                    // 원본 PDF 열기
                    PdfDocument pdfDoc = new PdfDocument(
                        new PdfReader(pdf.OutputPath),
                        new PdfWriter(tempFilePath)
                    );
                    
                    // 워터마크 이미지 로드
                    ImageData imgData = ImageDataFactory.Create(imagePath);
                    
                    // 문서의 페이지 수
                    int numberOfPages = pdfDoc.GetNumberOfPages();
                    
                    // 모든 페이지에 이미지 워터마크 추가
                    for (int i = 1; i <= numberOfPages; i++)
                    {
                        PdfPage page = pdfDoc.GetPage(i);
                        Rectangle pageSize = page.GetPageSize();
                        float width = pageSize.GetWidth();
                        float height = pageSize.GetHeight();
                        
                        // 이미지 크기 조정
                        float imgWidth = width / 4;
                        float imgHeight = imgData.GetHeight() * (imgWidth / imgData.GetWidth());
                        
                        // 이미지 워터마크
                        iText.Layout.Element.Image watermark = new iText.Layout.Element.Image(imgData)
                            .ScaleToFit(imgWidth, imgHeight)
                            .SetOpacity(0.3f)
                            .SetFixedPosition(width / 2 - imgWidth / 2, height / 2 - imgHeight / 2);
                        
                        // 페이지에 워터마크 추가
                        new Canvas(page, pageSize)
                            .Add(watermark);
                    }
                    
                    // 문서 닫기
                    pdfDoc.Close();
                    
                    // 원본 파일 교체
                    File.Delete(pdf.OutputPath);
                    File.Move(tempFilePath, pdf.OutputPath);
                    
                    // 결과 업데이트
                    pdf.SetSuccess(DateTime.Now - startTime);
                    return pdf;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"이미지 워터마크 추가 중 오류 발생: {pdf.OutputPath}", ex);
                    pdf.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdf;
                }
            });
        }

        public async Task<PdfOutput> CompressPdfAsync(PdfOutput pdf, int compressionLevel)
        {
            if (compressionLevel <= 0 || !File.Exists(pdf.OutputPath))
            {
                return pdf;
            }
            
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                
                try
                {
                    // 임시 파일 경로 생성
                    string tempFilePath = FileHelper.CreateTempFile(".pdf");
                    
                    // 압축 레벨에 따른 설정
                    int compLevel = CompressionConstants.DEFAULT_COMPRESSION;
                    
                    if (compressionLevel >= 9)
                    {
                        compLevel = CompressionConstants.BEST_COMPRESSION;
                    }
                    else if (compressionLevel >= 5)
                    {
                        compLevel = CompressionConstants.BEST_SPEED;
                    }
                    
                    // 압축 옵션 설정
                    WriterProperties writerProperties = new WriterProperties()
                        .SetCompressionLevel(compLevel)
                        .UseSmartMode();
                    
                    // 원본 PDF 읽고 압축된 PDF 생성
                    PdfDocument pdfDoc = new PdfDocument(
                        new PdfReader(pdf.OutputPath),
                        new PdfWriter(tempFilePath, writerProperties)
                    );
                    
                    // 문서 닫기
                    pdfDoc.Close();
                    
                    // 원본 파일 교체
                    File.Delete(pdf.OutputPath);
                    File.Move(tempFilePath, pdf.OutputPath);
                    
                    // 결과 업데이트
                    pdf.SetSuccess(DateTime.Now - startTime);
                    return pdf;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"PDF 압축 중 오류 발생: {pdf.OutputPath}", ex);
                    pdf.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdf;
                }
            });
        }

        public async Task<PdfOutput> ExtractPagesAsync(PdfOutput pdf, string pageRange)
        {
            if (string.IsNullOrEmpty(pageRange) || !File.Exists(pdf.OutputPath))
            {
                return pdf;
            }
            
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                
                try
                {
                    // 임시 파일 경로 생성
                    string tempFilePath = FileHelper.CreateTempFile(".pdf");
                    
                    // 원본 PDF 열기
                    PdfReader reader = new PdfReader(pdf.OutputPath);
                    PdfDocument srcDoc = new PdfDocument(reader);
                    
                    // 총 페이지 수
                    int totalPages = srcDoc.GetNumberOfPages();
                    
                    // 페이지 범위 파싱
                    (int startPage, int endPage) = ParsePageRange(pageRange, totalPages);
                    
                    // 범위가 유효하지 않은 경우
                    if (startPage <= 0 || endPage <= 0 || startPage > totalPages || endPage > totalPages || startPage > endPage)
                    {
                        srcDoc.Close();
                        return pdf;
                    }
                    
                    // 새 PDF 생성
                    PdfWriter writer = new PdfWriter(tempFilePath);
                    PdfDocument destDoc = new PdfDocument(writer);
                    
                    // 지정된 페이지 복사
                    for (int i = startPage; i <= endPage; i++)
                    {
                        srcDoc.CopyPagesTo(i, i, destDoc);
                    }
                    
                    // 문서 닫기
                    destDoc.Close();
                    srcDoc.Close();
                    
                    // 원본 파일 교체
                    File.Delete(pdf.OutputPath);
                    File.Move(tempFilePath, pdf.OutputPath);
                    
                    // 결과 업데이트
                    pdf.SetSuccess(DateTime.Now - startTime);
                    return pdf;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"페이지 추출 중 오류 발생: {pdf.OutputPath}", ex);
                    pdf.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdf;
                }
            });
        }

        public async Task<PdfOutput> OptimizePdfAsync(PdfOutput pdf)
        {
            if (!File.Exists(pdf.OutputPath))
            {
                return pdf;
            }
            
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                
                try
                {
                    // 임시 파일 경로 생성
                    string tempFilePath = FileHelper.CreateTempFile(".pdf");
                    
                    // 최적화 옵션 설정
                    WriterProperties writerProperties = new WriterProperties()
                        .SetCompressionLevel(CompressionConstants.BEST_COMPRESSION)
                        .UseSmartMode();
                    
                    // 원본 PDF 읽고 최적화된 PDF 생성
                    PdfDocument pdfDoc = new PdfDocument(
                        new PdfReader(pdf.OutputPath),
                        new PdfWriter(tempFilePath, writerProperties)
                    );
                    
                    // 문서 최적화 설정
                    pdfDoc.GetWriter().SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
                    
                    // 문서 닫기
                    pdfDoc.Close();
                    
                    // 원본 파일 교체
                    File.Delete(pdf.OutputPath);
                    File.Move(tempFilePath, pdf.OutputPath);
                    
                    // 결과 업데이트
                    pdf.SetSuccess(DateTime.Now - startTime);
                    return pdf;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"PDF 최적화 중 오류 발생: {pdf.OutputPath}", ex);
                    pdf.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdf;
                }
            });
        }
        
        /// <summary>
        /// 페이지 범위 파싱 (예: "1-3", "5-끝")
        /// </summary>
        private (int start, int end) ParsePageRange(string pageRange, int totalPages)
        {
            if (string.IsNullOrEmpty(pageRange))
            {
                return (1, totalPages);
            }
            
            string[] parts = pageRange.Split('-');
            
            if (parts.Length == 2)
            {
                int start = 1;
                int end = totalPages;
                
                if (int.TryParse(parts[0], out int parsedStart))
                {
                    start = parsedStart;
                }
                
                if (parts[1].Trim() != "끝" && int.TryParse(parts[1], out int parsedEnd))
                {
                    end = parsedEnd;
                }
                
                // 유효성 검사
                start = Math.Max(1, Math.Min(start, totalPages));
                end = Math.Max(1, Math.Min(end, totalPages));
                
                if (start <= end)
                {
                    return (start, end);
                }
            }
            else if (parts.Length == 1 && int.TryParse(parts[0], out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= totalPages)
                {
                    return (singlePage, singlePage);
                }
            }
            
            // 기본값 반환
            return (1, totalPages);
        }
    }

    internal class CompressionConstants
    {
        public const int NO_COMPRESSION = 0;
        public const int BEST_SPEED = 1;
        public const int DEFAULT_COMPRESSION = 6;
        public const int BEST_COMPRESSION = 9;
    }
} 