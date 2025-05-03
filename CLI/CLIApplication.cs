using System;
using System.Linq;
using System.Threading.Tasks;
using HwpToPdf.Controllers;
using HwpToPdf.Models;
using HwpToPdf.Services;

namespace HwpToPdf.CLI
{
    /// <summary>
    /// CLI 애플리케이션
    /// </summary>
    public class CLIApplication
    {
        private readonly CommandLineOptions _options;
        private readonly ConversionController _controller;
        private bool _processingComplete = false;
        private int _exitCode = 0;

        public CLIApplication(string[] args, ConversionController controller)
        {
            var parser = new CommandLineParser();
            _options = parser.Parse(args);
            _controller = controller;
            
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            _controller.JobStatusChanged += Controller_JobStatusChanged;
            _controller.LogMessage += Controller_LogMessage;
        }

        public async Task<int> RunAsync()
        {
            if (_options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            if (_options.ShowVersion)
            {
                ShowVersion();
                return 0;
            }

            if (string.IsNullOrEmpty(_options.Input))
            {
                Console.Error.WriteLine("오류: 입력 파일 또는 디렉토리가 지정되지 않았습니다.");
                return 1;
            }

            try
            {
                await StartConversionAsync();
                
                // 변환 완료될 때까지 대기
                while (!_processingComplete)
                {
                    await Task.Delay(100);
                }
                
                return _exitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"오류: {ex.Message}");
                return 1;
            }
        }

        private async Task StartConversionAsync()
        {
            // CLI 옵션을 ConversionOptions로 변환
            var conversionOptions = MapCommandLineToConversionOptions();
            
            if (System.IO.Directory.Exists(_options.Input))
            {
                // 디렉토리 변환
                string searchPattern = "*.hw?";
                await _controller.StartFolderConversionAsync(_options.Input, searchPattern, _options.Recursive, conversionOptions);
            }
            else if (_options.Input.Contains("*") || _options.Input.Contains("?"))
            {
                // 와일드카드 패턴 변환
                var directory = System.IO.Path.GetDirectoryName(_options.Input);
                var pattern = System.IO.Path.GetFileName(_options.Input);
                
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }
                
                var files = System.IO.Directory.GetFiles(directory, pattern);
                await _controller.StartBatchConversionAsync(files.ToList(), conversionOptions);
            }
            else
            {
                // 단일 파일 변환
                await _controller.StartSingleConversionAsync(_options.Input, conversionOptions);
            }
        }

        private ConversionOptions MapCommandLineToConversionOptions()
        {
            return new ConversionOptions
            {
                OutputDirectory = _options.Output,
                Watermark = _options.Watermark,
                WatermarkImagePath = _options.WatermarkImage,
                PageRange = _options.PageRange,
                OverwriteExisting = _options.Force,
                CompressionLevel = _options.Compression,
                RecursiveSearch = _options.Recursive
            };
        }

        private void ShowHelp()
        {
            Console.WriteLine(new CommandLineParser().GetHelpText());
        }

        private void ShowVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"HWP to PDF 변환 도구 v{version}");
        }

        private void Controller_JobStatusChanged(object sender, ConversionJob job)
        {
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
            {
                _processingComplete = true;
                _exitCode = job.FailedFiles > 0 ? 4 : 0;
                
                if (!_options.Quiet)
                {
                    Console.WriteLine($"변환 완료: 성공 {job.CompletedFiles - job.FailedFiles}/{job.TotalFiles}, 실패 {job.FailedFiles}/{job.TotalFiles}");
                }
            }
            else if (job.Status == JobStatus.InProgress && !_options.Quiet)
            {
                Console.Write($"\r진행 중: {job.CompletedFiles}/{job.TotalFiles} ({job.GetProgress():P0})");
            }
        }

        private void Controller_LogMessage(object sender, string message)
        {
            if (!_options.Quiet || message.StartsWith("오류:"))
            {
                Console.WriteLine(message);
            }
        }
    }
} 