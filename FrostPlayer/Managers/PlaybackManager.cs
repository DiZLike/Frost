using FrostPlayer.Models;
using FrostPlayer.Services;
using System;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace FrostPlayer.Managers
{
    public class PlaybackManager
    {
        private readonly AudioService _audioService;
        private readonly Timer _progressTimer;

        public Playlist CurrentPlaylist { get; set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public string CurrentFile { get; private set; }

        public event Action<double, double> PlaybackProgressChanged;
        public event Action<string> TrackChanged;
        public event Action PlaybackStarted;
        public event Action PlaybackPaused;
        public event Action PlaybackStopped;

        public PlaybackManager(AudioService audioService)
        {
            _audioService = audioService;
            CurrentPlaylist = new Playlist();

            _progressTimer = new Timer();
            _progressTimer.Interval = 100;
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        public void LoadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            Stop();

            CurrentFile = filePath;

            if (_audioService.LoadFile(filePath) != 0)
            {
                TrackChanged?.Invoke(CurrentFile);
            }
        }

        public void Play()
        {
            if (string.IsNullOrEmpty(CurrentFile) &&
                CurrentPlaylist.FilePaths.Count > 0 &&
                CurrentPlaylist.CurrentTrackIndex >= 0)
            {
                LoadFile(CurrentPlaylist.FilePaths[CurrentPlaylist.CurrentTrackIndex]);
            }

            if (_audioService.Play())
            {
                IsPlaying = true;
                IsPaused = false;
                _progressTimer.Start();
                PlaybackStarted?.Invoke();
            }
        }

        public void Pause()
        {
            if (_audioService.Pause())
            {
                IsPlaying = false;
                IsPaused = true;
                _progressTimer.Stop();
                PlaybackPaused?.Invoke();
            }
        }

        public void Stop()
        {
            if (_audioService.Stop())
            {
                IsPlaying = false;
                IsPaused = false;
                _progressTimer.Stop();
                PlaybackStopped?.Invoke();
            }
        }

        public void Seek(int milliseconds)
        {
            _audioService.SetPosition(milliseconds / 1000.0);

            if (!IsPlaying && !IsPaused)
            {
                Play();
            }
        }

        public void SetVolume(int volume)
        {
            _audioService.SetVolume(volume / 100f);
        }

        public void PlayNext()
        {
            if (CurrentPlaylist.FilePaths.Count == 0) return;

            CurrentPlaylist.CurrentTrackIndex =
                (CurrentPlaylist.CurrentTrackIndex + 1) % CurrentPlaylist.FilePaths.Count;

            LoadFile(CurrentPlaylist.FilePaths[CurrentPlaylist.CurrentTrackIndex]);
            Play();
        }

        public void PlayPrevious()
        {
            if (CurrentPlaylist.FilePaths.Count == 0) return;

            CurrentPlaylist.CurrentTrackIndex =
                (CurrentPlaylist.CurrentTrackIndex - 1 + CurrentPlaylist.FilePaths.Count) %
                CurrentPlaylist.FilePaths.Count;

            LoadFile(CurrentPlaylist.FilePaths[CurrentPlaylist.CurrentTrackIndex]);
            Play();
        }

        public void AddToPlaylist(string[] files)
        {
            foreach (var file in files)
            {
                CurrentPlaylist.AddTrack(file);
            }

            if (CurrentPlaylist.FilePaths.Count > 0 && CurrentPlaylist.CurrentTrackIndex == -1)
            {
                CurrentPlaylist.CurrentTrackIndex = 0;
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_audioService.IsPlaying())
            {
                var currentTime = _audioService.GetPosition();
                var duration = _audioService.GetDuration();

                PlaybackProgressChanged?.Invoke(currentTime, duration);

                // Если трек закончился, играем следующий
                if (currentTime >= duration && duration > 0)
                {
                    PlayNext();
                }
            }
        }

        public void Dispose()
        {
            _progressTimer.Stop();
            _progressTimer.Dispose();
            _audioService.Dispose();
        }
    }
}