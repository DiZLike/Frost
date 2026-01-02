// TrackListControl.cs
using FrostLive.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrostLive.Controls
{
    [DefaultEvent("DownloadClicked")]
    [DefaultProperty("Title")]
    [ToolboxItem(true)]
    public class TrackListControl : UserControl
    {
        // Поля
        private string _title = "TRACKS";
        private IEnumerable _tracks;
        private bool _showRefreshButton = false;
        private double _maxListHeight = double.PositiveInfinity;

        // Для прокрутки
        private int _scrollOffset = 0;
        private int _itemHeight = 70;
        private int _hoveredItemIndex = -1;
        private int _selectedItemIndex = -1;
        private Dictionary<int, Rectangle> _downloadButtonRects = new Dictionary<int, Rectangle>(); // Изменено на словарь

        // Цвета
        private Color _backgroundColor = Color.FromArgb(0x05, 0x05, 0x08);
        private Color _borderColor = Color.Cyan;
        private Color _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
        private Color _neonGreen = Color.Lime;
        private Color _selectedItemColor = Color.FromArgb(0x33, 0x00, 0xFF, 0xFF);
        private Color _hoverItemColor = Color.FromArgb(0x22, 0x00, 0xFF, 0xFF);

        // Ресурсы
        private SolidBrush _bgBrush;
        private Pen _borderPen;
        private Font _titleFont;
        private Font _trackTitleFont;
        private Font _artistFont;
        private Font _timeFont;
        private StringFormat _centerFormat;

        // События
        [Browsable(true)]
        [Category("Action")]
        [Description("Происходит при нажатии кнопки скачать")]
        public event EventHandler<string> DownloadClicked;

        [Browsable(true)]
        [Category("Action")]
        [Description("Происходит при нажатии кнопки обновить")]
        public event EventHandler RefreshClicked;

        public TrackListControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            InitializeResources();

            this.MouseWheel += TrackListControl_MouseWheel;
            this.MouseClick += TrackListControl_MouseClick;
            this.MouseMove += TrackListControl_MouseMove;
            this.MouseDown += TrackListControl_MouseDown;
        }

        private void InitializeComponent()
        {
            this.Name = "TrackListControl";
            this.Size = new Size(400, 400);
            this.BackColor = Color.Transparent;
            _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
            this.ForeColor = _neonBlue;
        }

        private void InitializeResources()
        {
            _bgBrush = new SolidBrush(_backgroundColor);
            _borderPen = new Pen(_borderColor, 1);
            _titleFont = new Font("Courier New", 16, FontStyle.Bold);
            _trackTitleFont = new Font("Courier New", 11, FontStyle.Bold);
            _artistFont = new Font("Courier New", 10);
            _timeFont = new Font("Courier New", 9);

            _centerFormat = new StringFormat();
            _centerFormat.Alignment = StringAlignment.Center;
            _centerFormat.LineAlignment = StringAlignment.Center;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bgBrush?.Dispose();
                _borderPen?.Dispose();
                _titleFont?.Dispose();
                _trackTitleFont?.Dispose();
                _artistFont?.Dispose();
                _timeFont?.Dispose();
                _centerFormat?.Dispose();
                _downloadButtonRects.Clear();
            }
            base.Dispose(disposing);
        }

        #region Свойства с поддержкой дизайнера

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Заголовок списка")]
        [DefaultValue("TRACKS")]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable Tracks
        {
            get => _tracks;
            set
            {
                _tracks = value;
                _scrollOffset = 0;
                _hoveredItemIndex = -1;
                _selectedItemIndex = -1;
                _downloadButtonRects.Clear(); // Очищаем словарь при обновлении списка
                Invalidate();
            }
        }

        // ... остальные свойства без изменений ...

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Фон
            var clientRect = ClientRectangle;
            using (var bgPath = GetRoundedRectPath(clientRect, 5))
            {
                // Тень
                var shadowRect = new Rectangle(1, 1, Width - 2, Height - 2);
                using (var shadowPath = GetRoundedRectPath(shadowRect, 5))
                using (var shadowPen = new Pen(Color.FromArgb(25, 0, 255, 255), 10))
                {
                    g.DrawPath(shadowPen, shadowPath);
                }

                // Фон и рамка
                g.FillPath(_bgBrush, bgPath);
                g.DrawPath(_borderPen, bgPath);
            }

            // Header
            DrawHeader(g);

            // Очищаем словарь перед отрисовкой
            _downloadButtonRects.Clear();

            // Список треков
            DrawTrackList(g);
        }

        private void DrawHeader(Graphics g)
        {
            var headerRect = new Rectangle(20, 20, Width - 40, 40);

            // Заголовок
            using (var titleBrush = new SolidBrush(_neonBlue))
            {
                g.DrawString(_title, _titleFont, titleBrush, headerRect);
            }

            // Нижняя граница
            using (var borderPen = new Pen(_neonBlue, 1))
            {
                g.DrawLine(borderPen,
                    headerRect.Left, headerRect.Bottom,
                    headerRect.Right, headerRect.Bottom);
            }

            // Кнопка обновления
            if (_showRefreshButton)
            {
                var refreshRect = new Rectangle(Width - 60, 25, 35, 30);
                DrawRefreshButton(g, refreshRect);
            }
        }

        private void DrawRefreshButton(Graphics g, Rectangle rect)
        {
            bool isHovered = rect.Contains(PointToClient(Cursor.Position));
            Color buttonColor = isHovered ? Color.FromArgb(200, _neonBlue) : _neonBlue;

            using (var buttonPen = new Pen(buttonColor, 1))
            using (var buttonBrush = new SolidBrush(buttonColor))
            using (var buttonPath = GetRoundedRectPath(rect, 4))
            {
                g.FillPath(Brushes.Transparent, buttonPath);
                g.DrawPath(buttonPen, buttonPath);

                g.DrawString("⟳", new Font("Segoe UI Emoji", 12),
                           buttonBrush, rect, _centerFormat);
            }
        }

        private void DrawTrackList(Graphics g)
        {
            var listRect = new Rectangle(20, 70, Width - 40, Height - 90);
            g.SetClip(listRect);

            if (_tracks != null)
            {
                int yPos = listRect.Top - _scrollOffset;
                int itemIndex = 0;

                foreach (var trackObj in _tracks)
                {
                    var track = trackObj as RadioTrack;
                    if (track == null) continue;

                    var itemRect = new Rectangle(listRect.Left, yPos, listRect.Width, _itemHeight);

                    if (itemRect.Bottom >= listRect.Top && itemRect.Top <= listRect.Bottom)
                    {
                        DrawTrackItem(g, track, itemRect, itemIndex);
                    }

                    yPos += _itemHeight;
                    itemIndex++;

                    if (yPos > listRect.Bottom)
                        break;
                }
            }

            g.ResetClip();
        }

        private void DrawTrackItem(Graphics g, RadioTrack track, Rectangle rect, int index)
        {
            bool isHovered = index == _hoveredItemIndex;
            bool isSelected = index == _selectedItemIndex;

            // Фон элемента
            if (isSelected)
            {
                using (var selectedBrush = new SolidBrush(_selectedItemColor))
                {
                    g.FillRectangle(selectedBrush, rect);
                }
            }
            else if (isHovered)
            {
                using (var hoverBrush = new SolidBrush(_hoverItemColor))
                {
                    g.FillRectangle(hoverBrush, rect);
                }
            }

            // Отступы
            var contentRect = new Rectangle(
                rect.Left + 10,
                rect.Top + 5,
                rect.Width - 60,
                rect.Height - 10
            );

            // Время
            using (var timeBrush = new SolidBrush(Color.FromArgb(0x80, 0x00, 0xFF, 0xFF)))
            {
                g.DrawString(track.FormattedTime, _timeFont, timeBrush,
                    contentRect.Left, contentRect.Top);
            }

            // Название трека
            using (var titleBrush = new SolidBrush(_neonBlue))
            {
                var titleRect = new Rectangle(
                    contentRect.Left,
                    contentRect.Top + 18,
                    contentRect.Width,
                    20
                );

                using (var format = new StringFormat())
                {
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    g.DrawString(track.DisplayTitle, _trackTitleFont, titleBrush, titleRect, format);
                }
            }

            // Исполнитель
            using (var artistBrush = new SolidBrush(Color.FromArgb(0x90, 0x00, 0xFF, 0xFF)))
            {
                g.DrawString(track.Artist, _artistFont, artistBrush,
                    contentRect.Left, contentRect.Top + 40);
            }

            // Кнопка скачать
            var downloadButtonRect = new Rectangle(
                rect.Right - 45,
                rect.Top + 15,
                35, 35
            );

            // Сохраняем прямоугольник кнопки в словарь
            _downloadButtonRects[index] = downloadButtonRect;

            DrawDownloadButton(g, downloadButtonRect, isHovered);
        }

        private void DrawDownloadButton(Graphics g, Rectangle rect, bool isHot)
        {
            bool isHovered = rect.Contains(PointToClient(Cursor.Position));
            Color buttonColor = (isHot || isHovered) ? _neonGreen : Color.FromArgb(0x80, 0x00, 0xFF, 0x00);

            using (var buttonPen = new Pen(buttonColor, 1))
            using (var buttonBrush = new SolidBrush(buttonColor))
            using (var buttonPath = GetRoundedRectPath(rect, 4))
            {
                g.FillPath(Brushes.Transparent, buttonPath);
                g.DrawPath(buttonPen, buttonPath);

                // Стрелка вниз
                var arrowRect = new Rectangle(rect.Left + 10, rect.Top + 10, 15, 15);
                Point[] arrowPoints = new Point[]
                {
                    new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top),
                    new Point(arrowRect.Left, arrowRect.Bottom),
                    new Point(arrowRect.Right, arrowRect.Bottom)
                };
                g.FillPolygon(buttonBrush, arrowPoints);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.X + rect.Width - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.X + rect.Width - radius * 2, rect.Y + rect.Height - radius * 2,
                       radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void TrackListControl_MouseWheel(object sender, MouseEventArgs e)
        {
            int itemsPerPage = (Height - 90) / _itemHeight;
            int maxScroll = GetTotalItems() * _itemHeight - (Height - 90);

            if (maxScroll > 0)
            {
                _scrollOffset += e.Delta > 0 ? -_itemHeight : _itemHeight;
                _scrollOffset = Math.Max(0, Math.Min(maxScroll, _scrollOffset));
                Invalidate();
            }
        }

        private void TrackListControl_MouseMove(object sender, MouseEventArgs e)
        {
            var listRect = new Rectangle(20, 70, Width - 40, Height - 90);

            if (listRect.Contains(e.Location))
            {
                int relativeY = e.Y - listRect.Top + _scrollOffset;
                int newHoveredIndex = relativeY / _itemHeight;

                if (newHoveredIndex != _hoveredItemIndex)
                {
                    _hoveredItemIndex = newHoveredIndex;
                    Invalidate();
                }

                // Проверяем, находится ли курсор над любой кнопкой скачивания
                bool isOverDownloadButton = false;
                foreach (var kvp in _downloadButtonRects)
                {
                    if (kvp.Value.Contains(e.Location))
                    {
                        isOverDownloadButton = true;
                        break;
                    }
                }

                Cursor = isOverDownloadButton ? Cursors.Hand : Cursors.Default;
            }
            else
            {
                if (_hoveredItemIndex != -1)
                {
                    _hoveredItemIndex = -1;
                    Invalidate();
                }
                Cursor = Cursors.Default;
            }
        }

        private void TrackListControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Кнопка обновления
                if (_showRefreshButton)
                {
                    var refreshRect = new Rectangle(Width - 60, 25, 35, 30);
                    if (refreshRect.Contains(e.Location))
                    {
                        RefreshClicked?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }

                // Проверяем клик по кнопкам скачивания
                foreach (var kvp in _downloadButtonRects)
                {
                    if (kvp.Value.Contains(e.Location))
                    {
                        var track = GetTrackAtIndex(kvp.Key);
                        if (track != null && !string.IsNullOrEmpty(track.Link) && track.Link != "#")
                        {
                            DownloadClicked?.Invoke(this, track.Link);
                            return; // Выходим после обработки
                        }
                    }
                }

                // Выбор элемента (клик по самому элементу, не по кнопке)
                var listRect = new Rectangle(20, 70, Width - 40, Height - 90);
                if (listRect.Contains(e.Location))
                {
                    int relativeY = e.Y - listRect.Top + _scrollOffset;
                    int clickedIndex = relativeY / _itemHeight;

                    // Проверяем, что индекс в пределах
                    if (clickedIndex >= 0 && clickedIndex < GetTotalItems())
                    {
                        _selectedItemIndex = clickedIndex;
                        Invalidate();
                    }
                }
            }
        }

        private void TrackListControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
        }

        private int GetTotalItems()
        {
            if (_tracks == null) return 0;

            int count = 0;
            foreach (var item in _tracks) count++;
            return count;
        }

        private RadioTrack GetTrackAtIndex(int index)
        {
            if (_tracks == null || index < 0) return null;

            int currentIndex = 0;
            foreach (var trackObj in _tracks)
            {
                if (currentIndex == index)
                    return trackObj as RadioTrack;
                currentIndex++;
            }

            return null;
        }

        [Browsable(false)]
        public override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = value;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _downloadButtonRects.Clear(); // Очищаем при изменении размера
            Invalidate();
        }
    }
}