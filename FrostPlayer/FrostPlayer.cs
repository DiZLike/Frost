using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Opus;
using Timer = System.Windows.Forms.Timer;

namespace FrostPlayer
{
    public partial class FrostPlayer : Form
    {
        private string currentFile;
        private int stream;
        private bool isPlaying;
        private bool isPaused;
        private double currentPosition;
        private List<string> playlist = new List<string>();
        private int currentTrackIndex = -1;
        private Timer progressTimer;

        // Для поддержки opus
        private const int BASS_STREAM_PRESCAN = 0x20000;

        public FrostPlayer()
        {
            InitializeComponent();
            InitializeBass();
            InitializeTimer();
            SetupEventHandlers();
            LoadPlaylist();
        }

        private void InitializeBass()
        {
            // Инициализация BASS
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                MessageBox.Show("Ошибка инициализации BASS!");
                return;
            }
        }

        private void InitializeTimer()
        {
            progressTimer = new Timer();
            progressTimer.Interval = 100; // Обновление каждые 100 мс
            progressTimer.Tick += ProgressTimer_Tick;
        }

        private void SetupEventHandlers()
        {
            // Обработка перетаскивания файлов
            this.AllowDrop = true;
            this.DragEnter += FrostPlayer_DragEnter;
            this.DragDrop += FrostPlayer_DragDrop;
        }

