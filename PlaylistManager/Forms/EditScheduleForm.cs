// EditScheduleForm.cs - исправленная версия
using PlaylistManager.Models;
using PlaylistManager.Services;
using System;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace PlaylistManager.Forms
{
    public partial class EditScheduleForm : Form
    {
        private ScheduleItem _item;
        private readonly ScheduleValidator _validator;
        public ScheduleItem ScheduleItem => _item;

        public EditScheduleForm()
        {
            InitializeComponent();
            _validator = new ScheduleValidator();
            _item = new ScheduleItem();
            SetupDayCheckboxes();
            SetupTimePickers();
        }

        public EditScheduleForm(ScheduleItem item)
        {
            InitializeComponent();
            _validator = new ScheduleValidator();
            // СОЗДАЕМ ГЛУБОКУЮ КОПИЮ ОБЪЕКТА
            _item = new ScheduleItem
            {
                Name = item.Name,
                PlaylistPath = item.PlaylistPath,
                StartHour = item.StartHour,
                StartMinute = item.StartMinute,
                EndHour = item.EndHour,
                EndMinute = item.EndMinute,
                DaysOfWeek = new List<string>(item.DaysOfWeek ?? new List<string>())
            };
            SetupDayCheckboxes();
            SetupTimePickers();
            LoadData();
        }

        private void SetupDayCheckboxes()
        {
            // Настраиваем чекбоксы для дней недели
            var days = new[]
            {
                new { CheckBox = chkMonday, Value = "mon", Text = "Понедельник" },
                new { CheckBox = chkTuesday, Value = "tue", Text = "Вторник" },
                new { CheckBox = chkWednesday, Value = "wed", Text = "Среда" },
                new { CheckBox = chkThursday, Value = "thu", Text = "Четверг" },
                new { CheckBox = chkFriday, Value = "fri", Text = "Пятница" },
                new { CheckBox = chkSaturday, Value = "sat", Text = "Суббота" },
                new { CheckBox = chkSunday, Value = "sun", Text = "Воскресенье" }
            };

            foreach (var day in days)
            {
                day.CheckBox.Tag = day.Value;
                day.CheckBox.Text = day.Text;
            }
        }

        private void SetupTimePickers()
        {
            // Настраиваем TimePicker для отображения только времени
            dtpStartTime.Format = DateTimePickerFormat.Custom;
            dtpStartTime.CustomFormat = "HH:mm";
            dtpStartTime.ShowUpDown = true;

            dtpEndTime.Format = DateTimePickerFormat.Custom;
            dtpEndTime.CustomFormat = "HH:mm";
            dtpEndTime.ShowUpDown = true;
        }

        private void LoadData()
        {
            txtName.Text = _item.Name;
            txtPath.Text = _item.PlaylistPath;

            // Устанавливаем время из ScheduleItem
            try
            {
                var startTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day,
                                            _item.StartHour, _item.StartMinute, 0);
                var endTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day,
                                          _item.EndHour, _item.EndMinute, 0);

                dtpStartTime.Value = startTime;
                dtpEndTime.Value = endTime;
            }
            catch
            {
                // Устанавливаем значения по умолчанию при ошибке
                dtpStartTime.Value = DateTime.Today.AddHours(8); // 08:00
                dtpEndTime.Value = DateTime.Today.AddHours(18);  // 18:00
            }

            // Сбрасываем все чекбоксы
            chkAllDays.Checked = false;
            chkMonday.Checked = chkTuesday.Checked = chkWednesday.Checked =
            chkThursday.Checked = chkFriday.Checked = chkSaturday.Checked =
            chkSunday.Checked = false;

            // Сначала включаем все чекбоксы дней
            EnableDayCheckboxes();

            // Проверяем наличие "*" или пустого списка
            if (_item.DaysOfWeek == null || _item.DaysOfWeek.Count == 0)
            {
                _item.DaysOfWeek = new List<string> { "*" };
            }

            if (_item.DaysOfWeek.Contains("*") || _item.DaysOfWeek.Count == 7)
            {
                chkAllDays.Checked = true;
                SetAllDaysCheckboxes(true);
                DisableDayCheckboxes();
            }
            else
            {
                // Устанавливаем отдельные дни
                // Сначала сбрасываем все чекбоксы
                SetAllDaysCheckboxes(false);

                // Затем устанавливаем выбранные дни
                foreach (var day in _item.DaysOfWeek.Distinct())
                {
                    var dayLower = day.ToLower();

                    // Преобразуем различные форматы дней недели
                    var dayMapping = new Dictionary<string, string>
            {
                { "mon", "mon" }, { "monday", "mon" }, { "1", "mon" },
                { "tue", "tue" }, { "tuesday", "tue" }, { "2", "tue" },
                { "wed", "wed" }, { "wednesday", "wed" }, { "3", "wed" },
                { "thu", "thu" }, { "thursday", "thu" }, { "4", "thu" },
                { "fri", "fri" }, { "friday", "fri" }, { "5", "fri" },
                { "sat", "sat" }, { "saturday", "sat" }, { "6", "sat" },
                { "sun", "sun" }, { "sunday", "sun" }, { "7", "sun" }
            };

                    if (dayMapping.TryGetValue(dayLower, out var mappedDay))
                    {
                        // Находим соответствующий чекбокс по тегу
                        foreach (Control control in pnlDays.Controls)
                        {
                            if (control is CheckBox chk && chk.Tag?.ToString() == mappedDay)
                            {
                                chk.Checked = true;
                                break;
                            }
                        }
                    }
                }

                // Проверяем, выбраны ли все дни
                int checkedCount = 0;
                foreach (Control control in pnlDays.Controls)
                {
                    if (control is CheckBox chk && chk != chkAllDays && chk.Checked)
                    {
                        checkedCount++;
                    }
                }

                if (checkedCount == 7)
                {
                    chkAllDays.Checked = true;
                    DisableDayCheckboxes();
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // СОХРАНЯЕМ ВСЕ ДАННЫЕ ИЗ ФОРМЫ
            _item.Name = txtName.Text.Trim();
            _item.PlaylistPath = txtPath.Text.Trim();

            // ВАЖНО: Сохраняем время из DateTimePicker
            _item.StartHour = dtpStartTime.Value.Hour;
            _item.StartMinute = dtpStartTime.Value.Minute;
            _item.EndHour = dtpEndTime.Value.Hour;
            _item.EndMinute = dtpEndTime.Value.Minute;

            // Отладочное сообщение (можно убрать после проверки)
            Console.WriteLine($"Сохранение времени: Start={_item.StartHour}:{_item.StartMinute}, End={_item.EndHour}:{_item.EndMinute}");

            // Очищаем список дней
            _item.DaysOfWeek.Clear();

            if (chkAllDays.Checked)
            {
                // Только один "*"
                _item.DaysOfWeek.Add("*");
            }
            else
            {
                // Собираем выбранные дни
                foreach (Control control in pnlDays.Controls)
                {
                    if (control is CheckBox chk && chk.Checked && chk.Tag != null && chk != chkAllDays)
                    {
                        _item.DaysOfWeek.Add(chk.Tag.ToString());
                    }
                }

                // Если ничего не выбрано или выбраны все 7 дней - ставим "*"
                if (_item.DaysOfWeek.Count == 0 || _item.DaysOfWeek.Count == 7)
                {
                    _item.DaysOfWeek.Clear();
                    _item.DaysOfWeek.Add("*");
                }
            }

            // ВАЛИДАЦИЯ
            if (!_validator.ValidateScheduleItem(_item, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Если валидация прошла успешно, закрываем форму с OK
            this.DialogResult = DialogResult.OK;
        }

        private void SetAllDaysCheckboxes(bool isChecked)
        {
            chkMonday.Checked = isChecked;
            chkTuesday.Checked = isChecked;
            chkWednesday.Checked = isChecked;
            chkThursday.Checked = isChecked;
            chkFriday.Checked = isChecked;
            chkSaturday.Checked = isChecked;
            chkSunday.Checked = isChecked;
        }

        private void DisableDayCheckboxes()
        {
            chkMonday.Enabled = false;
            chkTuesday.Enabled = false;
            chkWednesday.Enabled = false;
            chkThursday.Enabled = false;
            chkFriday.Enabled = false;
            chkSaturday.Enabled = false;
            chkSunday.Enabled = false;
        }

        private void EnableDayCheckboxes()
        {
            chkMonday.Enabled = true;
            chkTuesday.Enabled = true;
            chkWednesday.Enabled = true;
            chkThursday.Enabled = true;
            chkFriday.Enabled = true;
            chkSaturday.Enabled = true;
            chkSunday.Enabled = true;
        }

        private void chkAllDays_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAllDays.Checked)
            {
                SetAllDaysCheckboxes(true);
                DisableDayCheckboxes();
            }
            else
            {
                EnableDayCheckboxes();
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Playlist files (*.pls)|*.pls|All files (*.*)|*.*";
                dialog.Title = "Выберите файл плейлиста";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dialog.FileName;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        // Дополнительные методы для проверки времени
        private void dtpStartTime_ValueChanged(object sender, EventArgs e)
        {
            // Можно добавить проверку, что время начала не позже времени окончания
            if (dtpStartTime.Value > dtpEndTime.Value)
            {
                dtpEndTime.Value = dtpStartTime.Value.AddHours(1);
            }
        }

        private void dtpEndTime_ValueChanged(object sender, EventArgs e)
        {
            // Можно добавить проверку, что время окончания не раньше времени начала
            if (dtpEndTime.Value < dtpStartTime.Value)
            {
                dtpStartTime.Value = dtpEndTime.Value.AddHours(-1);
            }
        }
    }
}