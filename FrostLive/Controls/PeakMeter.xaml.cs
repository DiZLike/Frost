using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FrostLive.Controls
{
    public partial class PeakMeter : UserControl
    {
        private DispatcherTimer _updateTimer;
        private Rectangle[] _leftBars;
        private Rectangle[] _rightBars;
        private double _leftPeak = 0;
        private double _rightPeak = 0;
        private double _leftHold = 0;
        private double _rightHold = 0;
        private DateTime _lastPeakTime;

        public static readonly DependencyProperty LeftValueProperty =
            DependencyProperty.Register("LeftValue", typeof(double), typeof(PeakMeter),
                new PropertyMetadata(0.0, OnValuesChanged));

        public static readonly DependencyProperty RightValueProperty =
            DependencyProperty.Register("RightValue", typeof(double), typeof(PeakMeter),
                new PropertyMetadata(0.0, OnValuesChanged));

        public static readonly DependencyProperty ShowPeakValuesProperty =
            DependencyProperty.Register("ShowPeakValues", typeof(bool), typeof(PeakMeter),
                new PropertyMetadata(true));

        public static readonly DependencyProperty BarCountProperty =
            DependencyProperty.Register("BarCount", typeof(int), typeof(PeakMeter),
                new PropertyMetadata(20)); // Увеличил количество баров для лучшего визуала

        public PeakMeter()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public double LeftValue
        {
            get => (double)GetValue(LeftValueProperty);
            set => SetValue(LeftValueProperty, value);
        }

        public double RightValue
        {
            get => (double)GetValue(RightValueProperty);
            set => SetValue(RightValueProperty, value);
        }

        public bool ShowPeakValues
        {
            get => (bool)GetValue(ShowPeakValuesProperty);
            set => SetValue(ShowPeakValuesProperty, value);
        }

        public int BarCount
        {
            get => (int)GetValue(BarCountProperty);
            set => SetValue(BarCountProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeMeter();

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(30);
            _updateTimer.Tick += OnUpdateTimer;
            _updateTimer.Start();

            _lastPeakTime = DateTime.Now;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }
        }

        private void InitializeMeter()
        {
            // Очищаем существующие шкалы
            LeftMeterCanvas.Children.Clear();
            RightMeterCanvas.Children.Clear();

            int bars = BarCount;
            _leftBars = new Rectangle[bars];
            _rightBars = new Rectangle[bars];

            // Если ActualSize еще не установлен, используем минимальные размеры
            double canvasWidth = LeftMeterCanvas.ActualWidth;
            if (canvasWidth <= 0) canvasWidth = ActualWidth - 4; // Учитываем отступы

            double canvasHeight = LeftMeterCanvas.ActualHeight;
            if (canvasHeight <= 0) canvasHeight = (ActualHeight - 4) / 2; // Половина высоты на канал

            // Новая шкала заполнения - горизонтальные полосы
            double barWidth = Math.Max(1, canvasWidth / bars - 0.5);
            double barHeight = canvasHeight;

            // Создаем шкалы для левого канала (верхнего)
            for (int i = 0; i < bars; i++)
            {
                _leftBars[i] = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = GetBarBrush(i, bars),
                    RadiusX = 1,
                    RadiusY = 1,
                    Opacity = 0.15
                };

                Canvas.SetLeft(_leftBars[i], i * (barWidth + 0.5));
                Canvas.SetTop(_leftBars[i], 0);
                LeftMeterCanvas.Children.Add(_leftBars[i]);
            }

            // Создаем шкалы для правого канала (нижнего)
            for (int i = 0; i < bars; i++)
            {
                _rightBars[i] = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = GetBarBrush(i, bars),
                    RadiusX = 1,
                    RadiusY = 1,
                    Opacity = 0.15
                };

                Canvas.SetLeft(_rightBars[i], i * (barWidth + 0.5));
                Canvas.SetTop(_rightBars[i], 0);
                RightMeterCanvas.Children.Add(_rightBars[i]);
            }
        }

        private Brush GetBarBrush(int index, int total)
        {
            float position = (float)index / total;

            try
            {
                Color cyan = ((SolidColorBrush)Application.Current.FindResource("RetroBarCyan")).Color;
                Color yellow = ((SolidColorBrush)Application.Current.FindResource("RetroBarYellow")).Color;
                Color red = ((SolidColorBrush)Application.Current.FindResource("RetroBarRed")).Color;

                // Три сегмента: Cyan -> Yellow -> Red
                if (position < 0.5f)
                {
                    // Первая половина: Cyan -> Yellow
                    float ratio = position / 0.5f;

                    byte r = (byte)(cyan.R + (yellow.R - cyan.R) * ratio);      // 0 → 255
                    byte g = (byte)(cyan.G + (yellow.G - cyan.G) * ratio);      // 255 → 255
                    byte b = (byte)(cyan.B + (yellow.B - cyan.B) * ratio);      // 255 → 0

                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
                else
                {
                    // Вторая половина: Yellow -> Red
                    float ratio = (position - 0.5f) / 0.5f;

                    byte r = (byte)(yellow.R + (red.R - yellow.R) * ratio);     // 255 → 255
                    byte g = (byte)(yellow.G + (red.G - yellow.G) * ratio);     // 255 → 0
                    byte b = (byte)(yellow.B + (red.B - yellow.B) * ratio);     // 0 → 0

                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
            }
            catch
            {
                // Запасной вариант если ресурсы не найдены
                if (position < 0.5f)
                {
                    // Cyan -> Yellow
                    float ratio = position / 0.5f;
                    byte r = (byte)(0 + 255 * ratio);      // 0 → 255
                    byte g = 255;                          // 255 → 255
                    byte b = (byte)(255 - 255 * ratio);    // 255 → 0

                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
                else
                {
                    // Yellow -> Red
                    float ratio = (position - 0.5f) / 0.5f;
                    byte r = 255;                          // 255 → 255
                    byte g = (byte)(255 - 255 * ratio);    // 255 → 0
                    byte b = 0;                            // 0 → 0

                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
            }
        }


        private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var meter = d as PeakMeter;
            meter?.UpdateMeter();
        }

        private void UpdateMeter()
        {
            // Обновляем пиковые значения (hold peaks)
            if (LeftValue > _leftPeak)
            {
                _leftPeak = LeftValue;
                _lastPeakTime = DateTime.Now;
            }

            if (RightValue > _rightPeak)
            {
                _rightPeak = RightValue;
                _lastPeakTime = DateTime.Now;
            }

            // Сбрасываем пики через 1.5 секунды
            if ((DateTime.Now - _lastPeakTime).TotalSeconds > 1.5)
            {
                _leftHold = _leftPeak;
                _rightHold = _rightPeak;
                _leftPeak = LeftValue;
                _rightPeak = RightValue;

                // Обновляем текстовые значения
                if (ShowPeakValues)
                {
                    LeftPeakText.Text = _leftHold.ToString("0.00");
                    RightPeakText.Text = _rightHold.ToString("0.00");
                    LeftPeakText.Visibility = Visibility.Visible;
                    RightPeakText.Visibility = Visibility.Visible;
                }
            }

            UpdateMeterBars();
        }

        private void UpdateMeterBars()
        {
            if (_leftBars == null || _rightBars == null)
                return;

            int bars = _leftBars.Length;

            // Обновляем левые шкалы (верхний канал)
            int leftActive = (int)(LeftValue * bars);
            for (int i = 0; i < bars; i++)
            {
                _leftBars[i].Opacity = i < leftActive ? 1.0 : 0.15;
            }

            // Обновляем правые шкалы (нижний канал)
            int rightActive = (int)(RightValue * bars);
            for (int i = 0; i < bars; i++)
            {
                _rightBars[i].Opacity = i < rightActive ? 1.0 : 0.15;
            }

            // Показываем текстовые значения, если есть активные бары
            if (ShowPeakValues)
            {
                bool leftHasValue = leftActive > 0;
                bool rightHasValue = rightActive > 0;

                LeftPeakText.Visibility = leftHasValue ? Visibility.Visible : Visibility.Collapsed;
                RightPeakText.Visibility = rightHasValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnUpdateTimer(object sender, EventArgs e)
        {
            UpdateMeterBars();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            // Задержка для корректного обновления размеров
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeMeter();
                UpdateMeterBars();
            }), DispatcherPriority.Render);
        }
    }
}