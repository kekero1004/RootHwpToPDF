using System;
using System.Collections.Generic;
using System.IO;

namespace HwpToPdf.CLI
{
    /// <summary>
    /// 명령행 인터페이스 파서
    /// </summary>
    public class CommandLineParser
    {
        public CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();
            
            if (args.Length == 0)
            {
                options.ShowHelp = true;
                return options;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                
                // 도움말 또는 버전 정보 표시
                if (arg == "-h" || arg == "--help")
                {
                    options.ShowHelp = true;
                    return options;
                }
                else if (arg == "--version")
                {
                    options.ShowVersion = true;
                    return options;
                }
                
                // 파라미터 처리
                if (i + 1 < args.Length)
                {
                    string nextArg = args[i + 1];
                    
                    if (nextArg.StartsWith("-"))
                        nextArg = null;
                        
                    switch (arg)
                    {
                        case "-i":
                        case "--input":
                            if (nextArg != null)
                            {
                                options.Input = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-o":
                        case "--output":
                            if (nextArg != null)
                            {
                                options.Output = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-w":
                        case "--watermark":
                            if (nextArg != null)
                            {
                                options.Watermark = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-wi":
                        case "--watermark-image":
                            if (nextArg != null)
                            {
                                options.WatermarkImage = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-p":
                        case "--password":
                            if (nextArg != null)
                            {
                                options.Password = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-pg":
                        case "--page-range":
                            if (nextArg != null)
                            {
                                options.PageRange = nextArg;
                                i++;
                            }
                            break;
                            
                        case "-c":
                        case "--compression":
                            if (nextArg != null && int.TryParse(nextArg, out int compressionLevel))
                            {
                                options.Compression = Math.Max(0, Math.Min(9, compressionLevel));
                                i++;
                            }
                            break;
                            
                        case "-t":
                        case "--threads":
                            if (nextArg != null && int.TryParse(nextArg, out int maxThreads))
                            {
                                options.MaxThreads = Math.Max(1, Math.Min(8, maxThreads));
                                i++;
                            }
                            break;
                            
                        case "-l":
                        case "--log-level":
                            if (nextArg != null)
                            {
                                options.LogLevel = ParseLogLevel(nextArg);
                                i++;
                            }
                            break;
                            
                        case "-lf":
                        case "--log-file":
                            if (nextArg != null)
                            {
                                options.LogFile = nextArg;
                                i++;
                            }
                            break;
                            
                        default:
                            // 위치 기반 파라미터 (첫 번째는 입력으로 간주)
                            if (arg[0] != '-' && string.IsNullOrEmpty(options.Input))
                            {
                                options.Input = arg;
                            }
                            break;
                    }
                }
                else
                {
                    // 마지막 인자는 입력 파일 또는 플래그
                    if (arg[0] != '-' && string.IsNullOrEmpty(options.Input))
                    {
                        options.Input = arg;
                    }
                    else
                    {
                        // 값 없는 플래그 처리
                        switch (arg)
                        {
                            case "-r":
                            case "--recursive":
                                options.Recursive = true;
                                break;
                                
                            case "-v":
                            case "--verbose":
                                options.Verbose = true;
                                options.LogLevel = LogLevel.Debug;
                                break;
                                
                            case "-q":
                            case "--quiet":
                                options.Quiet = true;
                                options.LogLevel = LogLevel.Error;
                                break;
                                
                            case "-f":
                            case "--force":
                                options.Force = true;
                                break;
                        }
                    }
                }
            }
            
            // 기본값 설정
            if (string.IsNullOrEmpty(options.Output) && !string.IsNullOrEmpty(options.Input))
            {
                if (File.Exists(options.Input))
                {
                    options.Output = Path.GetDirectoryName(options.Input);
                }
                else if (Directory.Exists(options.Input))
                {
                    options.Output = options.Input;
                }
                else
                {
                    options.Output = Environment.CurrentDirectory;
                }
            }
            
            return options;
        }
        
        private LogLevel ParseLogLevel(string level)
        {
            if (string.IsNullOrEmpty(level))
                return LogLevel.Info;
                
            return level.ToLowerInvariant() switch
            {
                "error" => LogLevel.Error,
                "warning" => LogLevel.Warning,
                "warn" => LogLevel.Warning,
                "info" => LogLevel.Info,
                "debug" => LogLevel.Debug,
                "trace" => LogLevel.Trace,
                _ => LogLevel.Info
            };
        }

        public string GetHelpText()
        {
            return @"
HWP to PDF 변환 도구 v1.0.0

사용법: hwp2pdf [옵션] <입력 파일/패턴>

옵션:
  -i, --input <경로>          입력 파일 또는 디렉토리 (필수)
  -o, --output <경로>         출력 디렉토리 (기본값: 입력 파일과 동일)
  -w, --watermark <텍스트>    워터마크 텍스트 추가
  -wi, --watermark-image <경로> 워터마크 이미지 추가
  -p, --password <암호>       암호화된 문서의 암호
  -pg, --page-range <범위>    페이지 범위 (예: 1-3,5,7-끝)
  -r, --recursive             하위 디렉토리 포함
  -v, --verbose               상세 로그 출력
  -q, --quiet                 오류만 출력
  -c, --compression <0-9>     PDF 압축 레벨 (0=없음, 9=최대)
  -f, --force                 기존 파일 덮어쓰기
  -t, --threads <수>          최대 스레드 수 (기본값: 1)
  -l, --log-level <레벨>      로그 레벨 (오류, 경고, 정보, 디버그, 추적)
  -lf, --log-file <파일>      로그 파일 경로
  -h, --help                  도움말 표시
  --version                   버전 정보 표시

예제:
  hwp2pdf -i sample.hwp -o output_dir
  hwp2pdf -i 'c:\docs\*.hwp' -o 'c:\pdfs' -w '기밀문서' -r
";
        }
    }
} 