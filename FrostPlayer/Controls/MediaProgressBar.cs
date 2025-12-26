// MediaProgressBar.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrostPlayer.Controls
{
    public class MediaProgressBar : ProgressBar
    {
        private Color _bufferColor = Color.FromArgb(100, 100, 100, 100);
        private Color _progressColor = Color.DodgerBlue;
        private Color _backgroundColor = Color.FromArgb(240, 240, 240);
        private double _bufferValue;
        private bool _showTimeTooltip;
        private ToolTip _timeTooltip;
        private string _timeFormat = "mm\\:ss";

        public MediaProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _timeTooltip = new ToolTip
            {
                AutoPopDelay = 3000,
                InitialDelay = 100,
                ReshowDelay = 100,
                ShowAlways = false
            };
        }

        [Category("Appearance")]
        [Description("Цвет буферизации")]
        public Color BufferColor
        {
            get => _bufferColor;
            set
            {
                if (_bufferColor != value)
                {
                    _bufferColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Цвет прогресса")]
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                if (_progressColor != value)
                {
                    _progressColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Фоновый цвет")]
        public Color BarBackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Значение буферизации (0-1)")]
        [DefaultValue(0.0)]
        public double BufferValue
        {
            get => _bufferValue;
            set
            {
                if (value < 0) value = 0;
                if (value > 1) value = 1;

                if (Math.Abs(_bufferValue - value) > 0.001)
                {
                    _bufferValue = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Показывать подсказку с временем при наведении")]
        [DefaultValue(true)]
        public bool ShowTimeTooltip
        {
            get => _showTimeTooltip;
            set => _showTimeTooltip = value;
        }

        [Category("Behavior")]
        [Description("Формат отображения времени")]
        [DefaultValue("mm\\:ss")]
        public string TimeFormat
        {
            get => _timeFormat;
            set => _timeFormat = value ?? "mm\\:ss";
        }

        [Category("Behavior")]
        [Description("Текущая позиция в секундах")]
        [Browsable(false)]
        public double CurrentTime { get; set; }

        [Category("Behavior")]
        [Description("Общая длительность в секундах")]
        [Browsable(false)]
        public double Duration { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Очищаем фон
            e.Graphics.Clear(BackColor);

            // Рисуем фон прогресс-бара
            var barRect = new Rectangle(0, 0, Width, Height);
            using (var bgBrush = new SolidBrush(_backgroundColor))
            {
                e.Graphics.FillRectangle(bgBrush, barRect);
            }

            // Рисуем буферизацию (если есть)
            if (_bufferValue > 0)
            {
                int bufferWidth = (int)(Width * _bufferValue);
                var bufferRect = new Rectangle(0, 0, bufferWidth, Height);
                using (var bufferBrush = new SolidBrush(_bufferColor))
                {
                    e.Graphics.FillRectangle(bufferBrush, bufferRect);
                }
            }

            // Рисуем прогресс
            if (Maximum > Minimum)
            {
                float scaleFactor = (float)(Value - Minimum) / (Maximum - Minimum);
                int progressWidth = (int)(Width * scaleFactor);

                if (progressWidth > 0)
                {
                    var progressRect = new Rectangle(0, 0, progressWidth, Height);
                    using (var progressBrush = new SolidBrush(_progressColor))
                    {
                        e.Graphics.FillRectangle(progressBrush, progressRect);
                    }

                    // Рисуем градиент для красивого вида (опционально)
                    if (progressWidth > 10)
                    {
                        using (var gradientBrush = new LinearGradientBrush(
                            progressRect,
                            Color.FromArgb(100, Color.White),
                            Color.Transparent,
                            LinearGradientMode.Vertical))
                        {
                            e.Graphics.FillRectangle(gradientBrush, progressRect);
                        }
                    }
                }
            }

            // Рисуем рамку
            using (var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                e.Graphics.DrawRectangle(borderPen,
                    new Rectangle(0, 0, Width - 1, Height - 1));
            }

            // Если контрол в фокусе, рисуем индикатор
            if (Focused)
            {
                var focusRect = new Rectangle(2, 2, Width - 5, Height - 5);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_showTimeTooltip && Duration > 0)
            {
                // Вычисляем время в позиции курсора
                float percent = (float)e.X / Width;
                double timeAtCursor = Duration * percent;
                var timeSpan = TimeSpan.FromSeconds(timeAtCursor);

                string timeText = $"{timeSpan.ToString(_timeFormat)} / {TimeSpan.FromSeconds(Duration).ToString(_timeFormat)}";

                // Показываем подсказку
                _timeTooltip.Show(timeText, this, e.X + 10, e.Y + 10, 2000);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _timeTooltip.Hide(this);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timeTooltip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}