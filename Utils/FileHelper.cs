using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HwpToPdf.Utils
{
    /// <summary>
    /// 파일 처리 유틸리티 클래스
    /// </summary>
    public static class FileHelper
    {
        private static readonly string[] HwpExtensions = { ".hwp", ".hwpx" };

        /// <summary>
        /// HWP 파일 목록 조회
        /// </summary>
        public static List<string> GetHwpFiles(string path, bool recursive = false)
        {
            List<string> files = new List<string>();

            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (HwpExtensions.Contains(extension))
                {
                    files.Add(path);
                }
            }
            else if (Directory.Exists(path))
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                foreach (var ext in HwpExtensions)
                {
                    files.AddRange(Directory.GetFiles(path, "*" + ext, searchOption));
                }
            }
            else if (path.Contains("*") || path.Contains("?"))
            {
                string directory = Path.GetDirectoryName(path);
                string pattern = Path.GetFileName(path);

                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }

                if (Directory.Exists(directory))
                {
                    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    files.AddRange(Directory.GetFiles(directory, pattern, searchOption));
                }
            }

            return files.OrderBy(f => f).ToList();
        }

        /// <summary>
        /// 출력 파일 경로 생성
        /// </summary>
        public static string GetOutputFilePath(string inputPath, string outputDirectory, bool overwriteExisting = false)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var outputFilePath = Path.Combine(outputDirectory, fileName + ".pdf");

            if (!overwriteExisting && File.Exists(outputFilePath))
            {
                int counter = 1;
                while (File.Exists(outputFilePath))
                {
                    outputFilePath = Path.Combine(outputDirectory, $"{fileName}_{counter}.pdf");
                    counter++;
                }
            }

            return outputFilePath;
        }

        /// <summary>
        /// 임시 파일 생성
        /// </summary>
        public static string CreateTempFile(string extension = ".tmp")
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "HwpToPdf");
            Directory.CreateDirectory(tempPath);
            return Path.Combine(tempPath, Guid.NewGuid().ToString() + extension);
        }

        /// <summary>
        /// 임시 파일 정리
        /// </summary>
        public static void CleanupTempFiles(string path = null, int olderThanHours = 24)
        {
            try
            {
                string tempPath = path ?? Path.Combine(Path.GetTempPath(), "HwpToPdf");
                if (!Directory.Exists(tempPath))
                    return;

                var cutoffTime = DateTime.Now.AddHours(-olderThanHours);
                foreach (var file in Directory.GetFiles(tempPath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffTime)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // 삭제 실패 무시
                        }
                    }
                }
            }
            catch
            {
                // 정리 실패 무시
            }
        }

        /// <summary>
        /// 파일 해시값 계산
        /// </summary>
        public static string GetFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
} 