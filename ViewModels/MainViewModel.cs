using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
using HwpToPdf.Controllers;
using HwpToPdf.Models;
using Microsoft.Win32;
using System.IO;

namespace HwpToPdf.ViewModels
{
    /// <summary>
    /// 메인 화면 ViewModel
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ConversionController _controller;
        private string _statusMessage;
        private double _progressValue;
        private bool _isConverting;
        private ConversionJob _currentJob;
        private ConversionOptions _options;

        public MainViewModel(ConversionController controller)
        {
            _controller = controller;
            FileItems = new ObservableCollection<FileItem>();
            _options = new ConversionOptions();
            _options.OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            
            // 명령 초기화
            BrowseCommand = new RelayCommand(BrowseFiles);
            BrowseOutputCommand = new RelayCommand(BrowseOutputDirectory);
            ConvertCommand = new RelayCommand(Convert, CanConvert);
            CancelCommand = new RelayCommand(Cancel, CanCancel);
            ClearCommand = new RelayCommand(ClearFiles, () => FileItems.Count > 0);
            BrowseFolderCommand = new RelayCommand(BrowseFolderForFiles);
            
            // 이벤트 등록
            _controller.JobStatusChanged += Controller_JobStatusChanged;
            _controller.LogMessage += Controller_LogMessage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 기존 Files 컬렉션 대신 FileItems 컬렉션을 사용
        public ObservableCollection<FileItem> FileItems { get; }
        
        // 하위 호환성을 위한 Files 속성 (필요한 경우 사용)
        public IEnumerable<string> Files => FileItems.Select(f => f.FilePath);
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }
        
        public bool IsConverting
        {
            get => _isConverting;
            set => SetProperty(ref _isConverting, value);
        }
        
        public ConversionOptions Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        public ICommand BrowseCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        private void BrowseFiles()
        {
            // 파일 탐색 로직
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "한글 파일 (*.hwp;*.hwpx)|*.hwp;*.hwpx|모든 파일 (*.*)|*.*";
            dialog.Title = "변환할 한글 파일 선택";
            
            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    if (!Files.Contains(file))
                    {
                        FileItems.Add(new FileItem(file));
                    }
                }
                
                // 출력 디렉토리가 설정되지 않았으면 첫 파일의 디렉토리로 설정
                if (string.IsNullOrEmpty(Options.OutputDirectory) && FileItems.Count > 0)
                {
                    Options.OutputDirectory = Path.GetDirectoryName(FileItems[0].FilePath);
                }
                
