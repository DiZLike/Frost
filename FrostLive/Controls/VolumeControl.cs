// VolumeControl.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrostLive.Controls
{
    [DefaultEvent("VolumeChanged")]
    [DefaultProperty("Volume")]
    [ToolboxItem(true)]
    public class VolumeControl : UserControl
    {
        // Поля
        private double _volume = 50.0;
        private bool _isDragging = false;
        private Rectangle _trackRect;

        // Цвета
        private Color _backgroundColor = Color.Transparent;
        private Color _borderColor = Color.FromArgb(0x66, 0x00, 0xFF, 0xFF); // Тот же цвет, что у времени
        private Color _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
        private Color _trackBackground = Color.FromArgb(0x22, 0x00, 0xFF, 0xFF);
        private Color _trackFillColor = Color.Cyan;
        private Color _thumbColor = Color.Cyan;

        // События
        [Browsable(true)]
        [Category("Action")]
        [Description("Происходит при изменении громкости")]
        public event EventHandler<double> VolumeChanged;

        public VolumeControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            // Устанавливаем фиксированную высоту, как у контрола времени
            this.Height = 30; // Примерная высота для соответствия контролу времени
            this.MinimumSize = new Size(100, 30);
        }

        private void InitializeComponent()
        {
            this.Name = "VolumeControl";
            this.Size = new Size(200, 30); // Фиксированная высота
            this.BackColor = Color.Transparent;
            _trackRect = new Rectangle(10, 12, Width - 20, 6); // Центрируем по вертикали
        }

        #region Свойства с поддержкой дизайнера

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Громкость (0-100)")]
        [DefaultValue(50.0)]
        public double Volume
        {
            get => _volume;
            set
            {
                if (Math.Abs(_volume - value) > 0.1)
                {
                    _volume = Math.Max(0, Math.Min(100, value));
                    VolumeChanged?.Invoke(this, _volume);
                    Invalidate();
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет рамки")]
        [DefaultValue(typeof(Color), "102, 0, 255, 255")] // Цвет как у времени
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет фона трека")]
        public Color TrackBackground
        {
            get => _trackBackground;
            set
            {
                _trackBackground = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет заполненной части трека")]
        public Color TrackFillColor
        {
            get => _trackFillColor;
            set
            {
                _trackFillColor = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет ползунка")]
        public Color ThumbColor
        {
            get => _thumbColor;
            set
            {
                _thumbColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Обновляем прямоугольник трека для текущей высоты
            int trackTop = Height / 2 - 3; // Центрируем по вертикали
            _trackRect = new Rectangle(15, trackTop, Width - 30, 6);

            // Фон контрола
            g.Clear(_backgroundColor);

            // Рисуем рамку как у контрола времени (закругленные углы с радиусом 4)
            var borderRect = new Rectangle(
                ClientRectangle.X,
                ClientRectangle.Y,
                ClientRectangle.Width - 1,
                ClientRectangle.Height - 1
            );

            using (var borderPen = new Pen(_borderColor, 1))
            using (var path = GetRoundedRectPath(borderRect, 4))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(borderPen, path);
            }

            // Дорожка слайдера (внутри рамки)
            int trackPadding = 15;
            var trackRect = new Rectangle(
                trackPadding,
                trackTop,
                Width - trackPadding * 2,
                6
            );

            using (var trackBg = new SolidBrush(Color.FromArgb(0x44, 0x00, 0xFF, 0xFF)))
            using (var trackFill = new LinearGradientBrush(
                trackRect,
                _trackFillColor,
                Color.FromArgb(0x80, _trackFillColor.R, _trackFillColor.G, _trackFillColor.B),
                0f))
            {
                // Фон всей дорожки
                using (var trackPath = GetRoundedRectPath(trackRect, 3))
                {
                    g.FillPath(trackBg, trackPath);
                }

                // Заполненная часть
                float fillRatio = (float)_volume / 100f;
                var fillRect = new Rectangle(
                    trackRect.Left,
                    trackRect.Top,
                    (int)(trackRect.Width * fillRatio),
                    trackRect.Height
                );

                if (fillRect.Width > 0)
                {
                    using (var fillPath = GetRoundedRectPath(fillRect, 3))
                    {
                        g.FillPath(trackFill, fillPath);
                    }
                }

                // Контур дорожки
                using (var path = GetRoundedRectPath(trackRect, 3))
                using (var pen = new Pen(Color.FromArgb(0x66, 0x00, 0xFF, 0xFF), 1))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Рисуем thumb
            DrawCustomThumb(g, trackTop);
        }

        private void DrawCustomThumb(Graphics g, int trackTop)
        {
            // Позиция thumb
            float ratio = (float)_volume / 100f;
            int trackWidth = Width - 30;
            int thumbX = (int)(15 + trackWidth * ratio) - 7;
            int thumbY = trackTop - 4; // Центрируем относительно дорожки

            // Эллипс thumb
            var thumbRect = new Rectangle(thumbX, thumbY, 14, 14);

            // Внешнее свечение
            using (var glowBrush = new SolidBrush(Color.FromArgb(100, _thumbColor.R, _thumbColor.G, _thumbColor.B)))
            {
                g.FillEllipse(glowBrush, thumbRect.X - 2, thumbRect.Y - 2,
                    thumbRect.Width + 4, thumbRect.Height + 4);
            }

            // Сам thumb с градиентом
            using (var thumbBrush = new LinearGradientBrush(
                thumbRect,
                _thumbColor,
                Color.FromArgb(200, Math.Min(255, _thumbColor.R + 50),
                                      Math.Min(255, _thumbColor.G + 50),
                                      Math.Min(255, _thumbColor.B + 50)),
                45f))
            {
                g.FillEllipse(thumbBrush, thumbRect);
            }

            // Контур
            using (var thumbPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
            {
                g.DrawEllipse(thumbPen, thumbRect);
            }

            // Внутренний отблеск
            using (var highlightBrush = new LinearGradientBrush(
                thumbRect,
                Color.FromArgb(150, 255, 255, 255),
                Color.Transparent,
                45f))
            {
                var highlightRect = new Rectangle(
                    thumbRect.X + 3, thumbRect.Y + 3,
                    thumbRect.Width - 6, thumbRect.Height / 2
                );
                g.FillEllipse(highlightBrush, highlightRect);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (rect.Width <= 0 || rect.Height <= 0)
                return path;

            float diameter = radius * 2;

            // Верхний левый угол
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            // Верхний правый угол
            path.AddArc(rect.X + rect.Width - diameter, rect.Y, diameter, diameter, 270, 90);
            // Нижний правый угол
            path.AddArc(rect.X + rect.Width - diameter, rect.Y + rect.Height - diameter,
                       diameter, diameter, 0, 90);
            // Нижний левый угол
            path.AddArc(rect.X, rect.Y + rect.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateVolumeFromMouse(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                UpdateVolumeFromMouse(e.X);
            }

            // Обновляем курсор
            var trackArea = new Rectangle(15, Height / 2 - 10, Width - 30, 20);
            Cursor = trackArea.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        private void UpdateVolumeFromMouse(int mouseX)
        {
            float ratio = (mouseX - 15) / (float)(Width - 30);
            ratio = Math.Max(0, Math.Min(1, ratio));
            Volume = ratio * 100;
        }

        // Методы управления
        public void IncreaseVolume(int step = 5)
        {
            Volume = Math.Min(100, Volume + step);
        }

        public void DecreaseVolume(int step = 5)
        {
            Volume = Math.Max(0, Volume - step);
        }

        public void Mute()
        {
            Volume = 0;
        }

        public void SetToDefault()
        {
            Volume = 50;
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
            // Поддерживаем фиксированную высоту
            if (this.Height != 30)
            {
                this.Height = 30;
                return;
            }
            Invalidate();
        }
    }
}