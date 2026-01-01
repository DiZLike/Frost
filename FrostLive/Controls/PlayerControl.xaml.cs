using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FrostLive.Controls
{
    public partial class PlayerControl : UserControl
    {
        private double _marqueePosition = 0;
        private double _scrollSpeed = 55.0; // пикселей в секунду
        private bool _isMarqueeActive = false;
        private double _textWidth = 0;
        private TimeSpan _lastCompositionTime = TimeSpan.Zero;

        public PlayerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Unloaded += OnUnloadedHandler;
        }

        #region Peak Meter Properties

        public static readonly DependencyProperty LeftPeakValueProperty =
            DependencyProperty.Register("LeftPeakValue", typeof(double), typeof(PlayerControl),
                new PropertyMetadata(0.0));

        public double LeftPeakValue
        {
            get => (double)GetValue(LeftPeakValueProperty);
            set => SetValue(LeftPeakValueProperty, value);
        }

        public static readonly DependencyProperty RightPeakValueProperty =
            DependencyProperty.Register("RightPeakValue", typeof(double), typeof(PlayerControl),
                new PropertyMetadata(0.0));

        public double RightPeakValue
        {
            get => (double)GetValue(RightPeakValueProperty);
            set => SetValue(RightPeakValueProperty, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentSongProperty =
            DependencyProperty.Register("CurrentSong", typeof(string), typeof(PlayerControl),
                new PropertyMetadata("No song selected", OnCurrentSongChanged));

        public string CurrentSong
        {
            get => (string)GetValue(CurrentSongProperty);
            set => SetValue(CurrentSongProperty, value);
        }

        public static readonly DependencyProperty PlayerStatusProperty =
            DependencyProperty.Register("PlayerStatus", typeof(string), typeof(PlayerControl),
                new PropertyMetadata("READY"));

        public string PlayerStatus
        {
            get => (string)GetValue(PlayerStatusProperty);
            set => SetValue(PlayerStatusProperty, value);
        }

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(PlayerControl),
                new PropertyMetadata(false));

        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double), typeof(PlayerControl),
                new PropertyMetadata(50.0, null, CoerceVolume));

        private static object CoerceVolume(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;
            if (value < 0) return 0.0;
            if (value > 100) return 100.0;
            return value;
        }

        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(string), typeof(PlayerControl),
                new PropertyMetadata("00:00"));

        public string CurrentTime
        {
            get => (string)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public static readonly DependencyProperty PlayPauseCommandProperty =
            DependencyProperty.Register("PlayPauseCommand", typeof(ICommand), typeof(PlayerControl));

        public ICommand PlayPauseCommand
        {
            get => (ICommand)GetValue(PlayPauseCommandProperty);
            set => SetValue(PlayPauseCommandProperty, value);
        }

        public static readonly DependencyProperty PlayPauseButtonTextProperty =
            DependencyProperty.Register("PlayPauseButtonText", typeof(string), typeof(PlayerControl),
                new PropertyMetadata("PLAY"));

        public string PlayPauseButtonText
        {
            get => (string)GetValue(PlayPauseButtonTextProperty);
            set => SetValue(PlayPauseButtonTextProperty, value);
        }

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Ждем полной загрузки и инициализации
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeMarquee();
            }), DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitializeMarquee();
        }

        private void OnUnloadedHandler(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        private static void OnCurrentSongChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as PlayerControl;
            control?.InitializeMarquee();
        }

        #endregion

        #region Marquee Logic with Composition API

        private void InitializeMarquee()
        {
            if (SongTitleText == null || MarqueeContainer == null)
                return;

            // Даем время на обновление layout
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CheckMarqueeNeed();
            }), DispatcherPriority.Render);
        }

        private void CheckMarqueeNeed()
        {
            try
            {
                if (SongTitleText == null || MarqueeContainer == null)
                    return;

                // Принудительно обновляем layout
                SongTitleText.InvalidateMeasure();
                SongTitleText.InvalidateArrange();

                SongTitleText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                SongTitleText.Arrange(new Rect(SongTitleText.DesiredSize));

                _textWidth = SongTitleText.ActualWidth;
                double containerWidth = MarqueeContainer.ActualWidth;

                // Для отладки - выводим размеры
                System.Diagnostics.Debug.WriteLine($"Marquee Debug - Text Width: {_textWidth}, Container Width: {containerWidth}, Text: '{CurrentSong}'");

                // Проверяем, нужна ли прокрутка
                if (_textWidth > containerWidth && containerWidth > 0 && _textWidth > 0)
                {
                    StartMarqueeComposition();
                }
                else
                {
                    StopMarqueeComposition();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking marquee need: {ex.Message}");
            }
        }

        private void StartMarqueeComposition()
        {
            if (_isMarqueeActive)
                return;

            System.Diagnostics.Debug.WriteLine("Starting marquee with Composition API...");

            _isMarqueeActive = true;

            // Показываем дублирующий текст
            if (DuplicateText != null)
            {
                DuplicateText.Visibility = Visibility.Visible;
            }

            // Сбрасываем позицию
            _marqueePosition = 0;
            UpdateMarqueePosition();

            // Регистрируем обработчик события рендеринга
            _lastCompositionTime = TimeSpan.Zero;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void StopMarqueeComposition()
        {
            if (!_isMarqueeActive)
                return;

            System.Diagnostics.Debug.WriteLine("Stopping marquee...");

            _isMarqueeActive = false;

            // Отписываемся от события рендеринга
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            // Сбрасываем позицию
            _marqueePosition = 0;
            UpdateMarqueePosition();

            // Скрываем дублирующий текст
            if (DuplicateText != null)
            {
                DuplicateText.Visibility = Visibility.Collapsed;
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!_isMarqueeActive || MarqueeContainer == null || _textWidth <= 0)
                return;

            RenderingEventArgs args = (RenderingEventArgs)e;

            if (_lastCompositionTime == TimeSpan.Zero)
            {
                _lastCompositionTime = args.RenderingTime;
                return;
            }

            TimeSpan elapsed = args.RenderingTime - _lastCompositionTime;
            _lastCompositionTime = args.RenderingTime;

            double delta = _scrollSpeed * elapsed.TotalSeconds;
            _marqueePosition -= delta;

            // Если первый текст полностью скрылся
            if (_marqueePosition < -_textWidth)
            {
                // Вместо резкого сброса, продолжаем скроллинг дальше
                // Второй текст уже находится в позиции _marqueePosition + _textWidth + 20
                // Когда первый полностью исчез, второй займет его место
                // А затем мы добавим новый третий текст для бесшовности
            }

            UpdateMarqueePosition();
        }

        private void UpdateMarqueePosition()
        {
            if (TitleTransform == null)
                return;

            TitleTransform.X = _marqueePosition;

            if (DuplicateTransform != null)
            {
                // Позиция дублирующего текста
                DuplicateTransform.X = _marqueePosition + _textWidth + 20;
            }
        }

        private void MarqueeContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isMarqueeActive)
            {
                // Пауза через отписку от события рендеринга
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                System.Diagnostics.Debug.WriteLine("Marquee paused (mouse enter)");
            }
        }

        private void MarqueeContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isMarqueeActive)
            {
                // Возобновление через подписку на событие рендеринга
                _lastCompositionTime = TimeSpan.Zero;
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                System.Diagnostics.Debug.WriteLine("Marquee resumed (mouse leave)");
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Отписываемся от события рендеринга
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            // Отписываемся от событий
            Loaded -= OnLoaded;
            SizeChanged -= OnSizeChanged;
            Unloaded -= OnUnloadedHandler;
        }

        #endregion
    }
}