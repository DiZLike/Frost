// Services/ScheduleValidator.cs
using PlaylistManager.Models;
using System;
using System.Linq;

namespace PlaylistManager.Services
{
    public class ScheduleValidator
    {
        public bool ValidateScheduleItem(ScheduleItem item, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errorMessage = "Название не может быть пустым";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.PlaylistPath))
            {
                errorMessage = "Путь к плейлисту не может быть пустым";
                return false;
            }

            if (item.StartHour < 0 || item.StartHour > 23 ||
                item.StartMinute < 0 || item.StartMinute > 59 ||
                item.EndHour < 0 || item.EndHour > 23 ||
                item.EndMinute < 0 || item.EndMinute > 59)
            {
                errorMessage = "Некорректное время";
                return false;
            }

            // ПРОВЕРКА ДНЕЙ НЕДЕЛИ
            if (item.DaysOfWeek == null || item.DaysOfWeek.Count == 0)
            {
                errorMessage = "Дни недели не указаны";
                return false;
            }

            // Проверяем, что если есть "*", то он должен быть единственным
            if (item.DaysOfWeek.Contains("*") && item.DaysOfWeek.Count > 1)
            {
                errorMessage = "При использовании '*' не должно быть других дней";
                return false;
            }

            return true;
        }
    }
}