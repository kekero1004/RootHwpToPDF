using System;
using System.Windows;
using System.Windows.Controls;
using HwpToPdf.Controllers;
using HwpToPdf.Models;
using HwpToPdf.ViewModels;

namespace HwpToPdf.Views.WPF
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window, IView
    {
        private readonly MainViewModel _viewModel;
        private readonly ConversionController _controller;

        public MainWindow(ConversionController controller)
        {
            this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
            
            // XAML 초기화
            InitializeComponent();
            
            _viewModel = new MainViewModel(controller);
            DataContext = _viewModel;
            
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // 이벤트 핸들러 등록
        }

        public void ShowMessage(string message)
        {
            // 메시지 표시 로직
            MessageBox.Show(message);
        }

        public void ShowError(string error)
        {
            // 오류 표시 로직
            MessageBox.Show(error, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void UpdateProgress(double progress, string status)
        {
            // 진행 상황 업데이트 로직
            Dispatcher.Invoke(() => {
                // UI 업데이트
            });
        }

        public void UpdateJobStatus(ConversionJob job)
        {
            // 작업 상태 업데이트 로직
            Dispatcher.Invoke(() => {
                // UI 업데이트
            });
        }

        public void FileSelectionCompleted(string[] files)
        {
            // 파일 선택 완료 처리 로직
        }

        public void ConversionCompleted(ConversionJob job)
        {
            // 변환 완료 처리 로직
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 윈도우 로드 로직
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 윈도우 종료 로직
        }
    }
} 