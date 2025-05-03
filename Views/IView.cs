using System;
using HwpToPdf.Models;

namespace HwpToPdf.Views
{
    /// <summary>
    /// 뷰 인터페이스
    /// </summary>
    public interface IView
    {
        void ShowMessage(string message);
        void ShowError(string error);
        void UpdateProgress(double progress, string status);
        void UpdateJobStatus(ConversionJob job);
        void FileSelectionCompleted(string[] files);
        void ConversionCompleted(ConversionJob job);
    }
} 