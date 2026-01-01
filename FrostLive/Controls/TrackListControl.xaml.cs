using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostLive.Controls
{
    public partial class TrackListControl : UserControl
    {
        public TrackListControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TrackListControl),
                new PropertyMetadata("TRACKS"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TracksProperty =
            DependencyProperty.Register("Tracks", typeof(IEnumerable), typeof(TrackListControl),
                new PropertyMetadata(null));

        public IEnumerable Tracks
        {
            get => (IEnumerable)GetValue(TracksProperty);
            set => SetValue(TracksProperty, value);
        }

        public static readonly DependencyProperty DownloadCommandProperty =
            DependencyProperty.Register("DownloadCommand", typeof(ICommand), typeof(TrackListControl));

        public ICommand DownloadCommand
        {
            get => (ICommand)GetValue(DownloadCommandProperty);
            set => SetValue(DownloadCommandProperty, value);
        }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand", typeof(ICommand), typeof(TrackListControl));

        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        public static readonly DependencyProperty ShowRefreshButtonProperty =
            DependencyProperty.Register("ShowRefreshButton", typeof(bool), typeof(TrackListControl),
                new PropertyMetadata(false));

        public bool ShowRefreshButton
        {
            get => (bool)GetValue(ShowRefreshButtonProperty);
            set => SetValue(ShowRefreshButtonProperty, value);
        }

        public static readonly DependencyProperty MaxListHeightProperty =
            DependencyProperty.Register("MaxListHeight", typeof(double), typeof(TrackListControl),
                new PropertyMetadata(double.PositiveInfinity));

        public double MaxListHeight
        {
            get => (double)GetValue(MaxListHeightProperty);
            set => SetValue(MaxListHeightProperty, value);
        }

        #endregion
    }
}