using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HwpToPdf.Controllers;
using HwpToPdf.Models;
using HwpToPdf.API.Models;

namespace HwpToPdf.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversionApiController : ControllerBase
    {
        private readonly ConversionController _conversionController;

        public ConversionApiController(ConversionController conversionController)
        {
            _conversionController = conversionController;
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] ConversionApiOptions options)
        {
            // 단일 파일 변환 API 구현
            if (file == null || file.Length == 0)
            {
                return BadRequest("파일이 없거나 크기가 0입니다.");
            }

            // 지원되는 파일 형식 확인
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".hwp" && extension != ".hwpx")
            {
                return BadRequest("지원되지 않는 파일 형식입니다. HWP 또는 HWPX 파일만 허용됩니다.");
            }

            try
            {
                // 임시 파일로 저장
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + extension);
                
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 변환 옵션 설정
                var conversionOptions = new ConversionOptions
                {
                    OutputDirectory = Path.GetTempPath(),
                    Watermark = options.Watermark,
                    PageRange = options.PageRange,
                    CompressionLevel = options.Compression
                };

                // 변환 실행
                var jobId = await _conversionController.StartSingleConversionAsync(tempFilePath, conversionOptions);
                
                // 작업 결과 반환
                return Ok(new { JobId = jobId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"변환 중 오류 발생: {ex.Message}");
            }
        }

        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            // 작업 상태 조회 API 구현
            var job = await _conversionController.GetJobStatusAsync(jobId);
            
            if (job == null)
            {
                return NotFound($"작업 ID {jobId}를 찾을 수 없습니다.");
            }

            return Ok(new
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                Progress = job.GetProgress(),
                TotalFiles = job.TotalFiles,
                CompletedFiles = job.CompletedFiles,
                FailedFiles = job.FailedFiles
            });
        }

        [HttpGet("download/{jobId}")]
        public async Task<IActionResult> DownloadResult(Guid jobId)
        {
            // 변환 결과 다운로드 API 구현
            var job = await _conversionController.GetJobStatusAsync(jobId);
            
            if (job == null)
            {
                return NotFound($"작업 ID {jobId}를 찾을 수 없습니다.");
            }

            if (job.Status != JobStatus.Completed)
            {
                return BadRequest("변환이 완료되지 않았습니다.");
            }

            if (job.Results == null || job.Results.Count == 0)
            {
                return NotFound("변환 결과 파일을 찾을 수 없습니다.");
            }

            // 파일 반환
            var pdfResult = job.Results[0];
            var pdfFilePath = pdfResult.OutputPath;
            
            if (!System.IO.File.Exists(pdfFilePath))
            {
                return NotFound("PDF 파일을 찾을 수 없습니다.");
            }

            return PhysicalFile(pdfFilePath, "application/pdf", Path.GetFileName(pdfFilePath));
        }
    }
} 