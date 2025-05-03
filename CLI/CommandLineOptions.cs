using System.Collections.Generic;

namespace HwpToPdf.CLI
{
    /// <summary>
    /// 명령행 인터페이스 옵션
    /// </summary>
    public class CommandLineOptions
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public string Watermark { get; set; }
        public string WatermarkImage { get; set; }
        public string Password { get; set; }
        public string PageRange { get; set; }
        public bool Recursive { get; set; }
        public bool Verbose { get; set; }
        public bool Quiet { get; set; }
        public int Compression { get; set; } = 0;
        public bool Force { get; set; }
        public int MaxThreads { get; set; } = 1;
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public string LogFile { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
    }

    /// <summary>
    /// 로그 레벨
    /// </summary>
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug,
        Trace
    }
} 