        private void FrostPlayer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FrostPlayer_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddToPlaylist(files);
        }

        private void AddToPlaylist(string[] files)
        {
            foreach (string file in files)
            {
                if (IsSupportedFormat(file) && !playlist.Contains(file))
                {
                    playlist.Add(file);
                    playlistListBox.Items.Add(Path.GetFileName(file));
                }
            }

            if (playlist.Count > 0 && currentTrackIndex == -1)
            {
                currentTrackIndex = 0;
            }

            SavePlaylist();
        }

        private bool IsSupportedFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp3" || ext == ".wav" || ext == ".flac" ||
                   ext == ".ogg" || ext == ".opus" || ext == ".m4a";
        }

        private void LoadFile(string filePath)
        {
            Stop();

            currentFile = filePath;

            // Определяем флаги в зависимости от формата
            BASSFlag flags = BASSFlag.BASS_DEFAULT;
            if (Path.GetExtension(filePath).ToLower() == ".opus")
            {
                flags |= (BASSFlag)BASS_STREAM_PRESCAN;
            }

            stream = Bass.BASS_StreamCreateFile(filePath, 0, 0, flags);

            if (stream != 0)
            {
                UpdateTrackInfo();
                Play();
            }
            else
            {
                MessageBox.Show($"Ошибка загрузки файла: {Bass.BASS_ErrorGetCode()}");
            }
        }

        private void UpdateTrackInfo()
        {
            if (stream != 0 && !string.IsNullOrEmpty(currentFile))
            {
                // Получаем длительность трека
                long length = Bass.BASS_ChannelGetLength(stream);
                double duration = Bass.BASS_ChannelBytes2Seconds(stream, length);

                // Обновляем информацию в UI
                this.Invoke(new Action(() =>
                {
                    trackInfoLabel.Text = $"Трек: {Path.GetFileName(currentFile)}";
                    durationLabel.Text = $"Длительность: {TimeSpan.FromSeconds(duration):mm\\:ss}";
                    progressBar.Maximum = (int)(duration * 1000); // в миллисекундах
                    progressBar.Value = 0;
                }));
            }
        }

        private void Play()
        {
            if (stream != 0)
            {
                if (isPaused)
                {
                    Bass.BASS_ChannelPlay(stream, false);
                }
                else
                {
                    Bass.BASS_ChannelPlay(stream, true);
                }

                isPlaying = true;
                isPaused = false;
                progressTimer.Start();

                UpdatePlayPauseButton();
            }
        }

        private void Pause()
        {
            if (isPlaying && stream != 0)
            {
                Bass.BASS_ChannelPause(stream);
                isPlaying = false;
                isPaused = true;
                progressTimer.Stop();
                UpdatePlayPauseButton();
            }
        }

        private void Stop()
        {
            if (stream != 0)
            {
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream);
                stream = 0;
            }

            isPlaying = false;
            isPaused = false;
            progressTimer.Stop();

            this.Invoke(new Action(() =>
            {
                progressBar.Value = 0;
                currentTimeLabel.Text = "00:00";
                UpdatePlayPauseButton();
            }));
        }

        private void Seek(int milliseconds)
        {
            if (stream != 0)
            {
                double position = Bass.BASS_ChannelSeconds2Bytes(stream, milliseconds / 1000.0);
                Bass.BASS_ChannelSetPosition(stream, position);

                if (!isPlaying && !isPaused)
                {
                    Play();
                }
            }
        }

        private void SetVolume(int volume)
        {
            if (stream != 0)
            {
                float vol = volume / 100f;
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, vol);
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (stream != 0 && isPlaying)
            {
                long position = Bass.BASS_ChannelGetPosition(stream);
                double currentTime = Bass.BASS_ChannelBytes2Seconds(stream, position);
                long length = Bass.BASS_ChannelGetLength(stream);
                double duration = Bass.BASS_ChannelBytes2Seconds(stream, length);

                this.Invoke(new Action(() =>
                {
                    progressBar.Value = (int)(currentTime * 1000);
                    currentTimeLabel.Text = $"{TimeSpan.FromSeconds(currentTime):mm\\:ss}";

                    // Если трек закончился, играем следующий
                    if (currentTime >= duration && duration > 0)
                    {
                        PlayNext();
                    }
                }));
            }
        }

        private void PlayNext()
        {
            if (playlist.Count > 0)
            {
                currentTrackIndex = (currentTrackIndex + 1) % playlist.Count;
                LoadFile(playlist[currentTrackIndex]);
                UpdatePlaylistSelection();
            }
        }

        private void PlayPrevious()
        {
            if (playlist.Count > 0)
            {
                currentTrackIndex = (currentTrackIndex - 1 + playlist.Count) % playlist.Count;
                LoadFile(playlist[currentTrackIndex]);
                UpdatePlaylistSelection();
            }
        }

        private void UpdatePlaylistSelection()
        {
            if (playlistListBox.InvokeRequired)
            {
                playlistListBox.Invoke(new Action(UpdatePlaylistSelection));
                return;
            }

            if (currentTrackIndex >= 0 && currentTrackIndex < playlistListBox.Items.Count)
            {
                playlistListBox.SelectedIndex = currentTrackIndex;
            }
        }

        private void UpdatePlayPauseButton()
        {
            if (playButton.InvokeRequired)
            {
                playButton.Invoke(new Action(UpdatePlayPauseButton));
                return;
            }

            playButton.Text = isPlaying ? "⏸️ Пауза" : "▶️ Воспр.";
        }

        private void SavePlaylist()
        {
            try
            {
                string playlistPath = Path.Combine(Application.StartupPath, "playlist.json");
                File.WriteAllText(playlistPath, Newtonsoft.Json.JsonConvert.SerializeObject(playlist));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения плейлиста: {ex.Message}");
            }
        }

        private void LoadPlaylist()
        {
            try
            {
                string playlistPath = Path.Combine(Application.StartupPath, "playlist.json");
                if (File.Exists(playlistPath))
                {
                    playlist = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(
                        File.ReadAllText(playlistPath));

                    foreach (var file in playlist)
                    {
                        if (File.Exists(file))
                        {
                            playlistListBox.Items.Add(Path.GetFileName(file));
                        }
                    }

                    if (playlist.Count > 0)
                    {
                        currentTrackIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки плейлиста: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Stop();
            Bass.BASS_Free();
            base.OnFormClosing(e);
        }

        // Обработчики событий UI
        private void playButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFile) && playlist.Count > 0)
            {
                LoadFile(playlist[0]);
            }
            else if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void prevButton_Click(object sender, EventArgs e)
        {
            PlayPrevious();
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            PlayNext();
        }

        private void volumeTrackBar_Scroll(object sender, EventArgs e)
        {
            SetVolume(volumeTrackBar.Value);
            volumeLabel.Text = $"Громкость: {volumeTrackBar.Value}%";
        }

        private void progressBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (stream != 0)
            {
                float percent = (float)e.X / progressBar.Width;
                int newPosition = (int)(percent * progressBar.Maximum);
                Seek(newPosition);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Аудио файлы|*.mp3;*.wav;*.flac;*.ogg;*.opus;*.m4a|Все файлы|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    AddToPlaylist(openFileDialog.FileNames);
                }
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (playlistListBox.SelectedIndex != -1)
            {
                int selectedIndex = playlistListBox.SelectedIndex;
                playlist.RemoveAt(selectedIndex);
                playlistListBox.Items.RemoveAt(selectedIndex);

                if (selectedIndex == currentTrackIndex)
                {
                    Stop();
                    currentTrackIndex = -1;
                }
                else if (selectedIndex < currentTrackIndex)
                {
                    currentTrackIndex--;
                }

                SavePlaylist();
            }
        }

        private void playlistListBox_DoubleClick(object sender, EventArgs e)
        {
            if (playlistListBox.SelectedIndex != -1)
            {
                currentTrackIndex = playlistListBox.SelectedIndex;
                LoadFile(playlist[currentTrackIndex]);
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            playlist.Clear();
            playlistListBox.Items.Clear();
            Stop();
            currentTrackIndex = -1;
            SavePlaylist();
        }
    }
}