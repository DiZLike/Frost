using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using RadioStationManager.Configuration;
using RadioStationManager.Services;

namespace RadioStationManager
{
    public partial class MainForm : Form
    {
        private RadioConfig _config;
        private List<string> _selectedFiles = new List<string>();
        private bool _isProcessing = false;
        private OpenFileDialog openFileDialog1;

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();
            InitializeAudioFormats();
        }

        private void LoadConfig()
        {
            _config = ConfigManager.LoadConfig();
            txtServerUrl.Text = _config.ServerUrl;
            txtApiKey.Text = _config.ApiKey;
            txtServerPath.Text = _config.ServerPath;
            txtDownloadLink.Text = _config.DownloadLink;
            txtPlaylistPath.Text = _config.PlaylistPath;
        }

        private void InitializeAudioFormats()
        {
            openFileDialog1 = new OpenFileDialog();
            // Поддерживаемые форматы
            openFileDialog1.Filter = "Аудио файлы|*.mp3;*.opus;*.ogg;*.flac;*.wav;*.wma;*.m4a;*.aac|Все файлы|*.*";
            openFileDialog1.Multiselect = true;
        }

        private void btnSelectFiles_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedFiles.AddRange(openFileDialog1.FileNames);
                UpdateFileList();
            }
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = Directory.GetFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsSupportedAudioFile(f))
                        .ToArray();

                    _selectedFiles.AddRange(files);
                    UpdateFileList();
                }
            }
        }

        private bool IsSupportedAudioFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp3" || ext == ".opus" || ext == ".ogg" ||
                   ext == ".flac" || ext == ".wav" || ext == ".wma" ||
                   ext == ".m4a" || ext == ".aac";
        }

        private void UpdateFileList()
        {
            lstFiles.Items.Clear();
            foreach (var file in _selectedFiles)
            {
                lstFiles.Items.Add(Path.GetFileName(file));
            }
            lblFileCount.Text = $"Файлов: {_selectedFiles.Count}";
        }

        private void btnClearList_Click(object sender, EventArgs e)
        {
            _selectedFiles.Clear();
            UpdateFileList();
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Идет обработка, пожалуйста подождите", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show("Выберите файлы для обработки", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Сохраняем настройки
            SaveConfig();

            // Определяем режим работы
            if (rbPlaylistMode.Checked)
            {
                await ProcessPlaylistMode();
            }
            else if (rbServerMode.Checked)
            {
                await ProcessServerMode();
            }
        }

        private async Task ProcessPlaylistMode()
        {
            _isProcessing = true;
            btnProcess.Enabled = false;
            progressBar1.Value = 0;
            progressBar1.Maximum = _selectedFiles.Count;

            try
            {
                var processor = new PlaylistProcessor(_config);

                for (int i = 0; i < _selectedFiles.Count; i++)
                {
                    var file = _selectedFiles[i];

                    // Обновляем интерфейс
                    lblStatus.Text = $"Обработка: {Path.GetFileName(file)}";
                    progressBar1.Value = i + 1;
                    Application.DoEvents();

                    // Обрабатываем файл
                    processor.AddTrack(file);
                }

                // Сохраняем плейлист
                processor.Save(_config.PlaylistPath);

                MessageBox.Show($"Плейлист успешно создан!\nФайлов: {_selectedFiles.Count}",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnProcess.Enabled = true;
                lblStatus.Text = "Готово";
            }
        }

        private async Task ProcessServerMode()
        {
            _isProcessing = true;
            btnProcess.Enabled = false;
            progressBar1.Value = 0;
            progressBar1.Maximum = _selectedFiles.Count;

            try
            {
                var uploader = new ServerUploader(_config);
                int successCount = 0;
                int errorCount = 0;

                for (int i = 0; i < _selectedFiles.Count; i++)
                {
                    var file = _selectedFiles[i];

                    // Обновляем интерфейс
                    lblStatus.Text = $"Загрузка: {Path.GetFileName(file)}";
                    progressBar1.Value = i + 1;
                    Application.DoEvents();

                    try
                    {
                        var result = await uploader.UploadFileAsync(file);
                        if (result.Success)
                        {
                            successCount++;
                            LogMessage($"✓ {Path.GetFileName(file)} - успешно");
                        }
                        else
                        {
                            errorCount++;
                            LogMessage($"✗ {Path.GetFileName(file)} - {result.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogMessage($"✗ {Path.GetFileName(file)} - {ex.Message}");
                    }
                }

                MessageBox.Show($"Обработка завершена!\nУспешно: {successCount}\nОшибок: {errorCount}",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnProcess.Enabled = true;
                lblStatus.Text = "Готово";
            }
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            txtLog.ScrollToCaret();
        }

        private void SaveConfig()
        {
            _config.ServerUrl = txtServerUrl.Text;
            _config.ApiKey = txtApiKey.Text;
            _config.ServerPath = txtServerPath.Text;
            _config.DownloadLink = txtDownloadLink.Text;
            _config.PlaylistPath = txtPlaylistPath.Text;

            ConfigManager.SaveConfig(_config);
        }

        private void btnBrowsePlaylist_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PLS Playlist|*.pls|Все файлы|*.*";
                saveDialog.FileName = "playlist.pls";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPlaylistPath.Text = saveDialog.FileName;
                }
            }
        }

        private void rbMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isServerMode = rbServerMode.Checked;

            txtServerUrl.Enabled = isServerMode;
            txtApiKey.Enabled = isServerMode;
            txtDownloadLink.Enabled = isServerMode;
            lblServerUrl.Enabled = isServerMode;
            lblApiKey.Enabled = isServerMode;
            lblDownloadLink.Enabled = isServerMode;
        }
    }
}