using FrostPlayer.Controls;
using FrostPlayer.Managers;
using FrostPlayer.Models;
using FrostPlayer.Services;
using FrostPlayer.Utilities;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FrostPlayer
{
    public partial class MainForm : Form
    {
        private readonly PlaybackManager _playbackManager;
        private readonly PlaylistService _playlistService;
        private readonly TagService _tagService;
        public MainForm()
        {
            InitializeComponent();

            // Инициализация сервисов
            var audioService = new AudioService();
            _playbackManager = new PlaybackManager(audioService);
            _playlistService = new PlaylistService();
            _tagService = new TagService();

            // Загрузка плейлиста
            var playlist = _playlistService.LoadPlaylist();
            _playbackManager.CurrentPlaylist = playlist;

            // Настройка обработчиков событий
            SetupEventHandlers();
            SetupDragDrop();
            LoadPlaylistToControl();

            // Подписка на события менеджера воспроизведения
            _playbackManager.PlaybackProgressChanged += OnPlaybackProgressChanged;
            _playbackManager.TrackChanged += OnTrackChanged;
            _playbackManager.PlaybackStarted += OnPlaybackStarted;
            _playbackManager.PlaybackPaused += OnPlaybackPaused;
            _playbackManager.PlaybackStopped += OnPlaybackStopped;

            // Инициализация UI
            UpdatePlayPauseButton();
        }

        private void SetupEventHandlers()
        {
            // Обработчики кнопок
            playButton.Click += PlayButton_Click;
            stopButton.Click += StopButton_Click;
            prevButton.Click += PrevButton_Click;
            nextButton.Click += NextButton_Click;

            // Обработчики других элементов
            progressBar.MouseDown += ProgressBar_MouseDown;
            addButton.Click += AddButton_Click;
            removeButton.Click += RemoveButton_Click;
            clearButton.Click += ClearButton_Click;

            // Обработчики событий контрола плейлиста
            playlist.ItemDoubleClick += Playlist_ItemDoubleClick;
            playlist.SelectionChanged += Playlist_SelectionChanged;
            playlist.ColumnClick += Playlist_ColumnClick;
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            // Также разрешаем перетаскивание на сам контрол плейлиста
            playlist.AllowDrop = true;
            playlist.DragEnter += Playlist_DragEnter;
            playlist.DragDrop += Playlist_DragDrop;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ProcessDroppedFiles(files);
        }

        private void Playlist_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Playlist_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ProcessDroppedFiles(files);
        }

        private void ProcessDroppedFiles(string[] files)
        {
            var audioFiles = FileHelper.FilterAudioFiles(files);

            // Добавляем файлы в плейлист
            _playbackManager.AddToPlaylist(audioFiles);

            // Сохраняем плейлист
            _playlistService.SavePlaylist(_playbackManager.CurrentPlaylist);

            // Загружаем обновленный плейлист с метаданными
            LoadPlaylistToControl();
        }

        private void LoadPlaylistToControl()
        {
            playlist.Clear();

            // Создаем элементы плейлиста с информацией о треках
            var playlistItems = new System.Collections.Generic.List<PlaylistItem>();

            for (int i = 0; i < _playbackManager.CurrentPlaylist.FilePaths.Count; i++)
            {
                var filePath = _playbackManager.CurrentPlaylist.FilePaths[i];
                var item = CreatePlaylistItem(filePath, i);
                playlistItems.Add(item);
            }

            playlist.AddRange(playlistItems);

            // Обновляем индикатор текущего трека
            if (_playbackManager.CurrentPlaylist.CurrentTrackIndex >= 0 &&
                _playbackManager.CurrentPlaylist.CurrentTrackIndex < playlist.Items.Count)
            {
                playlist.SelectedIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
                playlist.PlayingIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
            }
        }

        private PlaylistItem CreatePlaylistItem(string filePath, int index)
        {
            try
            {
                // Получаем полную информацию о треке через TagService
                var audioTrack = _tagService.GetTrackInfo(filePath);

                // Создаем элемент плейлиста с полученными данными
                return new PlaylistItem
                {
                    Index = index + 1,
                    Artist = audioTrack.Artist,
                    Album = audioTrack.Album,
                    Title = audioTrack.Title,
                    Duration = TimeSpan.FromSeconds(audioTrack.Duration),
                    FilePath = filePath
                };
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, создаем элемент с базовой информацией
                Console.WriteLine($"Ошибка создания элемента плейлиста: {ex.Message}");

                return new PlaylistItem
                {
                    Index = index + 1,
                    Artist = "Неизвестный исполнитель",
                    Album = "Неизвестный альбом",
                    Title = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    Duration = TimeSpan.Zero,
                    FilePath = filePath
                };
            }
        }

        // Обработчики событий PlaybackManager
        private void OnPlaybackProgressChanged(double currentTime, double duration)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnPlaybackProgressChanged(currentTime, duration)));
                return;
            }

            progressBar.Maximum = (int)(duration * 1000);
            int value = (int)(currentTime * 1000);
            progressBar.Value = value <= progressBar.Maximum ? value : progressBar.Maximum;
            currentTimeLabel.Text = $"{TimeSpan.FromSeconds(currentTime):mm\\:ss}";

            if (duration > 0)
            {
                durationLabel.Text = $"{TimeSpan.FromSeconds(duration):mm\\:ss}";
            }
        }

        private void OnTrackChanged(string filePath)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnTrackChanged(filePath)));
                return;
            }

            trackInfoLabel.Text = $"Трек: {Path.GetFileName(filePath)}";

            // Обновляем индикатор воспроизведения в плейлисте
            int index = _playbackManager.CurrentPlaylist.FilePaths.IndexOf(filePath);
            if (index >= 0)
            {
                playlist.PlayingIndex = index;
                playlist.SelectedIndex = index;
                playlist.Invalidate();
            }
        }

        private void OnPlaybackStarted()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnPlaybackStarted));
                return;
            }

            UpdatePlayPauseButton();

            // Обновляем индикатор воспроизведения
            if (_playbackManager.CurrentPlaylist.CurrentTrackIndex >= 0)
            {
                playlist.PlayingIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
            }
        }

        private void OnPlaybackPaused()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnPlaybackPaused));
                return;
            }

            UpdatePlayPauseButton();
        }

        private void OnPlaybackStopped()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnPlaybackStopped));
                return;
            }

            progressBar.Value = 0;
            currentTimeLabel.Text = "00:00";
            playlist.PlayingIndex = -1;
            UpdatePlayPauseButton();
        }

        // Методы UI
        private void UpdatePlayPauseButton()
        {
            if (playButton.InvokeRequired)
            {
                playButton.Invoke(new Action(UpdatePlayPauseButton));
                return;
            }

            playButton.Text = _playbackManager.IsPlaying ? "⏸️ Пауза" : "▶️ Воспр.";
        }

        // Обработчики UI событий
        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (_playbackManager.CurrentPlaylist.FilePaths.Count > 0 &&
                string.IsNullOrEmpty(_playbackManager.CurrentFile))
            {
                if (_playbackManager.CurrentPlaylist.CurrentTrackIndex >= 0)
                {
                    _playbackManager.LoadFile(_playbackManager.CurrentPlaylist
                        .FilePaths[_playbackManager.CurrentPlaylist.CurrentTrackIndex]);
                }
                else if (playlist.SelectedIndex >= 0)
                {
                    // Если трек не выбран, но есть выделенный в плейлисте
                    _playbackManager.CurrentPlaylist.CurrentTrackIndex = playlist.SelectedIndex;
                    _playbackManager.LoadFile(_playbackManager.CurrentPlaylist
                        .FilePaths[playlist.SelectedIndex]);
                }
            }

            if (_playbackManager.IsPlaying)
            {
                _playbackManager.Pause();
            }
            else
            {
                _playbackManager.Play();
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            _playbackManager.Stop();
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            _playbackManager.PlayPrevious();

            // Обновляем выделение в плейлисте
            if (_playbackManager.CurrentPlaylist.CurrentTrackIndex >= 0)
            {
                playlist.SelectedIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
                playlist.PlayingIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            _playbackManager.PlayNext();

            // Обновляем выделение в плейлисте
            if (_playbackManager.CurrentPlaylist.CurrentTrackIndex >= 0)
            {
                playlist.SelectedIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
                playlist.PlayingIndex = _playbackManager.CurrentPlaylist.CurrentTrackIndex;
            }
        }

        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            _playbackManager.SetVolume(volumeControl1.Value);
        }

        private void ProgressBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (_playbackManager.CurrentPlaylist.FilePaths.Count == 0) return;

            float percent = (float)e.X / progressBar.Width;
            int newPosition = (int)(percent * progressBar.Maximum);
            _playbackManager.Seek(newPosition);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Аудио файлы|*.mp3;*.wav;*.flac;*.ogg;*.opus;*.m4a|Все файлы|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _playbackManager.AddToPlaylist(openFileDialog.FileNames);
                    _playlistService.SavePlaylist(_playbackManager.CurrentPlaylist);
                    LoadPlaylistToControl();
                }
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (playlist.SelectedItem != null)
            {
                // Находим файл по выбранному элементу
                string filePath = playlist.SelectedItem.FilePath;
                int index = _playbackManager.CurrentPlaylist.FilePaths.IndexOf(filePath);

                if (index >= 0)
                {
                    _playbackManager.CurrentPlaylist.RemoveTrack(index);
                    _playlistService.SavePlaylist(_playbackManager.CurrentPlaylist);
                    LoadPlaylistToControl();

                    // Если удалили текущий трек, останавливаем воспроизведение
                    if (index == _playbackManager.CurrentPlaylist.CurrentTrackIndex)
                    {
                        _playbackManager.Stop();
                    }
                }
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            _playbackManager.CurrentPlaylist.Clear();
            _playlistService.SavePlaylist(_playbackManager.CurrentPlaylist);
            LoadPlaylistToControl();
            _playbackManager.Stop();
        }

        // Обработчики событий контрола плейлиста
        private void Playlist_ItemDoubleClick(object sender, PlaylistItemEventArgs e)
        {
            if (e.Item != null)
            {
                _playbackManager.CurrentPlaylist.CurrentTrackIndex = e.Index;
                _playbackManager.LoadFile(e.Item.FilePath);
                _playbackManager.Play();

                // Обновляем индикаторы
                playlist.PlayingIndex = e.Index;
                playlist.SelectedIndex = e.Index;
            }
        }

        private void Playlist_SelectionChanged(object sender, PlaylistItemEventArgs e)
        {
            // Просто обновляем выделение, воспроизведение начинается только по двойному клику
            // или кнопке Play
        }

        private void Playlist_ColumnClick(object sender, ColumnHeaderEventArgs e)
        {
            // Здесь можно добавить дополнительную логику при клике на заголовок столбца
            // Например, показать меню с опциями сортировки
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _playlistService.SavePlaylist(_playbackManager.CurrentPlaylist);
            _playbackManager.Dispose();
            base.OnFormClosing(e);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Раскомментируйте, если нужно динамическое изменение размеров
            // UpdateControlSizes();
        }

        private void progressBar_MouseMove(object sender, MouseEventArgs e)
        {
            // Обработка перемещения мыши над прогресс-баром
        }
    }
}