                StatusMessage = $"{FileItems.Count}개 파일이 선택되었습니다.";
            }
        }
        
        private void BrowseOutputDirectory()
        {
            // 출력 디렉토리 선택 로직 (PDF 저장 폴더 선택)
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "PDF 파일을 저장할 폴더를 선택하세요";
                dialog.ShowNewFolderButton = true;
                
                // 현재 경로가 있으면 초기 경로로 설정
                if (!string.IsNullOrEmpty(Options.OutputDirectory) && System.IO.Directory.Exists(Options.OutputDirectory))
                {
                    dialog.SelectedPath = Options.OutputDirectory;
                }
                
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    // PDF 저장 폴더 경로 설정
                    Options.OutputDirectory = dialog.SelectedPath;
                    StatusMessage = $"PDF 저장 폴더가 설정되었습니다: {dialog.SelectedPath}";
                }
            }
        }

        private void BrowseFolderForFiles()
        {
            // 폴더 선택 로직
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "한글 파일이 있는 폴더를 선택하세요";
                dialog.ShowNewFolderButton = false;
                
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    try
                    {
                        // 하위 폴더 포함하여 모든 한글 파일 검색
                        var hwpFiles = Directory.GetFiles(dialog.SelectedPath, "*.hwp", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(dialog.SelectedPath, "*.hwpx", SearchOption.AllDirectories))
                            .ToList();
                        
                        int addedCount = 0;
                        foreach (string file in hwpFiles)
                        {
                            if (!Files.Contains(file))
                            {
                                FileItems.Add(new FileItem(file));
                                addedCount++;
                            }
                        }
                        
                        // 출력 디렉토리가 설정되지 않았으면 선택한 폴더로 설정
                        if (string.IsNullOrEmpty(Options.OutputDirectory))
                        {
                            Options.OutputDirectory = dialog.SelectedPath;
                        }
                        
                        StatusMessage = $"{addedCount}개 한글 파일이 추가되었습니다. (총 {FileItems.Count}개)";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"폴더 검색 중 오류 발생: {ex.Message}";
                    }
                }
            }
        }

        private async void Convert()
        {
            // 변환 로직
            IsConverting = true;
            StatusMessage = "변환 시작...";
            ProgressValue = 0;
            
            try
            {
                // 모든 파일 상태 초기화
                foreach (var fileItem in FileItems)
                {
                    fileItem.Status = "대기 중";
                    fileItem.IsConverted = false;
                }
                
                // 컨트롤러 호출
                List<string> filePaths = FileItems.Select(f => f.FilePath).ToList();
                
                // 배치 변환 시작
                Guid jobId = await _controller.StartBatchConversionAsync(filePaths, Options);
                _currentJob = await _controller.GetJobStatusAsync(jobId);
            }
            catch (Exception ex)
            {
                StatusMessage = $"오류: {ex.Message}";
                IsConverting = false;
            }
        }

        private async void Cancel()
        {
            // 취소 로직
            if (_currentJob != null)
            {
                await _controller.CancelConversionAsync(_currentJob.JobId);
                StatusMessage = "변환이 취소되었습니다.";
                
                // 처리되지 않은 파일들의 상태 업데이트
                foreach (var fileItem in FileItems)
                {
                    if (fileItem.Status == "대기 중" || fileItem.Status == "변환 중")
                    {
                        fileItem.Status = "취소됨";
                    }
                }
            }
            IsConverting = false;
        }

        private void ClearFiles()
        {
            // 파일 목록 초기화 로직
            FileItems.Clear();
            StatusMessage = "파일 목록이 초기화되었습니다.";
        }

        private bool CanConvert() => FileItems.Count > 0 && !IsConverting;
        
        private bool CanCancel() => IsConverting;

        private void Controller_JobStatusChanged(object sender, ConversionJob job)
        {
            // 작업 상태 변경 이벤트 처리 로직
            if (job != null)
            {
                // 현재 작업 갱신
                _currentJob = job;
                
                // 진행률 업데이트
                ProgressValue = job.GetProgress() * 100;
                
                // 파일별 상태 업데이트
                UpdateFileStatus(job);
                
                // 상태에 따른 UI 갱신
                switch (job.Status)
                {
                    case JobStatus.InProgress:
                        IsConverting = true;
                        StatusMessage = $"변환 중... ({job.CompletedFiles}/{job.TotalFiles})";
                        break;
                        
                    case JobStatus.Completed:
                        IsConverting = false;
                        StatusMessage = $"변환 완료. 성공: {job.CompletedFiles}, 실패: {job.FailedFiles}";
                        break;
                        
                    case JobStatus.Failed:
                        IsConverting = false;
                        StatusMessage = "변환 실패";
                        break;
                        
                    case JobStatus.Cancelled:
                        IsConverting = false;
                        StatusMessage = "변환 취소됨";
                        break;
                }
            }
        }
        
        private void UpdateFileStatus(ConversionJob job)
        {
            // 변환 상태에 따라 파일별 상태 업데이트
            if (job.ProcessedFiles != null)
            {
                foreach (var processedFile in job.ProcessedFiles)
                {
                    var fileItem = FileItems.FirstOrDefault(f => f.FilePath == processedFile.FilePath);
                    if (fileItem != null)
                    {
                        fileItem.Status = processedFile.Success ? "성공" : $"실패: {processedFile.ErrorMessage}";
                        fileItem.IsConverted = processedFile.Success;
                    }
                }
            }
            
            if (job.CurrentFile != null)
            {
                var currentItem = FileItems.FirstOrDefault(f => f.FilePath == job.CurrentFile);
                if (currentItem != null)
                {
                    currentItem.Status = "변환 중";
                }
            }
        }

        private void Controller_LogMessage(object sender, string message)
        {
            // 로그 메시지 이벤트 처리 로직
            StatusMessage = message;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// ICommand 간단 구현체
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }
} 