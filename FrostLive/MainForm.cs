using FrostLive.Controls;
using FrostLive.Models;
using FrostLive.Services;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace FrostLive
{
    public partial class MainForm : Form
    {
        private BassAudioService _audioService;
        private RadioApiService _apiService;
        private AppConfig _config;
        private System.Timers.Timer _historyTimer;
        private System.Timers.Timer _updateTimer;
        private Timer _peakMeterTimer;

        private ObservableCollection<RadioTrack> _historyTracks = new ObservableCollection<RadioTrack>();
        private ObservableCollection<RadioTrack> _newTracks = new ObservableCollection<RadioTrack>();

        private double _leftPeakValue = 0;
        private double _rightPeakValue = 0;

        public MainForm()
        {
            InitializeComponent(); // Теперь используется дизайнер
            InitializeServices();
            SetupEventHandlers();

            UpdateService.CheckAndUpdate();
            this.Text = $"FrostLive - Retro Pub.radio {UpdateService.GetAssemblyVersion()}";

            LoadDataAsync();
        }

        private void InitializeServices()
        {
            _config = new AppConfig();
            _audioService = new BassAudioService();
            _apiService = new RadioApiService(_config);

            _historyTimer = new System.Timers.Timer(10000);
            _historyTimer.Elapsed += async (s, e) => await RefreshHistoryAsync();

            _updateTimer = new System.Timers.Timer(1000);
            _updateTimer.Elapsed += (s, e) => UpdateCurrentTime();

            _peakMeterTimer = new Timer();
            _peakMeterTimer.Interval = 30; // ~33 FPS
            _peakMeterTimer.Tick += PeakMeterTimer_Tick;
        }

        private void SetupEventHandlers()
        {
            playerControl1.PlayPauseClicked += PlayerControl_PlayPauseClicked;
            playerControl1.VolumeChanged += PlayerControl_VolumeChanged;

            _historyControl.RefreshClicked += async (s, e) => await RefreshHistoryAsync();
            _historyControl.DownloadClicked += HistoryControl_DownloadClicked;

            _newTracksControl.DownloadClicked += NewTracksControl_DownloadClicked;

            _audioService.StatusChanged += OnStatusChanged;
            _audioService.PlaybackStateChanged += OnPlaybackStateChanged;
            _audioService.CurrentSongChanged += OnCurrentSongChanged;
        }

        private async void LoadDataAsync()
        {
            try
            {
                await RefreshHistoryAsync();
                await LoadNewTracksAsync();

                if (_config.AutoPlay)
                {
                    _audioService.PlayStream(_config.RadioStreamUrl);
                }

                _historyTimer.Start();
                _updateTimer.Start();
                _peakMeterTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task RefreshHistoryAsync()
        {
            try
            {
                var history = await _apiService.GetHistoryAsync();

                this.Invoke(new Action(() =>
                {
                    _historyTracks.Clear();
                    foreach (var track in history)
                    {
                        _historyTracks.Add(track);
                    }

                    _historyControl.Tracks = _historyTracks;

                    if (history.Count > 0)
                    {
                        playerControl1.CurrentSong = history[0].DisplayTitle;
                        _audioService.UpdateCurrentSong(history[0].DisplayTitle);
                    }
                }));
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

                this.Invoke(new Action(() =>
                {
                    _newTracks.Clear();
                    foreach (var track in tracks)
                    {
                        _newTracks.Add(track);
                    }

                    _newTracksControl.Tracks = _newTracks;
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load new tracks error: {ex.Message}");
            }
        }

        private void PlayerControl_PlayPauseClicked(object sender, EventArgs e)
        {
            if (_audioService.IsPlaying)
            {
                _audioService.Pause();
            }
            else
            {
                _audioService.Resume();
            }
        }

        private void PlayerControl_VolumeChanged(object sender, double volume)
        {
            _audioService.SetVolume((int)volume);
        }

        private void OnStatusChanged(string status)
        {
            this.Invoke(new Action(() =>
            {
                playerControl1.PlayerStatus = status;
            }));
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            this.Invoke(new Action(() =>
            {
                playerControl1.IsPlaying = isPlaying;
            }));
        }

        private void OnCurrentSongChanged(string song)
        {
            this.Invoke(new Action(() =>
            {
                playerControl1.CurrentSong = song;
            }));
        }

        private void UpdateCurrentTime()
        {
            if (_audioService.IsPlaying)
            {
                this.Invoke(new Action(() =>
                {
                    playerControl1.CurrentTime = _audioService.GetPlaybackTime();
                }));
            }
        }

        private void PeakMeterTimer_Tick(object sender, EventArgs e)
        {
            if (_audioService.IsPlaying)
            {
                var levels = _audioService.GetLevels();

                const double smoothingFactor = 0.3;
                _leftPeakValue = _leftPeakValue * (1 - smoothingFactor) + levels.left * smoothingFactor;
                _rightPeakValue = _rightPeakValue * (1 - smoothingFactor) + levels.right * smoothingFactor;

                playerControl1.LeftPeakValue = _leftPeakValue;
                playerControl1.RightPeakValue = _rightPeakValue;
            }
        }

        private void HistoryControl_DownloadClicked(object sender, string url)
        {
            DownloadTrack(url);
        }

        private void NewTracksControl_DownloadClicked(object sender, string url)
        {
            DownloadTrack(url);
        }

        private void DownloadTrack(string url)
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
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _historyTimer?.Stop();
            _updateTimer?.Stop();
            _peakMeterTimer?.Stop();
            _audioService?.Dispose();

            base.OnFormClosing(e);
        }

        private void playerControl1_Load(object sender, EventArgs e)
        {

        }
    }
}