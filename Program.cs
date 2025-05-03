using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using HwpToPdf.CLI;
using HwpToPdf.Controllers;
using HwpToPdf.Services;
using HwpToPdf.Utils;
using HwpToPdf.Views.WPF;
using Microsoft.Extensions.DependencyInjection;

namespace HwpToPdf
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                // 로그 초기화
                string logFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HwpToPdf",
                    "logs",
                    $"hwp2pdf_{DateTime.Now:yyyyMMdd}.log"
                );
                
                LogHelper.Initialize(HwpToPdf.Utils.LogLevel.Info, logFilePath);
                
                // 서비스 설정
                var services = ConfigureServices();
                
                // 임시 파일 정리
                FileHelper.CleanupTempFiles();
                
                try
                {
                    // 한글 프로그램 연결 확인
                    var hwpService = services.GetRequiredService<IHwpService>();
                    
                    if (args.Length > 0 && !args[0].StartsWith("--gui"))
                    {
                        // CLI 모드로 실행
                        var controller = services.GetRequiredService<ConversionController>();
                        var cliApp = new CLIApplication(args, controller);
                        return cliApp.RunAsync().GetAwaiter().GetResult();
                    }
                    else
                    {
                        // GUI 모드로 실행 - WPF 애플리케이션 시작
                        return StartWpfApplication(services);
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("한글"))
                {
                    // 한글 관련 오류 처리
                    string errorMessage = "한글 프로그램을 찾을 수 없습니다.\n\n" +
                                          "이 프로그램을 사용하려면 한글(한컴오피스 한글) 프로그램이 설치되어 있어야 합니다.\n" +
                                          "한글 프로그램을 설치한 후 다시 실행해주세요.\n\n" +
                                          "오류 메시지: " + ex.Message;
                    
                    LogHelper.LogError("한글 프로그램 오류", ex);
                    Console.Error.WriteLine(errorMessage);
                    
                    if (args.Length == 0 || args[0].StartsWith("--gui"))
                    {
                        // GUI 모드일 경우 팝업 표시
                        System.Windows.MessageBox.Show(errorMessage, "한글 프로그램 오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("치명적인 오류", ex);
                Console.Error.WriteLine($"치명적 오류: {ex.Message}");
                return 1;
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 서비스 등록
            services.AddSingleton<IHwpService, HwpComInteropService>();
            services.AddSingleton<IPdfPostProcessingService, ITextPdfService>();
            services.AddSingleton<ConversionQueueService>();
            services.AddSingleton<ConversionController>();
            
            // WPF 뷰 등록
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }

        private static int StartWpfApplication(IServiceProvider services)
        {
            try
            {
                // WPF 애플리케이션 생성
                var app = new Application();
                
                // 메인 윈도우 생성
                var mainWindow = services.GetRequiredService<MainWindow>();
                
                // 애플리케이션 실행
                app.Run(mainWindow);
                
                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("WPF 애플리케이션 실행 중 오류", ex);
                return 1;
            }
        }
        
        public static string GetAssemblyVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version.ToString();
        }
    }
} 