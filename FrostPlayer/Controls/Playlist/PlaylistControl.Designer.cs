// PlaylistControl.Designer.cs
using FrostPlayer.Controls.Playlist;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FrostPlayer.Controls
{
    public partial class PlaylistControl
    {
        // События
        public event EventHandler<PlaylistItemEventArgs> ItemDoubleClick;
        public event EventHandler<PlaylistItemEventArgs> SelectionChanged;
        public event EventHandler<ColumnHeaderEventArgs> ColumnClick;

        // Настройки отображения
        private Color _headerBackgroundColor = Color.FromArgb(245, 245, 245);
        private Color _headerTextColor = Color.FromArgb(80, 80, 80);
        private Color _rowBackgroundColor = Color.White;
        private Color _alternateRowBackgroundColor = Color.FromArgb(248, 248, 248);
        private Color _selectionColor = Color.FromArgb(200, 230, 255);
        private Color _borderColor = Color.FromArgb(220, 220, 220);
        private Color _playingIndicatorColor = Color.DodgerBlue;
        private Color _textColor = Color.FromArgb(60, 60, 60);
        private Color _durationTextColor = Color.FromArgb(120, 120, 120);

        // Размеры
        private int _rowHeight = PlaylistConstants.DefaultRowHeight;
        private int _headerHeight = PlaylistConstants.DefaultHeaderHeight;
        private bool _showGridLines = true;

        // Ширины колонок
        private int _widthDurationColumn = 70;
        private int _widthTitleColumn = 160;
        private int _widthAlbumColumn = 120;
        private int _widthArtistColumn = 120;
        private int _widthIndexColumn = 30;
        private int _widthStatusColumn = 30;

        [Category("Behavior")]
        [Description("Выбранный элемент плейлиста")]
        [Browsable(false)]
        public PlaylistItem SelectedItem
        {
            get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
            set
            {
                int index = value != null ? _items.IndexOf(value) : -1;
                SelectedIndex = index;
            }
        }

        [Category("Behavior")]
        [Description("Индекс выбранного элемента")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = Math.Max(-1, Math.Min(value, _items.Count - 1));
                    OnSelectionChanged();
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Индекс воспроизводимого элемента")]
        [DefaultValue(-1)]
        public int PlayingIndex
        {
            get => _playingIndex;
            set
            {
                if (_playingIndex != value)
                {
                    _playingIndex = value;
                    Invalidate();
                }
            }
        }

        // Свойства внешнего вида
        [Category("Appearance")]
        [Description("Цвет фона заголовков столбцов")]
        public Color HeaderBackgroundColor
        {
            get => _headerBackgroundColor;
            set { if (_headerBackgroundColor != value) { _headerBackgroundColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет текста заголовков столбцов")]
        public Color HeaderTextColor
        {
            get => _headerTextColor;
            set { if (_headerTextColor != value) { _headerTextColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет фона строк")]
        public Color RowBackgroundColor
        {
            get => _rowBackgroundColor;
            set { if (_rowBackgroundColor != value) { _rowBackgroundColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет фона четных строк")]
        public Color AlternateRowBackgroundColor
        {
            get => _alternateRowBackgroundColor;
            set { if (_alternateRowBackgroundColor != value) { _alternateRowBackgroundColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет выделения строк")]
        public Color SelectionColor
        {
            get => _selectionColor;
            set { if (_selectionColor != value) { _selectionColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет индикатора воспроизведения")]
        public Color PlayingIndicatorColor
        {
            get => _playingIndicatorColor;
            set { if (_playingIndicatorColor != value) { _playingIndicatorColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет текста")]
        public Color TextColor
        {
            get => _textColor;
            set { if (_textColor != value) { _textColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Цвет текста длительности")]
        public Color DurationTextColor
        {
            get => _durationTextColor;
            set { if (_durationTextColor != value) { _durationTextColor = value; Invalidate(); } }
        }

        [Category("Appearance")]
        [Description("Высота строки")]
        [DefaultValue(24)]
        public int RowHeight
        {
            get => _rowHeight;
            set
            {
                if (_rowHeight != value && value >= 18)
                {
                    _rowHeight = value;
                    QueueScrollbarUpdate();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Высота заголовка столбцов")]
        [DefaultValue(28)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set
            {
                if (_headerHeight != value && value >= 20)
                {
                    _headerHeight = value;
                    QueueScrollbarUpdate();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Показывать линии сетки")]
        [DefaultValue(true)]
        public bool ShowGridLines
        {
            get => _showGridLines;
            set
            {
                if (_showGridLines != value)
                {
                    _showGridLines = value;
                    Invalidate();
                }
            }
        }

        // Свойства ширины колонок
        [Category("Layout")]
        [Description("Ширина колонки Длительность")]
        [DefaultValue(70)]
        public int WidthDurationColumn
        {
            get => _widthDurationColumn;
            set { if (_widthDurationColumn != value) { _widthDurationColumn = value; UpdateColumnWidth("Duration", value); } }
        }

        [Category("Layout")]
        [Description("Ширина колонки Название")]
        [DefaultValue(160)]
        public int WidthTitleColumn
        {
            get => _widthTitleColumn;
            set { if (_widthTitleColumn != value) { _widthTitleColumn = value; UpdateColumnWidth("Title", value); } }
        }

        [Category("Layout")]
        [Description("Ширина колонки Альбом")]
        [DefaultValue(120)]
        public int WidthAlbumColumn
        {
            get => _widthAlbumColumn;
            set { if (_widthAlbumColumn != value) { _widthAlbumColumn = value; UpdateColumnWidth("Album", value); } }
        }

        [Category("Layout")]
        [Description("Ширина колонки Исполнитель")]
        [DefaultValue(120)]
        public int WidthArtistColumn
        {
            get => _widthArtistColumn;
            set { if (_widthArtistColumn != value) { _widthArtistColumn = value; UpdateColumnWidth("Artist", value); } }
        }

        [Category("Layout")]
        [Description("Ширина колонки Индекс")]
        [DefaultValue(30)]
        public int WidthIndexColumn
        {
            get => _widthIndexColumn;
            set { if (_widthIndexColumn != value) { _widthIndexColumn = value; UpdateColumnWidth("Index", value); } }
        }

        [Category("Layout")]
        [Description("Ширина колонки Статус")]
        [DefaultValue(30)]
        public int WidthStatusColumn
        {
            get => _widthStatusColumn;
            set { if (_widthStatusColumn != value) { _widthStatusColumn = value; UpdateColumnWidth("Status", value); } }
        }

        [Browsable(false)]
        public List<PlaylistItem> Items => _items;

        [Browsable(false)]
        public List<ColumnHeader> Columns => _columns;
    }
}