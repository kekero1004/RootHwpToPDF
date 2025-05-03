using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace HwpToPdf.Models
{
    /// <summary>
    /// 변환할 파일 항목 모델
    /// </summary>
    public class FileItem : INotifyPropertyChanged
    {
        private string _filePath;
        private string _status;
        private bool _isConverted;

        public FileItem(string filePath)
        {
            FilePath = filePath;
            Status = "대기 중";
            IsConverted = false;
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName)); // FilePath가 변경되면 FileName도 변경됨
            }
        }

        public string FileName => Path.GetFileName(FilePath);

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsConverted
        {
            get => _isConverted;
            set
            {
                _isConverted = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 