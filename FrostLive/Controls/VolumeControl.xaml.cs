using System.Windows;
using System.Windows.Controls;

namespace FrostLive.Controls
{
    public partial class VolumeControl : UserControl
    {
        public VolumeControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double), typeof(VolumeControl),
                new PropertyMetadata(50.0, OnVolumeChanged, CoerceVolume));

        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as VolumeControl;
            control?.RaiseVolumeChangedEvent();
        }

        private static object CoerceVolume(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;
            if (value < 0) return 0.0;
            if (value > 100) return 100.0;
            return value;
        }

        #endregion

        #region Custom Events

        public static readonly RoutedEvent VolumeChangedEvent =
            EventManager.RegisterRoutedEvent(
                "VolumeChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(VolumeControl));

        public event RoutedPropertyChangedEventHandler<double> VolumeChanged
        {
            add { AddHandler(VolumeChangedEvent, value); }
            remove { RemoveHandler(VolumeChangedEvent, value); }
        }

        private void RaiseVolumeChangedEvent()
        {
            RoutedPropertyChangedEventArgs<double> args =
                new RoutedPropertyChangedEventArgs<double>(0, Volume, VolumeChangedEvent);
            RaiseEvent(args);
        }

        #endregion

        #region Methods

        public void IncreaseVolume(int step = 5)
        {
            Volume = System.Math.Min(100, Volume + step);
        }

        public void DecreaseVolume(int step = 5)
        {
            Volume = System.Math.Max(0, Volume - step);
        }

        public void Mute()
        {
            Volume = 0;
        }

        public void SetToDefault()
        {
            Volume = 50;
        }

        #endregion
    }
}