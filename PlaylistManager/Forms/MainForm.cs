using PlaylistManager.Models;
using PlaylistManager.Services;
using System;
using System.Windows.Forms;

namespace PlaylistManager.Forms
{
    public partial class MainForm : Form
    {
        private string _currentFilePath = "pls.json"; // Текущий открытый файл
        private readonly JsonFileService _jsonService;
        private readonly ScheduleValidator _validator;
        private ScheduleConfig _config;
        private BindingSource _bindingSource;
        private bool _isModified = false; // Флаг изменения данных

        public MainForm()
        {
            InitializeComponent();
            _jsonService = new JsonFileService();
            _validator = new ScheduleValidator();
            _bindingSource = new BindingSource();

            UpdateFormTitle();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Сначала настраиваем DataGridView
            SetupDataGridView();

            // Затем загружаем данные
            LoadSchedule(_currentFilePath);

            // Принудительно обновляем отображение
            dataGridView.Refresh();
        }

        private void LoadSchedule(string filePath)
        {
            try
            {
                _config = _jsonService.LoadSchedule(filePath);
                _currentFilePath = filePath;

                // Очищаем и переустанавливаем BindingSource
                _bindingSource.DataSource = null;
                _bindingSource.DataSource = _config.ScheduleItems;

                // Принудительно обновляем отображение
                dataGridView.Refresh();

                _isModified = false;
                UpdateFormTitle();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveSchedule(string filePath)
        {
            try
            {
                _jsonService.SaveSchedule(filePath, _config);
                _currentFilePath = filePath;
                _isModified = false;
                UpdateFormTitle();
                MessageBox.Show("Расписание сохранено успешно", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateFormTitle()
        {
            string fileName = Path.GetFileName(_currentFilePath);
            string modified = _isModified ? " *" : "";
            this.Text = $"Менеджер плейлистов - {fileName}{modified}";
        }
        // В методе SetupDataGridView() заменим колонки:
        private void SetupDataGridView()
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Настраиваем колонки с привязкой к реальным свойствам
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Название",
                Name = "colName",
                Width = 150
            };
            dataGridView.Columns.Add(colName);

            var colPath = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PlaylistPath",
                HeaderText = "Путь к плейлисту",
                Name = "colPath",
                Width = 250,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridView.Columns.Add(colPath);

            // Колонка для времени начала (не привязываем к DataPropertyName)
            var colStart = new DataGridViewTextBoxColumn
            {
                HeaderText = "Время начала",
                Name = "colStart",
                Width = 100,
                ReadOnly = true
            };
            dataGridView.Columns.Add(colStart);

            // Колонка для времени окончания (не привязываем к DataPropertyName)
            var colEnd = new DataGridViewTextBoxColumn
            {
                HeaderText = "Время окончания",
                Name = "colEnd",
                Width = 100,
                ReadOnly = true
            };
            dataGridView.Columns.Add(colEnd);

            // Колонка для дней недели (не привязываем к DataPropertyName)
            var colDays = new DataGridViewTextBoxColumn
            {
                HeaderText = "Дни недели",
                Name = "colDays",
                Width = 150,
                ReadOnly = true
            };
            dataGridView.Columns.Add(colDays);

            // Устанавливаем источник данных
            dataGridView.DataSource = _bindingSource;

            // Добавляем обработчик форматирования
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView.Rows.Count)
                return;

            var item = dataGridView.Rows[e.RowIndex].DataBoundItem as ScheduleItem;
            if (item == null)
                return;

            if (dataGridView.Columns[e.ColumnIndex].Name == "colStart")
            {
                // Форматируем время начала
                e.Value = $"{item.StartHour:D2}:{item.StartMinute:D2}";
                e.FormattingApplied = true;
            }
            else if (dataGridView.Columns[e.ColumnIndex].Name == "colEnd")
            {
                // Форматируем время окончания
                e.Value = $"{item.EndHour:D2}:{item.EndMinute:D2}";
                e.FormattingApplied = true;
            }
            else if (dataGridView.Columns[e.ColumnIndex].Name == "colDays")
            {
                // Форматируем дни недели
                if (item.DaysOfWeek == null || item.DaysOfWeek.Count == 0)
                {
                    e.Value = "Ежедневно";
                }
                else if (item.DaysOfWeek.Contains("*"))
                {
                    // Если есть "*" - значит все дни
                    e.Value = "Ежедневно";
                }
                else if (item.DaysOfWeek.Count == 7)
                {
                    // Если выбраны все 7 дней недели
                    e.Value = "Ежедневно";
                }
                else
                {
                    // Преобразуем сокращенные названия в русские
                    var dayNames = new Dictionary<string, string>
            {
                { "mon", "Пн" }, { "monday", "Пн" },
                { "tue", "Вт" }, { "tuesday", "Вт" },
                { "wed", "Ср" }, { "wednesday", "Ср" },
                { "thu", "Чт" }, { "thursday", "Чт" },
                { "fri", "Пт" }, { "friday", "Пт" },
                { "sat", "Сб" }, { "saturday", "Сб" },
                { "sun", "Вс" }, { "sunday", "Вс" },
                { "1", "Пн" }, { "2", "Вт" }, { "3", "Ср" },
                { "4", "Чт" }, { "5", "Пт" }, { "6", "Сб" }, { "7", "Вс" }
            };

                    var displayDays = new List<string>();
                    foreach (var day in item.DaysOfWeek)
                    {
                        var dayLower = day.ToLower();
                        if (dayNames.ContainsKey(dayLower))
                            displayDays.Add(dayNames[dayLower]);
                        else
                            displayDays.Add(day);
                    }
                    e.Value = string.Join(", ", displayDays);
                }
                e.FormattingApplied = true;
            }
        }
        // Двойной клик по строке для редактирования
        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                BtnEdit_Click(sender, e);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var editForm = new EditScheduleForm())
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _config.ScheduleItems.Add(editForm.ScheduleItem);
                    _bindingSource.ResetBindings(false);
                    dataGridView.Refresh(); // Добавляем обновление отображения
                    UpdateStatusBar();
                    _isModified = true;
                    UpdateFormTitle();
                }
            }
        }
        // Редактировать элемент
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                var selectedItem = dataGridView.SelectedRows[0].DataBoundItem as ScheduleItem;
                if (selectedItem != null)
                {
                    // Используем конструктор с параметром - он создает копию
                    using (var editForm = new EditScheduleForm(selectedItem))
                    {
                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            var editedItem = editForm.ScheduleItem;

                            // ДЕБАГ: Проверяем, что время изменилось
                            Console.WriteLine($"После редактирования: Start={editedItem.StartHour}:{editedItem.StartMinute}, End={editedItem.EndHour}:{editedItem.EndMinute}");

                            // Полностью заменяем объект
                            int index = _config.ScheduleItems.IndexOf(selectedItem);
                            if (index >= 0)
                            {
                                _config.ScheduleItems[index] = editedItem;
                            }

                            // Обновляем привязку
                            _bindingSource.ResetBindings(false);

                            // Обновляем выделение
                            if (index < dataGridView.Rows.Count)
                            {
                                dataGridView.Rows[index].Selected = true;
                            }

                            _isModified = true;
                            UpdateFormTitle();

                            // Принудительно обновляем отображение
                            dataGridView.Refresh();
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите элемент для редактирования", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Удалить элемент
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0 &&
                MessageBox.Show("Удалить выбранные элементы?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    var item = row.DataBoundItem as ScheduleItem;
                    if (item != null)
                    {
                        _config.ScheduleItems.Remove(item);
                    }
                }
                _bindingSource.ResetBindings(false);
                dataGridView.Refresh(); // Добавляем обновление отображения
                UpdateStatusBar();
                _isModified = true;
                UpdateFormTitle();
            }
        }

        // Сохранить текущий файл
        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSchedule(_currentFilePath);
        }

        // Загрузить из текущего файла
        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (_isModified && !AskSaveChanges())
                return;

            if (MessageBox.Show("Загрузить данные из текущего файла? Текущие изменения будут потеряны.",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoadSchedule(_currentFilePath);
                _bindingSource.ResetBindings(false);
                UpdateStatusBar();
            }
        }

        private void UpdateStatusBar()
        {
            toolStripStatusLabel1.Text = $"Всего элементов: {_config.ScheduleItems.Count} | Файл: {Path.GetFileName(_currentFilePath)}";
        }

        // Выход из программы
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Менеджер плейлистов\nВерсия 1.0\n\nУправление расписанием воспроизведения плейлистов.",
                "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // Открыть файл через диалог
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isModified && !AskSaveChanges())
                return;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadSchedule(openFileDialog.FileName);
                _bindingSource.ResetBindings(false);
                UpdateStatusBar();
            }
        }
        // Сохранить как...
        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog.FileName = Path.GetFileName(_currentFilePath);
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(_currentFilePath);

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSchedule(saveFileDialog.FileName);
                UpdateStatusBar();
            }
        }
        // Спросить о сохранении изменений
        private bool AskSaveChanges()
        {
            if (!_isModified) return true;

            var result = MessageBox.Show("Сохранить изменения в файле?", "Сохранение изменений",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveSchedule(_currentFilePath);
                return true;
            }
            else if (result == DialogResult.No)
            {
                return true;
            }

            return false; // Cancel
        }
        // Закрытие формы
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isModified)
            {
                var result = MessageBox.Show("Сохранить изменения перед закрытием?", "Сохранение изменений",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveSchedule(_currentFilePath);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

            base.OnFormClosing(e);
        }
    }
}