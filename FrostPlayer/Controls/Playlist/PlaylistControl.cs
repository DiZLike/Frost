//PlaylistControl.cs - основной класс со вспомогательными классами (PlaylistItem, ColumnHeader и т.д.)
//PlaylistControl.Designer.cs - свойства для дизайнера
//PlaylistControl.Paint.cs - логика отрисовки
//PlaylistControl.Events.cs - обработчики событий
//PlaylistControl.Data.cs - работа с данными
//Constants.cs - константы

using FrostPlayer.Controls.Playlist;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace FrostPlayer.Controls
{
    public partial class PlaylistControl : Control
    {
        // Коллекции
        private readonly List<PlaylistItem> _items = new List<PlaylistItem>();
        private readonly List<ColumnHeader> _columns = new List<ColumnHeader>();

        // Переменные состояния
        private int _selectedIndex = -1;
        private int _playingIndex = -1;
        private int _hoveredIndex = -1;
        private int _resizingColumnIndex = -1;
        private int _sortColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private bool _isResizing;
        private int _resizeStartX;
        private int _resizeStartWidth;
        private int _scrollOffset;
        private int _visibleRows;
        private VScrollBar _vScrollBar;

        // Флаги
        private bool _needsScrollbarUpdate;

        public PlaylistControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;

            InitializeDefaultColumns();
            UpdateAllColumnsWidth();

            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Width = SystemInformation.VerticalScrollBarWidth
            };
            _vScrollBar.Scroll += OnScroll;
            Controls.Add(_vScrollBar);
        }

        private void InitializeDefaultColumns()
        {
            _columns.Clear();

            _columns.Add(new ColumnHeader
            {
                Text = "▶",
                Name = "Status",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthStatusColumn),
                Sortable = false,
                TextAlignment = HorizontalAlignment.Center
            });

            _columns.Add(new ColumnHeader
            {
                Text = "#",
                Name = "Index",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthIndexColumn),
                Sortable = true,
                TextAlignment = HorizontalAlignment.Center
            });

            _columns.Add(new ColumnHeader
            {
                Text = "Исполнитель",
                Name = "Artist",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthArtistColumn),
                Sortable = true,
                TextAlignment = HorizontalAlignment.Left
            });

            _columns.Add(new ColumnHeader
            {
                Text = "Альбом",
                Name = "Album",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthAlbumColumn),
                Sortable = true,
                TextAlignment = HorizontalAlignment.Left
            });

            _columns.Add(new ColumnHeader
            {
                Text = "Название",
                Name = "Title",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthTitleColumn),
                Sortable = true,
                TextAlignment = HorizontalAlignment.Left
            });

            _columns.Add(new ColumnHeader
            {
                Text = "Длительность",
                Name = "Duration",
                Width = Math.Max(PlaylistConstants.MinColumnWidth, WidthDurationColumn),
                Sortable = true,
                TextAlignment = HorizontalAlignment.Right
            });
        }

        private void UpdateAllColumnsWidth()
        {
            UpdateColumnWidth("Status", WidthStatusColumn);
            UpdateColumnWidth("Index", WidthIndexColumn);
            UpdateColumnWidth("Artist", WidthArtistColumn);
            UpdateColumnWidth("Album", WidthAlbumColumn);
            UpdateColumnWidth("Title", WidthTitleColumn);
            UpdateColumnWidth("Duration", WidthDurationColumn);
        }

        private void UpdateColumnWidth(string name, int width)
        {
            var column = _columns.FirstOrDefault(c => c.Name == name);
            if (column != null)
            {
                int newWidth = Math.Max(PlaylistConstants.MinColumnWidth, width);
                column.Width = newWidth;
                UpdateWidthProperty(name, newWidth);
                Invalidate();
            }
        }

        private void UpdateWidthProperty(string columnName, int width)
        {
            switch (columnName)
            {
                case "Status": WidthStatusColumn = width; break;
                case "Index": WidthIndexColumn = width; break;
                case "Artist": WidthArtistColumn = width; break;
                case "Album": WidthAlbumColumn = width; break;
                case "Title": WidthTitleColumn = width; break;
                case "Duration": WidthDurationColumn = width; break;
            }
        }

        private void ForEachColumn(Action<ColumnHeader, int> action)
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                action(_columns[i], i);
            }
        }

        private string GetColumnPropertyName(string columnName)
        {
            return columnName switch
            {
                "Status" => nameof(WidthStatusColumn),
                "Index" => nameof(WidthIndexColumn),
                "Artist" => nameof(WidthArtistColumn),
                "Album" => nameof(WidthAlbumColumn),
                "Title" => nameof(WidthTitleColumn),
                "Duration" => nameof(WidthDurationColumn),
                _ => null
            };
        }

        public void QueueScrollbarUpdate()
        {
            _needsScrollbarUpdate = true;
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(UpdateScrollbar));
            }
        }

        private void UpdateScrollbar()
        {
            if (!_needsScrollbarUpdate || _vScrollBar == null) return;
            _needsScrollbarUpdate = false;

            int totalHeight = _items.Count * RowHeight;
            int visibleHeight = Math.Max(0, ClientSize.Height - HeaderHeight);

            _vScrollBar.Visible = totalHeight > visibleHeight;

            if (_vScrollBar.Visible)
            {
                int maxValue = Math.Max(0, totalHeight - visibleHeight);
                _vScrollBar.Maximum = maxValue + _vScrollBar.LargeChange - 1;
                _vScrollBar.LargeChange = Math.Max(1, visibleHeight);
                _vScrollBar.SmallChange = RowHeight;

                if (_scrollOffset > maxValue)
                {
                    _scrollOffset = maxValue;
                    _vScrollBar.Value = _scrollOffset;
                }
            }
            else
            {
                _scrollOffset = 0;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintManager.Paint(this, e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            EventHandlers.HandleMouseDown(this, e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            EventHandlers.HandleMouseMove(this, e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            EventHandlers.HandleMouseUp(this, e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                QueueScrollbarUpdate();
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            QueueScrollbarUpdate();
        }

        // Вспомогательные свойства для доступа из partial классов
        internal int SelectedIndexInternal => _selectedIndex;
        internal int PlayingIndexInternal => _playingIndex;
        internal int HoveredIndexInternal => _hoveredIndex;
        internal int ScrollOffsetInternal => _scrollOffset;
        internal int VisibleRowsInternal => _visibleRows;
        internal List<PlaylistItem> ItemsInternal => _items;
        internal List<ColumnHeader> ColumnsInternal => _columns;
        internal VScrollBar ScrollBarInternal => _vScrollBar;

        internal void SetSelectedIndex(int value) => _selectedIndex = value;
        internal void SetPlayingIndex(int value) => _playingIndex = value;
        internal void SetHoveredIndex(int value) => _hoveredIndex = value;
        internal void SetScrollOffset(int value) => _scrollOffset = value;
        internal void SetVisibleRows(int value) => _visibleRows = value;
        internal void SetResizingColumnIndex(int value) => _resizingColumnIndex = value;
        internal void SetIsResizing(bool value) => _isResizing = value;
        internal void SetResizeStartX(int value) => _resizeStartX = value;
        internal void SetResizeStartWidth(int value) => _resizeStartWidth = value;
        internal void SetSortColumnIndex(int value) => _sortColumnIndex = value;
        internal void SetSortOrder(SortOrder value) => _sortOrder = value;

        internal int GetResizingColumnIndex() => _resizingColumnIndex;
        internal bool GetIsResizing() => _isResizing;
        internal int GetResizeStartX() => _resizeStartX;
        internal int GetResizeStartWidth() => _resizeStartWidth;
        internal int GetSortColumnIndex() => _sortColumnIndex;
        internal SortOrder GetSortOrder() => _sortOrder;

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            _scrollOffset = e.NewValue;
            Invalidate();
        }

        protected virtual void OnItemDoubleClick(int index)
        {
            ItemDoubleClick?.Invoke(this, new PlaylistItemEventArgs(_items[index], index));
        }

        protected virtual void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new PlaylistItemEventArgs(SelectedItem, _selectedIndex));
        }

        protected virtual void OnColumnClick(int columnIndex)
        {
            ColumnClick?.Invoke(this, new ColumnHeaderEventArgs(_columns[columnIndex], columnIndex));
        }
    }

    // Класс элемента плейлиста
    public class PlaylistItem
    {
        public int Index { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Title { get; set; }
        public TimeSpan Duration { get; set; }
        public string FilePath { get; set; }

        public string DurationFormatted => Duration.ToString(@"mm\:ss");

        public PlaylistItem() { }

        public PlaylistItem(string filePath, string artist = null, string album = null, string title = null, TimeSpan? duration = null)
        {
            FilePath = filePath;
            Artist = artist ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
            Album = album ?? "Неизвестный альбом";
            Title = title ?? System.IO.Path.GetFileName(filePath);
            Duration = duration ?? TimeSpan.Zero;
        }
    }

    // Класс заголовка столбца
    public class ColumnHeader
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public int Width { get; set; }
        public bool Sortable { get; set; }
        public HorizontalAlignment TextAlignment { get; set; }
    }

    // Аргументы событий
    public class PlaylistItemEventArgs : EventArgs
    {
        public PlaylistItem Item { get; }
        public int Index { get; }

        public PlaylistItemEventArgs(PlaylistItem item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    public class ColumnHeaderEventArgs : EventArgs
    {
        public ColumnHeader Column { get; }
        public int ColumnIndex { get; }

        public ColumnHeaderEventArgs(ColumnHeader column, int columnIndex)
        {
            Column = column;
            ColumnIndex = columnIndex;
        }
    }

    public enum SortOrder
    {
        None,
        Ascending,
        Descending
    }
}