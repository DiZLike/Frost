using FrostLive.Models;
using FrostLive.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FrostLive.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BassAudioService _audioService;
        private readonly RadioApiService _apiService;
        private readonly Timer _historyTimer;
        private readonly Timer _songTimer;
        private readonly Dispatcher _dispatcher;

        private string _currentSong = "Loading...";
        private string _playerStatus = "CONNECTING";
        private bool _isPlaying;
        private int _volume = 80;
        private string _currentTime = "00:00";
        private string _playPauseButtonText = "▶";
        private ObservableCollection<RadioTrack> _historyTracks = new ObservableCollection<RadioTrack>();
        private ObservableCollection<RadioTrack> _newTracks = new ObservableCollection<RadioTrack>();

        private double _leftPeakValue = 0;
        private double _rightPeakValue = 0;
        private EventHandler _renderingHandler;
        private DateTime _lastPeakUpdate;
        private double _peakUpdateIntervalMs = 20; // По умолчанию 40 FPS 25

        public event PropertyChangedEventHandler PropertyChanged;

        // Добавляем свойство для управления частотой кадров
        public double PeakMeterFrameRate
        {
            get => 1000.0 / _peakUpdateIntervalMs; // Конвертация из мс в FPS
            set
            {
                if (value > 0 && value <= 240) // Ограничение: от 1 до 240 FPS
                {
                    var newInterval = 1000.0 / value;
                    if (Math.Abs(_peakUpdateIntervalMs - newInterval) > 0.1)
                    {
                        _peakUpdateIntervalMs = newInterval;
                        OnPropertyChanged(nameof(PeakMeterFrameRate));
                        Console.WriteLine($"Peak meter frame rate set to {value:F1} FPS (interval: {_peakUpdateIntervalMs:F2} ms)");
                    }
                }
            }
        }

        public double LeftPeakValue
        {
            get { return _leftPeakValue; }
            set
            {
                if (Math.Abs(_leftPeakValue - value) > 0.001)
                {
                    _leftPeakValue = value;
                    OnPropertyChanged(nameof(LeftPeakValue));
                }
            }
        }

        public double RightPeakValue
        {
            get { return _rightPeakValue; }
            set
            {
                if (Math.Abs(_rightPeakValue - value) > 0.001)
                {
                    _rightPeakValue = value;
                    OnPropertyChanged(nameof(RightPeakValue));
                }
            }
        }

        public string CurrentSong
        {
            get { return _currentSong; }
            set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnPropertyChanged(nameof(CurrentSong));
                }
            }
        }

        public string PlayerStatus
        {
            get { return _playerStatus; }
            set
            {
                if (_playerStatus != value)
                {
                    _playerStatus = value;
                    OnPropertyChanged(nameof(PlayerStatus));
                }
            }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));

                    // Обновляем текст кнопки
                    PlayPauseButtonText = _isPlaying ? "❚❚" : "▶";

                    // Управляем анимацией пик-метра
                    if (_isPlaying)
                    {
                        StartPeakMeter();
                    }
                    else
                    {
                        StopPeakMeter();
                        LeftPeakValue = 0;
                        RightPeakValue = 0;
                    }
                }
            }
        }

        public int Volume
        {
            get { return _volume; }
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged(nameof(Volume));
                    _audioService.SetVolume(_volume);
                }
            }
        }

        public string CurrentTime
        {
            get { return _currentTime; }
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged(nameof(CurrentTime));
                }
            }
        }

        public string PlayPauseButtonText
        {
            get { return _playPauseButtonText; }
            set
            {
                if (_playPauseButtonText != value)
                {
                    _playPauseButtonText = value;
                    OnPropertyChanged(nameof(PlayPauseButtonText));
                }
            }
        }

        public ObservableCollection<RadioTrack> HistoryTracks
        {
            get { return _historyTracks; }
            set
            {
                if (_historyTracks != value)
                {
                    _historyTracks = value;
                    OnPropertyChanged(nameof(HistoryTracks));
                }
            }
        }

        public ObservableCollection<RadioTrack> NewTracks
        {
            get { return _newTracks; }
            set
            {
                if (_newTracks != value)
                {
                    _newTracks = value;
                    OnPropertyChanged(nameof(NewTracks));
                }
            }
        }

        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RefreshHistoryCommand { get; }
        public ICommand SetVolumeCommand { get; }
        public ICommand DownloadPlaylistCommand { get; }
        public ICommand DownloadTrackCommand { get; }
        public ICommand SetFrameRateCommand { get; }

        public MainViewModel()
        {
            _dispatcher = Application.Current.Dispatcher;

            var config = new AppConfig();
            _audioService = new BassAudioService();
            _apiService = new RadioApiService(config);

            SetupAudioServiceEvents();

            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            StopCommand = new RelayCommand(ExecuteStop);
            RefreshHistoryCommand = new RelayCommand(ExecuteRefreshHistory);
            SetVolumeCommand = new RelayCommand<int>(ExecuteSetVolume);
            DownloadPlaylistCommand = new RelayCommand(ExecuteDownloadPlaylist);
            DownloadTrackCommand = new RelayCommand<string>(ExecuteDownloadTrack);
            SetFrameRateCommand = new RelayCommand<double>(ExecuteSetFrameRate);

            // Таймеры для обновления
            _historyTimer = new Timer(10000); // Каждые 10 секунд
            _historyTimer.Elapsed += (s, e) => ExecuteRefreshHistory();
            _historyTimer.Start();

            _songTimer = new Timer(1000); // Каждую секунду
            _songTimer.Elapsed += (s, e) => UpdateCurrentTime();
            _songTimer.Start();

            // Начальная загрузка
            _dispatcher.BeginInvoke(new Action(() =>
            {
                ExecuteRefreshHistory();
                ExecuteLoadNewTracks();

                if (config.AutoPlay)
                {
                    _audioService.PlayStream(config.RadioStreamUrl);
                }
            }));
        }

        private void StartPeakMeter()
        {
            StopPeakMeter(); // Останавливаем предыдущую анимацию, если была

            _lastPeakUpdate = DateTime.Now;
            _renderingHandler = (sender, e) => OnRendering(sender, e);
            CompositionTarget.Rendering += _renderingHandler;
        }

        private void StopPeakMeter()
        {
            if (_renderingHandler != null)
            {
                CompositionTarget.Rendering -= _renderingHandler;
                _renderingHandler = null;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastPeakUpdate).TotalMilliseconds;

            // Обновляем пик-метр только если прошло достаточно времени
            if (elapsed >= _peakUpdateIntervalMs)
            {
                _lastPeakUpdate = now;
                UpdatePeakMeter();
            }
        }

        private void UpdatePeakMeter()
        {
            try
            {
                if (_audioService != null && _audioService.IsPlaying)
                {
                    var levels = _audioService.GetLevels();

                    // Плавная интерполяция для более приятной анимации
                    const double smoothingFactor = 0.3;
                    LeftPeakValue = LeftPeakValue * (1 - smoothingFactor) + levels.left * smoothingFactor;
                    RightPeakValue = RightPeakValue * (1 - smoothingFactor) + levels.right * smoothingFactor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Peak meter update error: {ex.Message}");
                LeftPeakValue = 0;
                RightPeakValue = 0;
            }
        }

        private void SetupAudioServiceEvents()
        {
            _audioService.StatusChanged += status => PlayerStatus = status;
            _audioService.PlaybackStateChanged += isPlaying => IsPlaying = isPlaying;
            _audioService.VolumeChanged += volume => Volume = volume;
            _audioService.CurrentSongChanged += song => CurrentSong = song;
        }

        private void ExecutePlayPause()
        {
            if (IsPlaying)
            {
                _audioService.Pause();
            }
            else
            {
                _audioService.Resume();
            }
        }

        private void ExecuteStop()
        {
            _audioService.Stop();
        }

        private void ExecuteRefreshHistory()
        {
            _dispatcher.Invoke((Action)(async () =>
            {
                await LoadHistoryAsync();
            }));
        }

        private void ExecuteLoadNewTracks()
        {
            _dispatcher.Invoke((Action)(async () =>
            {
                await LoadNewTracksAsync();
            }));
        }

        private void ExecuteSetFrameRate(double frameRate)
        {
            PeakMeterFrameRate = frameRate;
        }

        public void ExecuteSetVolume(int volume)
        {
            _audioService.SetVolume(volume);
        }

        private async System.Threading.Tasks.Task LoadHistoryAsync()
        {
            try
            {
                var history = await _apiService.GetHistoryAsync();

                _dispatcher.Invoke(() =>
                {
                    HistoryTracks.Clear();
                    foreach (var track in history)
                    {
                        HistoryTracks.Add(track);
                    }

                    if (history.Count > 0)
                    {
                        CurrentSong = history[0].DisplayTitle;
                        _audioService.UpdateCurrentSong(CurrentSong);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load history error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadNewTracksAsync()
        {
            try
            {
                var tracks = await _apiService.GetNewTracksAsync();

                _dispatcher.Invoke(() =>
                {
                    NewTracks.Clear();
                    foreach (var track in tracks)
                    {
                        NewTracks.Add(track);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load new tracks error: {ex.Message}");
            }
        }

        private void UpdateCurrentTime()
        {
            if (IsPlaying)
            {
                _dispatcher.Invoke(() =>
                {
                    CurrentTime = _audioService.GetPlaybackTime();
                });
            }
        }

        private void ExecuteDownloadPlaylist()
        {
            try
            {
                var playlistUrl = "http://r.dlike.ru:8000/live.m3u";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = playlistUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download playlist: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDownloadTrack(string url)
        {
            if (!string.IsNullOrEmpty(url) && url != "#")
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download track: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            _historyTimer.Stop();
            _songTimer.Stop();
            _audioService.Dispose();
            StopPeakMeter();
        }
    }

    // Простая реализация RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    // RelayCommand с параметром
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}