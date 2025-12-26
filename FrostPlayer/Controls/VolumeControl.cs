// VolumeControl.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace FrostPlayer.Controls
{
    public class VolumeControl : Control
    {
        private int _value = 80;
        private int _minimum = 0;
        private int _maximum = 100;
        private Color _trackColor = Color.FromArgb(220, 220, 220);
        private Color _fillColor = Color.DodgerBlue;
        private Color _thumbColor = Color.White;
        private Color _thumbBorderColor = Color.FromArgb(180, 180, 180);
        private bool _isDragging;
        private int _thumbSize = 16;
        private int _trackHeight = 6;
        private Rectangle _thumbRect;
        private Timer _updateTimer;

        public VolumeControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            Height = 45;

            _updateTimer = new Timer();
            _updateTimer.Interval = 50;
            _updateTimer.Tick += (s, e) => Invalidate();

            UpdateThumbPosition();
        }

        [Category("Behavior")]
        [Description("Текущее значение громкости")]
        [DefaultValue(80)]
        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != value)
                {
                    _value = value;
                    OnValueChanged();
                    UpdateThumbPosition();
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Минимальное значение громкости")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                if (_minimum != value)
                {
                    _minimum = value;
                    if (_value < _minimum)
                        Value = _minimum;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Максимальное значение громкости")]
        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                if (_maximum != value)
                {
                    _maximum = value;
                    if (_value > _maximum)
                        Value = _maximum;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Цвет фона трека")]
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                if (_trackColor != value)
                {
                    _trackColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Цвет заполненной части трека")]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                if (_fillColor != value)
                {
                    _fillColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Цвет бегунка")]
        public Color ThumbColor
        {
            get => _thumbColor;
            set
            {
                if (_thumbColor != value)
                {
                    _thumbColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Цвет границы бегунка")]
        public Color ThumbBorderColor
        {
            get => _thumbBorderColor;
            set
            {
                if (_thumbBorderColor != value)
                {
                    _thumbBorderColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Высота трека")]
        [DefaultValue(6)]
        public int TrackHeight
        {
            get => _trackHeight;
            set
            {
                if (_trackHeight != value)
                {
                    _trackHeight = Math.Max(2, Math.Min(20, value));
                    UpdateThumbPosition();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Размер бегунка")]
        [DefaultValue(16)]
        public int ThumbSize
        {
            get => _thumbSize;
            set
            {
                if (_thumbSize != value)
                {
                    _thumbSize = Math.Max(8, Math.Min(30, value));
                    UpdateThumbPosition();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public double NormalizedValue => (_value - _minimum) / (double)(_maximum - _minimum);

        public event EventHandler ValueChanged;
        public event EventHandler Scroll;

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnScroll()
        {
            Scroll?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateThumbPosition()
        {
            int trackWidth = Width - _thumbSize;
            int thumbX = (int)(trackWidth * NormalizedValue);
            int thumbY = (Height - _thumbSize) / 2;

            _thumbRect = new Rectangle(thumbX, thumbY, _thumbSize, _thumbSize);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Очищаем фон
            e.Graphics.Clear(BackColor);

            // Рассчитываем размеры
            int trackY = (Height - _trackHeight) / 2;
            int trackWidth = Width - _thumbSize;
            int trackStart = _thumbSize / 2;

            // Рисуем фон трека
            using (var trackBrush = new SolidBrush(_trackColor))
            {
                e.Graphics.FillRoundedRectangle(trackBrush,
                    trackStart, trackY, trackWidth, _trackHeight, _trackHeight / 2);
            }

            // Рисуем заполненную часть трека
            int fillWidth = (int)(trackWidth * NormalizedValue);
            if (fillWidth > 0)
            {
                using (var fillBrush = new SolidBrush(_fillColor))
                {
                    e.Graphics.FillRoundedRectangle(fillBrush,
                        trackStart, trackY, fillWidth, _trackHeight, _trackHeight / 2);
                }

                // Добавляем градиент для красивого вида
                if (fillWidth > 10)
                {
                    using (var gradientBrush = new LinearGradientBrush(
                        new Rectangle(trackStart, trackY, fillWidth, _trackHeight),
                        Color.FromArgb(100, Color.White),
                        Color.Transparent,
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRoundedRectangle(gradientBrush,
                            trackStart, trackY, fillWidth, _trackHeight, _trackHeight / 2);
                    }
                }
            }

            // Рисуем бегунок
            using (var thumbBrush = new SolidBrush(_thumbColor))
            {
                e.Graphics.FillEllipse(thumbBrush, _thumbRect);
            }

            // Рисуем границу бегунка
            using (var borderPen = new Pen(_thumbBorderColor, 1))
            {
                e.Graphics.DrawEllipse(borderPen, _thumbRect);
            }

            // Рисуем внутренний круг бегунка
            int innerSize = _thumbSize - 6;
            var innerRect = new Rectangle(
                _thumbRect.X + 3,
                _thumbRect.Y + 3,
                innerSize,
                innerSize);

            using (var innerBrush = new SolidBrush(Color.FromArgb(150, _fillColor)))
            {
                e.Graphics.FillEllipse(innerBrush, innerRect);
            }

            // Если контрол в фокусе, рисуем фокус
            if (Focused)
            {
                var focusRect = new Rectangle(
                    _thumbRect.X - 2,
                    _thumbRect.Y - 2,
                    _thumbRect.Width + 4,
                    _thumbRect.Height + 4);

                ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_thumbRect.Contains(e.Location) || e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
                _updateTimer.Start();
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                UpdateValueFromMouse(e.X);
            }
            else
            {
                // Обновляем курсор при наведении на бегунок
                Cursor = _thumbRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDragging)
            {
                _isDragging = false;
                _updateTimer.Stop();
                Capture = false;
                OnScroll();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    Value = Math.Max(_minimum, _value - 5);
                    OnScroll();
                    break;

                case Keys.Right:
                case Keys.Up:
                    Value = Math.Min(_maximum, _value + 5);
                    OnScroll();
                    break;

                case Keys.PageDown:
                    Value = Math.Max(_minimum, _value - 10);
                    OnScroll();
                    break;

                case Keys.PageUp:
                    Value = Math.Min(_maximum, _value + 10);
                    OnScroll();
                    break;

                case Keys.Home:
                    Value = _minimum;
                    OnScroll();
                    break;

                case Keys.End:
                    Value = _maximum;
                    OnScroll();
                    break;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateThumbPosition();
        }

        private void UpdateValueFromMouse(int mouseX)
        {
            int trackWidth = Width - _thumbSize;
            int trackStart = _thumbSize / 2;

            // Ограничиваем позицию мыши в пределах трека
            mouseX = Math.Max(trackStart, Math.Min(trackStart + trackWidth, mouseX));

            // Вычисляем новое значение
            double normalized = (mouseX - trackStart) / (double)trackWidth;
            normalized = Math.Max(0, Math.Min(1, normalized));

            int newValue = _minimum + (int)(normalized * (_maximum - _minimum));

            if (newValue != _value)
            {
                _value = newValue;
                UpdateThumbPosition();
                OnValueChanged();
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Вспомогательный класс для рисования скругленных прямоугольников
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush,
            float x, float y, float width, float height, float radius)
        {
            var path = CreateRoundedRectanglePath(x, y, width, height, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen,
            float x, float y, float width, float height, float radius)
        {
            var path = CreateRoundedRectanglePath(x, y, width, height, radius);
            graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(
            float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(new RectangleF(x, y, width, height));
                return path;
            }

            radius = Math.Min(radius, Math.Min(width, height) / 2);

            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}