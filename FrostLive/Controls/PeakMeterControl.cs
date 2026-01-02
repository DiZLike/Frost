// PeakMeterControl.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrostLive.Controls
{
    [DefaultEvent("ValueChanged")]
    [DefaultProperty("LeftValue")]
    [ToolboxItem(true)]
    public class PeakMeterControl : UserControl
    {
        // Поля
        private double _leftValue = 0;
        private double _rightValue = 0;
        private double _leftPeak = 0;
        private double _rightPeak = 0;
        private double _leftHold = 0;
        private double _rightHold = 0;
        private DateTime _lastPeakTime;

        private Timer _updateTimer;
        private int _barCount = 30;
        private bool _showPeakValues = true;

        // Цвета с поддержкой дизайнера
        private Color _backgroundColor = Color.FromArgb(0x05, 0x05, 0x08);
        private Color _borderColor = Color.Cyan;
        private Color _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
        private Color _neonGreen = Color.Lime;
        private Color _neonRed = Color.Red;
        private Color _neonYellow = Color.Yellow;
        private Color _channelBackground = Color.FromArgb(0x0A, 0x00, 0xFF, 0xFF);
        private Color _dividerColor = Color.FromArgb(0x66, 0x00, 0xFF, 0xFF);

        // Ресурсы
        private SolidBrush _bgBrush;
        private Pen _borderPen;
        private SolidBrush _channelBgBrush;
        private Pen _dividerPen;
        private SolidBrush _peakTextBrush;
        private Font _peakFont;

        // События
        [Browsable(true)]
        [Category("Action")]
        [Description("Происходит при изменении значений")]
        public event EventHandler ValueChanged;

        public void SetValues(double leftValue, double rightValue)
        {
            LeftValue = leftValue;
            RightValue = rightValue;
        }

        public void ResetPeaks()
        {
            _leftPeak = _leftValue;
            _rightPeak = _rightValue;
            _leftHold = _leftValue;
            _rightHold = _rightValue;
            Invalidate();
        }

        public PeakMeterControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            InitializeResources();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
            this.Name = "PeakMeterControl";
            this.Size = new Size(580, 40);
            this.BackColor = Color.Transparent;
            this.ForeColor = _neonBlue;
        }

        private void InitializeResources()
        {
            _bgBrush = new SolidBrush(_backgroundColor);
            _borderPen = new Pen(_borderColor, 1);
            _channelBgBrush = new SolidBrush(_channelBackground);
            _dividerPen = new Pen(_dividerColor, 1);
            _peakTextBrush = new SolidBrush(_neonBlue);
            _peakFont = new Font("Courier New", 8, FontStyle.Bold);
        }

        private void SetupTimer()
        {
            _updateTimer = new Timer();
            _updateTimer.Interval = 30;
            _updateTimer.Tick += UpdateTimer_Tick;
            _lastPeakTime = DateTime.Now;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode && _updateTimer != null)
                _updateTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();

                _bgBrush?.Dispose();
                _borderPen?.Dispose();
                _channelBgBrush?.Dispose();
                _dividerPen?.Dispose();
                _peakTextBrush?.Dispose();
                _peakFont?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Свойства с поддержкой дизайнера

        [Browsable(true)]
        [Category("Data")]
        [Description("Значение левого канала")]
        [DefaultValue(0.0)]
        public double LeftValue
        {
            get => _leftValue;
            set
            {
                if (Math.Abs(_leftValue - value) > 0.001)
                {
                    _leftValue = Math.Max(0, Math.Min(1, value));

                    if (_leftValue > _leftPeak)
                    {
                        _leftPeak = _leftValue;
                        _lastPeakTime = DateTime.Now;
                    }

                    Invalidate();
                    OnValueChanged(EventArgs.Empty);
                }
            }
        }

        [Browsable(true)]
        [Category("Data")]
        [Description("Значение правого канала")]
        [DefaultValue(0.0)]
        public double RightValue
        {
            get => _rightValue;
            set
            {
                if (Math.Abs(_rightValue - value) > 0.001)
                {
                    _rightValue = Math.Max(0, Math.Min(1, value));

                    if (_rightValue > _rightPeak)
                    {
                        _rightPeak = _rightValue;
                        _lastPeakTime = DateTime.Now;
                    }

                    Invalidate();
                    OnValueChanged(EventArgs.Empty);
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Показывать пиковые значения")]
        [DefaultValue(true)]
        public bool ShowPeakValues
        {
            get => _showPeakValues;
            set
            {
                _showPeakValues = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Количество баров")]
        [DefaultValue(30)]
        public int BarCount
        {
            get => _barCount;
            set
            {
                _barCount = Math.Max(1, Math.Min(50, value));
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет фона")]
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                _bgBrush?.Dispose();
                _bgBrush = new SolidBrush(value);
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет рамки")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                _borderPen?.Dispose();
                _borderPen = new Pen(value, 1);
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет неоново-голубой")]
        public Color NeonBlue
        {
            get => _neonBlue;
            set
            {
                _neonBlue = value;
                _peakTextBrush?.Dispose();
                _peakTextBrush = new SolidBrush(value);
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет фона канала")]
        public Color ChannelBackground
        {
            get => _channelBackground;
            set
            {
                _channelBackground = value;
                _channelBgBrush?.Dispose();
                _channelBgBrush = new SolidBrush(value);
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет разделителя")]
        public Color DividerColor
        {
            get => _dividerColor;
            set
            {
                _dividerColor = value;
                _dividerPen?.Dispose();
                _dividerPen = new Pen(value, 1);
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

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastPeakTime).TotalSeconds > 1.5)
            {
                _leftHold = _leftPeak;
                _rightHold = _rightPeak;
                _leftPeak = _leftValue;
                _rightPeak = _rightValue;
                Invalidate();
            }
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

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

            // Внутренние отступы
            var innerRect = new Rectangle(4, 4, Width - 8, Height - 8);

            // Разделяем на два канала
            int channelHeight = (innerRect.Height - 2) / 2;

            // Левая шкала
            var leftChannelRect = new Rectangle(
                innerRect.Left,
                innerRect.Top,
                innerRect.Width - 40,
                channelHeight
            );
            DrawChannel(g, leftChannelRect, _leftValue);

            // Правая шкала
            var rightChannelRect = new Rectangle(
                innerRect.Left,
                innerRect.Top + channelHeight + 2,
                innerRect.Width - 40,
                channelHeight
            );
            DrawChannel(g, rightChannelRect, _rightValue);

            // Разделительная линия
            int midY = innerRect.Top + channelHeight + 1;
            g.DrawLine(_dividerPen,
                innerRect.Left + 4, midY,
                innerRect.Right - 44, midY);

            // Пиковые значения
            if (_showPeakValues)
            {
                DrawPeakValue(g, _leftHold, true);
                DrawPeakValue(g, _rightHold, false);
            }
        }

        private void DrawChannel(Graphics g, Rectangle rect, double value)
        {
            // Фон канала
            g.FillRectangle(_channelBgBrush, rect);

            int barCount = _barCount;
            float barWidth = Math.Max(1, (rect.Width - (barCount - 1)) / (float)barCount);
            int barHeight = rect.Height - 4;

            // Рассчитываем активные бары
            int activeBars = (int)(value * barCount);
            activeBars = Math.Min(barCount, Math.Max(0, activeBars));

            // Рисуем бары
            for (int i = 0; i < barCount; i++)
            {
                bool isActive = i < activeBars;
                Color barColor = GetBarColor(i, barCount);
                float opacity = isActive ? 1.0f : 0.15f;

                float x = rect.Left + i * (barWidth + 1);
                float y = rect.Top + 2;

                using (var barBrush = new SolidBrush(Color.FromArgb((int)(opacity * 255), barColor)))
                {
                    var barRect = new RectangleF(x, y, barWidth, barHeight);
                    using (var barPath = GetRoundedBarPath(barRect, 1))
                    {
                        g.FillPath(barBrush, barPath);

                        // Контур для активных баров
                        if (isActive)
                        {
                            using (var outlinePen = new Pen(Color.FromArgb(100, 255, 255, 255), 0.5f))
                            {
                                g.DrawPath(outlinePen, barPath);
                            }
                        }
                    }
                }
            }
        }

        private void DrawPeakValue(Graphics g, double value, bool isTop)
        {
            string text = value.ToString("0.00");
            var textSize = g.MeasureString(text, _peakFont);

            int x = Width - 40;
            int y = isTop ? 5 : Height - 17;

            if (value > 0.01)
            {
                g.DrawString(text, _peakFont, _peakTextBrush, x, y);
            }
        }

        private GraphicsPath GetRoundedBarPath(RectangleF rect, float radius)
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

        private Color GetBarColor(int index, int total)
        {
            float position = (float)index / total;

            if (position < 0.5f)
            {
                float ratio = position / 0.5f;
                int r = (int)(0 + 255 * ratio);
                int g = 255;
                int b = (int)(255 - 255 * ratio);
                return Color.FromArgb(255, r, g, b);
            }
            else
            {
                float ratio = (position - 0.5f) / 0.5f;
                int r = 255;
                int g = (int)(255 - 255 * ratio);
                int b = 0;
                return Color.FromArgb(255, r, g, b);
            }
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
            Invalidate();
        }
    }
}