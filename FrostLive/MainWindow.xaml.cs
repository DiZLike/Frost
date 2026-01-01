using FrostLive.Services;
using FrostLive.ViewModels;
using System.Windows;

namespace FrostLive
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            UpdateService.CheckAndUpdate();
            InitializeComponent();
            // Устанавливаем ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Подписываемся на закрытие окна для очистки ресурсов
            Closing += MainWindow_Closing;
            this.Title = $"{this.Title} {UpdateService.GetAssemblyVersion()}";
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Очищаем ресурсы ViewModel
            _viewModel.Cleanup();
        }
    }
}