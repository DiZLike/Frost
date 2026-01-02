using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrostLive.Controls
{
    public partial class PlayerControl : UserControl
    {
        private double _volume = 50;
        private string _currentSong = "No song selected";
        private string _playerStatus = "READY";
        private bool _isPlaying = false;
        private string _currentTime = "00:00";

        private Font _titleFont;

        private Color _neonBlue = Color.FromArgb(0x00, 0xFF, 0xFF);
        private Color _neonGreen = Color.Lime;
        private Color _neonRed = Color.Red;
        private Color _neonYellow = Color.Yellow;
        private Color _backgroundColor = Color.FromArgb(0x05, 0x05, 0x08);
        private Color _borderColor = Color.Cyan;
        private Color _playButtonColor = Color.Lime;

        #region Свойства
        [Browsable(true)]
        [Category("Player")]
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
                    if (_volumeControl != null)
                        _volumeControl.Volume = _volume;
                    VolumeChanged?.Invoke(this, _volume);
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        [Browsable(true)]
        [Category("Player")]
        [Description("Текущая песня")]
        [DefaultValue("No song selected")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string CurrentSong
        {
            get => _currentSong;
            set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    if (_songTitleLabel != null)
                        _songTitleLabel.Text = value;
                    OnPropertyChanged(nameof(CurrentSong));
                }
            }
        }

        [Browsable(true)]
        [Category("Player")]
        [Description("Статус плеера")]
        [DefaultValue("READY")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string PlayerStatus
        {
            get => _playerStatus;
            set
            {
                if (_playerStatus != value)
                {
                    _playerStatus = value;
                    if (_statusLabel != null)
                    {
                        _statusLabel.Text = value;
                        _statusLabel.ForeColor = GetStatusColor();
                        _statusPanel.Invalidate();
                    }
                    OnPropertyChanged(nameof(PlayerStatus));
                }
            }
        }

        [Browsable(true)]
        [Category("Player")]
        [Description("Играет ли плеер")]
        [DefaultValue(false)]
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    UpdatePlayPauseButton();
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        [Browsable(true)]
        [Category("Player")]
        [Description("Текущее время")]
        [DefaultValue("00:00")]
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    if (_currentTimeLabel != null)
                    {
                        _currentTimeLabel.Text = value;
                        _currentTimeLabel.Invalidate();
                    }
                    OnPropertyChanged(nameof(CurrentTime));
                }
            }
        }

        [Browsable(true)]
        [Category("Peak Meter")]
        [Description("Значение левого пика")]
        [DefaultValue(0.0)]
        public double LeftPeakValue
        {
            get => _peakMeter?.LeftValue ?? 0;
            set
            {
                if (_peakMeter != null && Math.Abs(_peakMeter.LeftValue - value) > 0.001)
                {
                    _peakMeter.LeftValue = value;
                }
            }
        }

        [Browsable(true)]
        [Category("Peak Meter")]
        [Description("Значение правого пика")]
        [DefaultValue(0.0)]
        public double RightPeakValue
        {
            get => _peakMeter?.RightValue ?? 0;
            set
            {
                if (_peakMeter != null && Math.Abs(_peakMeter.RightValue - value) > 0.001)
                {
                    _peakMeter.RightValue = value;
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет фона")]
        [DefaultValue(typeof(Color), "5, 5, 8")]
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    Invalidate();
                    OnPropertyChanged(nameof(BackgroundColor));
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет рамки")]
        [DefaultValue(typeof(Color), "Cyan")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    Invalidate();
                    OnPropertyChanged(nameof(BorderColor));
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Цвет кнопки Play/Pause")]
        [DefaultValue(typeof(Color), "Lime")]
        public Color PlayButtonColor
        {
            get => _playButtonColor;
            set
            {
                if (_playButtonColor != value)
                {
                    _playButtonColor = value;
                    if (_playPauseButton != null)
                    {
                        _playPauseButton.ForeColor = value;
                        _playPauseButton.FlatAppearance.BorderColor = value;
                        _playPauseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, value);
                        _playPauseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, value);
                        _playPauseButton.Invalidate();
                    }
                    OnPropertyChanged(nameof(PlayButtonColor));
                }
            }
        }

        #endregion

        #region События
        [Browsable(true)]
        [Category("Player")]
        [Description("Происходит при изменении громкости")]
        public event EventHandler<double> VolumeChanged;

        [Browsable(true)]
        [Category("Player")]
        [Description("Происходит при нажатии кнопки Play/Pause")]
        public event EventHandler PlayPauseClicked;

        #endregion


        public PlayerControl()
        {
            InitializeComponent();
            _titleFont = new Font("Courier New", 11, FontStyle.Bold);

            _volumeControl.VolumeChanged += VolumeControl_VolumeChanged;

            _playPauseButton.Click += PlayPauseButton_Click;
            _playPauseButton.Paint += PlayPauseButton_Paint;
            _currentTimeLabel.Paint += CurrentTimeLabel_Paint;

            // Устанавливаем стили для двойной буферизации и корректной отрисовки
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Отрисовка фона и рамки с закругленными углами (как в TrackListControl)
            var clientRect = ClientRectangle;

            // Тень (опционально, можно убрать если не нужно)
            var shadowRect = new Rectangle(1, 1, Width - 2, Height - 2);
            using (var shadowPath = GetRoundedRectPath(shadowRect, 5))
            using (var shadowPen = new Pen(Color.FromArgb(25, 0, 255, 255), 10))
            {
                g.DrawPath(shadowPen, shadowPath);
            }

            // Фон с закругленными углами
            using (var bgPath = GetRoundedRectPath(clientRect, 5))
            using (var bgBrush = new SolidBrush(_backgroundColor))
            {
                g.FillPath(bgBrush, bgPath);
            }

            // Рамка с закругленными углами
            using (var borderPath = GetRoundedRectPath(clientRect, 5))
            using (var borderPen = new Pen(_borderColor, 1))
            {
                g.DrawPath(borderPen, borderPath);
            }
        }

        private Color GetStatusColor()
        {
            switch (_playerStatus.ToUpper())
            {
                case "PLAYING": return _neonGreen;
                case "PAUSED": return _neonYellow;
                case "ERROR": return _neonRed;
                case "READY": return _neonBlue;
                case "STOPPED": return Color.Gray;
                case "BUFFERING": return Color.Orange;
                default: return _neonBlue;
            }
        }
        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton != null)
            {
                _playPauseButton.Text = _isPlaying ? "PAUSE" : "PLAY";
                _playPauseButton.Invalidate();
            }
        }
        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            PlayPauseClicked?.Invoke(this, EventArgs.Empty);
        }
        private void MarqueePanel_Paint(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            using (var borderPen = new Pen(Color.FromArgb(0x66, 0x00, 0xFF, 0xFF), 1))
            using (var path = GetRoundedRectPath(panel.ClientRectangle, 4))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(borderPen, path);
            }
        }
        private void StatusPanel_Paint(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            Color statusColor = GetStatusColor();

            using (var borderPen = new Pen(statusColor, 1))
            using (var path = GetRoundedRectPath(panel.ClientRectangle, 4))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(borderPen, path);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, float radius)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return new GraphicsPath();

            GraphicsPath path = new GraphicsPath();
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

        private void VolumeControl_VolumeChanged(object sender, double volume)
        {
            _volume = volume;
            VolumeChanged?.Invoke(this, volume);
            OnPropertyChanged(nameof(Volume));
        }
        private void PlayPauseButton_Paint(object sender, PaintEventArgs e)
        {
            var button = (Button)sender;

            using (var path = GetRoundedRectPath(button.ClientRectangle, 6))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(new Pen(button.ForeColor, 2), path);
            }
        }
        private void CurrentTimeLabel_Paint(object sender, PaintEventArgs e)
        {
            var label = (Label)sender;

            // Уменьшаем область рисования на 1 пиксель с каждой стороны
            // чтобы рамка не обрезалась границами контрола
            var rect = new Rectangle(
                label.ClientRectangle.X,
                label.ClientRectangle.Y,
                label.ClientRectangle.Width - 1,
                label.ClientRectangle.Height - 1
            );

            using (var borderPen = new Pen(Color.FromArgb(0x66, 0x00, 0xFF, 0xFF), 1))
            using (var path = GetRoundedRectPath(rect, 4))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(borderPen, path);
            }
        }
        public void SetPeakValues(double leftValue, double rightValue)
        {
            _peakMeter?.SetValues(leftValue, rightValue);
        }

        // Метод для уведомления об изменении свойств
        protected virtual void OnPropertyChanged(string propertyName)
        {
            // Может быть использован для привязки данных
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate(); // Перерисовываем при изменении размера
        }
    }
}