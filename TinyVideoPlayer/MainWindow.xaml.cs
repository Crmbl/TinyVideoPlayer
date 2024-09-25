using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using TinyVideoPlayer.Converters;
using TinyVideoPlayer.Utils;
using Vlc.DotNet.Core;

namespace TinyVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DLL imports 

        [DllImport("User32.dll")]
        public static extern Boolean SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, UInt32 pvParam, UInt32 fWinIni);

        #endregion // DLL imports

        #region Constants

        /// <summary>
        /// Defines the zoom limit min.
        /// </summary>
        private const double ZoomMinTreshold = 0.1;

        /// <summary>
        /// Defines the zoom limit max.
        /// </summary>
        private const double ZoomMaxTreshold = 6.5;

        /// <summary>
        /// Defines the zoom update ratio.
        /// </summary>
        private const double ZoomRatio = 0.1;

        /// <summary>
        /// Defines the opacity to set.
        /// </summary>
        private const double OpacityLevel = 0.1;

        #endregion //Constants

        #region Properties

        /// <summary>
        /// The mouse position.
        /// </summary>
        private Point MousePosition { get; set; }

        /// <summary>
        /// The origin position.
        /// </summary>
        private Point OriginPosition { get; set; }

        /// <summary>
        /// Defines the mouse speed when starting the drag event.
        /// </summary>
        private uint OriginMouseSpeed { get; set; }

        /// <summary>
        /// Defines is repeating.
        /// </summary>
        public bool IsRepeating { get; set; }

        /// <summary>
        /// Current playing file.
        /// </summary>
        public Uri CurrentFile { get; set; }

        /// <summary>
        /// Window start position.
        /// </summary>
        private Point WindowStartPos { get; set; }

        /// <summary>
        /// Defines if use the fade animation for the buttons.
        /// </summary>
        private bool UseAnimation { get; }

        #endregion //Properties

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            IsRepeating = true;
            UseAnimation = true;

            OriginMouseSpeed = (uint)System.Windows.Forms.SystemInformation.MouseSpeed;
            Topmost = true;

            var vlcLibDirectory = new DirectoryInfo(Path.Combine(
                System.Reflection.Assembly.GetEntryAssembly().Location.Replace("TinyVideoPlayer.dll", ""),
                "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            var options = new []
            {
                //"--file-logging", "-vvv", "--extraintf=logger", "--logfile=Logs.log",
                "--no-ignore-config"
            };

            VideoControl.SourceProvider.CreatePlayer(vlcLibDirectory, options);
            VideoControl.RenderTransform = new TransformGroup { Children = new TransformCollection { new TranslateTransform(), new ScaleTransform() } };

            #region Events subscribing

            VideoControl.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
            VideoControl.SourceProvider.MediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
            VideoControl.SourceProvider.MediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
            VideoControl.SourceProvider.MediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            VideoControl.Loaded += VideoControl_Loaded;
            DropZone.MouseWheel += DropZone_MouseWheel;
            DropZone.Drop += DropZone_Drop;
            DropZone.SizeChanged += DropZone_SizeChanged;
            DropZone.PreviewMouseLeftButtonDown += DropZone_PreviewMouseLeftButtonDown;
            DropZone.PreviewMouseMove += DropZone_PreviewMouseMove;
            DropZone.PreviewMouseLeftButtonUp += DropZone_PreviewMouseLeftButtonUp;
            DropZone.MouseDown += DropZone_MouseDown;
            MediaGrid.MouseEnter += MediaGrid_MouseEnter;
            MediaGrid.MouseLeave += MediaGrid_MouseLeave;
            SoundGrid.MouseEnter += SoundGrid_MouseEnter;
            SoundGrid.MouseLeave += SoundGrid_MouseLeave;
            ToolGrid.MouseEnter += ToolGrid_MouseEnter;
            ToolGrid.MouseLeave += ToolGrid_MouseLeave;
            TimeGrid.MouseEnter += TimeGrid_MouseEnter;
            TimeGrid.MouseLeave += TimeGrid_MouseLeave;
            ResizeButton.Click += MediaButton_ButtonClick;
            MaximizeButton.Click += MediaButton_ButtonClick;
            FindMediaButton.Click += MediaButton_ButtonClick;
            ToggleMuteButton.Click += MediaButton_ButtonClick;
            ToggleRepeatButton.Click += MediaButton_ButtonClick;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            TimeSlider.ValueChanged += TimeSlider_ValueChanged;
            TimeSlider.PreviewMouseDown += TimeSlider_PreviewMouseDown;
            TimeSlider.PreviewMouseUp += TimeSlider_PreviewMouseUp;
            ThumbButton.Click += ThumbButton_Click;
            FavoriteButton.Click += FavoriteButton_Click;
            this.PreviewMouseLeftButtonDown += MainWindow_PreviewMouseLeftButtonDown;
            this.PreviewMouseRightButtonDown += MainWindow_PreviewMouseRightButtonDown;
            this.PreviewMouseMove += MainWindow_PreviewMouseMove;
            this.MouseEnter += MainWindow_MouseEnter;
            this.MouseLeave += MainWindow_MouseLeave;
            
            #endregion // Events subscribing

            #region Init bindings

            MediaGrid.SetBinding(Canvas.LeftProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "left",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = MediaGrid },
                    new Binding("ActualHeight") { Source = MediaGrid }
                }
            });
            DropMenu.SetBinding(Canvas.LeftProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "left",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = DropMenu },
                    new Binding("ActualHeight") { Source = DropMenu }
                }
            });
            DropMenu.SetBinding(Canvas.TopProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "top",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = DropMenu },
                    new Binding("ActualHeight") { Source = DropMenu }
                }
            });
            DropText.SetBinding(Canvas.TopProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "top",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = DropText },
                    new Binding("ActualHeight") { Source = DropText }
                }
            });
            TimeSlider.SetBinding(Canvas.BottomProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "bottom",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = TimeSlider },
                    new Binding("ActualHeight") { Source = TimeSlider }
                }
            });

            #endregion //Init bindings

            #region Init Visibility states

            ResizeButton.Visibility = Visibility.Hidden;
            FindMediaButton.Visibility = Visibility.Hidden;
            MaximizeButton.Visibility = Visibility.Hidden;
            VolumeSlider.Visibility = Visibility.Hidden;
            ToggleMuteButton.Visibility = Visibility.Hidden;
            DropMenu.Visibility = Visibility.Visible;
            TimeSlider.Visibility = Visibility.Hidden;
            ThumbButton.Visibility = Visibility.Hidden;
            FavoriteButton.Visibility = Visibility.Hidden;
            ToggleRepeatButton.Visibility = Visibility.Hidden;
            
            #endregion //Init Visibility states
        }

        /// <summary>
        /// Used for the repeat feature.
        /// </summary>
        delegate void VlcRepeatDelegate(Uri fileUri, string[] pars);

        #region Events

        /// <summary>
        /// If arg, play it.
        /// </summary>
        private void VideoControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(App.Arg)) return;
            CurrentFile = new Uri(App.Arg);
            Play(CurrentFile);
        }

        /// <summary>
        /// If window clicked, set opacity to 1.
        /// </summary>
        private void MainWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Opacity = 1;
            Keyboard.Focus(this);
        }

        /// <summary>
        /// When leaving mouse set opacity to 1.
        /// </summary>
        private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            Opacity = 1;
        }

        /// <summary>
        /// If mouse over but not focused, opacity to half
        /// </summary>
        private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Topmost && !HasEffectiveKeyboardFocus && WindowState != WindowState.Maximized)
                Opacity = OpacityLevel;
            else
                Opacity = 1;
        }

        /// <summary>
        /// Handles window dragging.
        /// </summary>
        private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton != MouseButtonState.Pressed) return;

            var vector = e.GetPosition(this) - WindowStartPos;
            Left += vector.X;
            Top += vector.Y;
        }

        /// <summary>
        /// Handles drag start.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowStartPos = e.GetPosition(this);
        }

        /// <summary>
        /// Repeat feature implementation.
        /// </summary>
        private void MediaPlayer_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            if (IsRepeating)
            {
                ThreadPool.QueueUserWorkItem(_ => VideoControl.SourceProvider.MediaPlayer.Play(CurrentFile));
                Dispatcher.Invoke(() => { TimeSlider.Value = 0; });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (DropMenu.Visibility == Visibility.Collapsed)
                        DropMenu.Visibility = Visibility.Visible;
                });
            }
        }

        /// <summary>
        /// Handles the slider movement when the video is playing.
        /// </summary>
        private void MediaPlayer_PositionChanged(object sender, VlcMediaPlayerPositionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!TimeSlider.IsMouseCaptured)
                    TimeSlider.Value = VideoControl.SourceProvider.MediaPlayer.Position;
            });
        }

        /// <summary>
        /// Shows the thumbnail buttons when there is a media.
        /// </summary>
        private void MediaPlayer_MediaChanged(object sender, VlcMediaPlayerMediaChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (ThumbButton.Visibility != Visibility.Visible)
                    ThumbButton.Visibility = Visibility.Visible;
                if (FavoriteButton.Visibility != Visibility.Visible)
                    FavoriteButton.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Reset the Scaling when canvas is resized.
        /// </summary>
        private void DropZone_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            if (scaleTransform.ScaleX != 1 || scaleTransform.ScaleY != 1)
            {
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;

                var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
                translateTransform.X = 0;
                translateTransform.Y = 0;
            }
        }

        /// <summary>
        /// Handles video toggle pause && kill app.
        /// </summary>
        private void DropZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;
            
            if (e.ChangedButton == MouseButton.Left)
            {
                if (VideoControl.SourceProvider.MediaPlayer.IsPlaying())
                {
                    VideoControl.SourceProvider.MediaPlayer.Pause();
                    ThumbButton.ImageSource = Application.Current.Resources["PlayImage"] as BitmapImage;
                }
                else
                {
                    VideoControl.SourceProvider.MediaPlayer.Play();
                    ThumbButton.ImageSource = Application.Current.Resources["PauseImage"] as BitmapImage;
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                VideoControl.SourceProvider.Dispose();
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Zoom feature implementation.
        /// </summary>
        private void DropZone_MouseWheel(object sender, MouseWheelEventArgs args)
        {
            if (!this.HasEffectiveKeyboardFocus) return;

            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var zoom = args.Delta > 0 ? ZoomRatio : -ZoomRatio;
            var relativePointCache = VideoControl.TranslatePoint(new Point(0, 0), DropZone);

            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            var height = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var width = VideoControl.ActualWidth * scaleTransform.ScaleX;

            if (width + zoom < this.ActualWidth && height + zoom < this.ActualHeight && zoom < 0)
            {
                translateTransform.X = 0;
                translateTransform.Y = 0;
                return;
            }

            if (scaleTransform.ScaleX + zoom > ZoomMinTreshold && scaleTransform.ScaleX + zoom < ZoomMaxTreshold &&
                scaleTransform.ScaleY + zoom > ZoomMinTreshold && scaleTransform.ScaleY + zoom < ZoomMaxTreshold)
            {
                scaleTransform.ScaleX += zoom;
                scaleTransform.ScaleY += zoom;
            }

            if (zoom > 0) return;

            height = VideoControl.ActualHeight * scaleTransform.ScaleY;
            width = VideoControl.ActualWidth * scaleTransform.ScaleX;
            var relativePoint = VideoControl.TransformToVisual(DropZone).Transform(new Point(0, 0));
            var mathRelativePositionY = Math.Round(relativePoint.Y - relativePointCache.Y, MidpointRounding.AwayFromZero);
            var mathRelativePositionX = Math.Round(relativePoint.X - relativePointCache.X, MidpointRounding.AwayFromZero);
            var mathTranslateY = Math.Round(translateTransform.Y, MidpointRounding.AwayFromZero);
            var mathTranslateX = Math.Round(translateTransform.X, MidpointRounding.AwayFromZero);

            if (height < DropZone.ActualHeight)
                translateTransform.Y = 0;
            else if (relativePoint.Y > 0)
                translateTransform.Y = mathTranslateY - mathRelativePositionY;
            else if (relativePoint.Y + height < DropZone.ActualHeight)
                translateTransform.Y = mathTranslateY + mathRelativePositionY;

            if (width < DropZone.ActualWidth)
                translateTransform.X = 0;
            else if (relativePoint.X > 0)
                translateTransform.X = mathTranslateX - mathRelativePositionX;
            else if (relativePoint.X + width < DropZone.ActualWidth)
                translateTransform.X = mathTranslateX + mathRelativePositionX;
        }

        /// <summary>
        /// Drop feature implementation.
        /// </summary>
        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length > 1) return;
                CurrentFile = new Uri(files.First());
                Play(CurrentFile);
            }
        }

        /// <summary>
        /// Move the media element with the mouse.
        /// </summary>
        private void DropZone_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!DropZone.IsMouseCaptured) return;
           
            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var controlHeight = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var controlWidth = VideoControl.ActualWidth * scaleTransform.ScaleX;

            Point relativePoint = VideoControl.TransformToVisual(DropZone).Transform(new Point(0, 0));
            var mathRelativeY = Math.Round(relativePoint.Y, MidpointRounding.AwayFromZero);
            var mathRelativeX = Math.Round(relativePoint.X, MidpointRounding.AwayFromZero);
            var mathMouseY = Math.Round(MousePosition.Y, MidpointRounding.AwayFromZero);
            var mathMouseX = Math.Round(MousePosition.X, MidpointRounding.AwayFromZero);

            Vector vector = new Vector { X = 0, Y = 0 };
            if (controlHeight >= DropZone.ActualHeight)
            {
                var vectorY = Math.Round(e.GetPosition(DropZone).Y, MidpointRounding.AwayFromZero) - mathMouseY;
                if (mathRelativeY <= 0 && mathRelativeY + vectorY <= 0 && vectorY > 0 || mathRelativeY + controlHeight >= DropZone.ActualHeight && mathRelativeY + controlHeight + vectorY >= DropZone.ActualHeight && vectorY < 0)
                    vector.Y = vectorY;
            }
            if (controlWidth >= DropZone.ActualWidth)
            {
                var vectorX = Math.Round(e.GetPosition(DropZone).X, MidpointRounding.AwayFromZero) - mathMouseX;
                if (mathRelativeX <= 0 && mathRelativeX + vectorX <= 0 && vectorX > 0 || mathRelativeX + controlWidth >= DropZone.ActualWidth && mathRelativeX + controlWidth + vectorX >= DropZone.ActualWidth && vectorX < 0)
                    vector.X = vectorX;
            }

            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            translateTransform.X = OriginPosition.X + vector.X;
            translateTransform.Y = OriginPosition.Y + vector.Y;

            MousePosition = e.GetPosition(DropZone);
            OriginPosition = new Point(Math.Round(translateTransform.X, MidpointRounding.AwayFromZero), Math.Round(translateTransform.Y, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Release the dragged media element.
        /// </summary>
        private void DropZone_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropZone.ReleaseMouseCapture();
            VideoControl.Cursor = Cursors.Arrow;
            SystemParametersInfo(0x0071, 0, OriginMouseSpeed, 0);
        }

        /// <summary>
        /// Defines the media element to drag.
        /// </summary>
        private void DropZone_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!VideoControl.SourceProvider.MediaPlayer.CouldPlay) return;

            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var elementHeight = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var elementWidth = VideoControl.ActualWidth * scaleTransform.ScaleX;

            if (elementWidth <= DropZone.ActualWidth && elementHeight <= DropZone.ActualHeight) return;

            VideoControl.Cursor = Cursors.SizeAll;
            DropZone.CaptureMouse();

            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            MousePosition = e.GetPosition(DropZone);
            OriginPosition = new Point(Math.Round(translateTransform.X, MidpointRounding.AwayFromZero), Math.Round(translateTransform.Y, MidpointRounding.AwayFromZero));

            SystemParametersInfo(0x0071, 0, 3, 0);
        }

        /// <summary>
        /// Shows media buttons.
        /// </summary>
        private void MediaGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!VideoControl.SourceProvider.MediaPlayer.CouldPlay) return;

            if (UseAnimation)
            {
                AnimationTools.FadeIn(ResizeButton);
                AnimationTools.FadeIn(FindMediaButton);
                AnimationTools.FadeIn(MaximizeButton);
            }
            else
            {
                ResizeButton.Visibility = Visibility.Visible;
                FindMediaButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides media buttons.
        /// </summary>
        private void MediaGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (UseAnimation)
            {
                AnimationTools.FadeOut(ResizeButton);
                AnimationTools.FadeOut(FindMediaButton);
                AnimationTools.FadeOut(MaximizeButton);
            }
            else
            {
                ResizeButton.Visibility = Visibility.Hidden;
                FindMediaButton.Visibility = Visibility.Hidden;
                MaximizeButton.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Shows tool buttons.
        /// </summary>
        private void ToolGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!VideoControl.SourceProvider.MediaPlayer.CouldPlay) return;

            if (UseAnimation)
            {
                AnimationTools.FadeIn(ToggleRepeatButton);
            }
            else
            {
                ToggleRepeatButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides tool buttons.
        /// </summary>
        private void ToolGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (UseAnimation)
            {
                AnimationTools.FadeOut(ToggleRepeatButton);
            }
            else
            {
                ToggleRepeatButton.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Shows sound buttons.
        /// </summary>
        private void SoundGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!VideoControl.SourceProvider.MediaPlayer.CouldPlay) return;

            if (UseAnimation)
            {
                AnimationTools.FadeIn(VolumeSlider);
                AnimationTools.FadeIn(ToggleMuteButton);
            }
            else
            {
                VolumeSlider.Visibility = Visibility.Visible;
                ToggleMuteButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides sound buttons.
        /// </summary>
        private void SoundGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (UseAnimation)
            {
                AnimationTools.FadeOut(VolumeSlider);
                AnimationTools.FadeOut(ToggleMuteButton);
            }
            else
            {
                VolumeSlider.Visibility = Visibility.Hidden;
                ToggleMuteButton.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Shows time slider.
        /// </summary>
        private void TimeGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!VideoControl.SourceProvider.MediaPlayer.CouldPlay) return;

            if (UseAnimation)
            {
                AnimationTools.FadeIn(TimeSlider);
            }
            else
            {
                TimeSlider.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides time slider.
        /// </summary>
        private void TimeGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (UseAnimation)
            {
                AnimationTools.FadeOut(TimeSlider);
            }
            else
            {
                TimeSlider.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Defines the buttons and their action.
        /// </summary>
        private void MediaButton_ButtonClick(object sender, RoutedEventArgs e)
        {
            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            translateTransform.X = 0;
            translateTransform.Y = 0;

            switch ((sender as Button)?.Name)
            {
                case "ResizeButton":
                    scaleTransform.ScaleY = 1;
                    scaleTransform.ScaleX = 1;
                    break;

                case "FindMediaButton":
                case "FindMediaButtonBis":
                    if (VideoControl.SourceProvider.MediaPlayer.IsPlaying())
                        VideoControl.SourceProvider.MediaPlayer.Pause();

                    var dialog = new System.Windows.Forms.OpenFileDialog { Filter = "Tous les fichiers vidéos|*.avi;*.mp4;*.wmv|Tous les fichiers (*.*)|*.*" };
                    dialog.ShowDialog();
                    if (dialog.FileName == string.Empty)
                        break;

                    CurrentFile = new Uri(dialog.FileName);
                    Play(CurrentFile);
                    break;

                case "ToggleMuteButton":
                    var currentVolume = VideoControl.SourceProvider.MediaPlayer.Audio.Volume;
                    if (currentVolume > 0)
                    {
                        VolumeSlider.Value = 0;
                        MuteIcon.Kind = PackIconKind.VolumeMute;
                    }
                    else
                    {
                        VolumeSlider.Value = 100;
                        MuteIcon.Kind = PackIconKind.VolumeHigh;
                    }

                    break;

                case "MaximizeButton":
                    var isMaximized = WindowState != WindowState.Maximized;
                    WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal;
                    MaximizeIcon.Kind = isMaximized ? PackIconKind.ArrowCollapse : PackIconKind.ArrowExpand;
                    break;

                case "ToggleRepeatButton":
                    IsRepeating = !IsRepeating;
                    RepeatIcon.Kind = IsRepeating ? PackIconKind.Repeat : PackIconKind.RepeatOff;
                    break;
            }
        }

        /// <summary>
        /// Update the volume.
        /// </summary>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var value = (int)Math.Floor(e.NewValue);
            Dispatcher.Invoke(() => { VideoControl.SourceProvider.MediaPlayer.Audio.Volume = value; });

            if (value > 0 && value < 100)
                MuteIcon.Kind = PackIconKind.VolumeMedium;
            else if (value >= 100)
                MuteIcon.Kind = PackIconKind.VolumeHigh;
            else
                MuteIcon.Kind = PackIconKind.VolumeMute;
        }

        /// <summary>
        /// Update the position of video.
        /// </summary>
        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TimeSlider.IsMouseCaptured)
                Dispatcher.Invoke(() => { VideoControl.SourceProvider.MediaPlayer.Position = (float) e.NewValue; });
        }

        /// <summary>
        /// Handles error on MediaPlayer.
        /// </summary>
        private void MediaPlayer_EncounteredError(object sender, VlcMediaPlayerEncounteredErrorEventArgs e)
        {
		    var result = MessageBox.Show($"{(sender as VlcMediaPlayer).State.ToString()}", "An error occured", MessageBoxButton.OK);
            if (result != MessageBoxResult.OK) return;
            VideoControl.SourceProvider.Dispose();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// When user click on thumb button.
        /// </summary>
        private void ThumbButton_Click(object sender, EventArgs e)
        {
            if (VideoControl.SourceProvider.MediaPlayer.IsPlaying())
            {
                VideoControl.SourceProvider.MediaPlayer.Pause();
                ThumbButton.ImageSource = Application.Current.Resources["PlayImage"] as BitmapImage;
            }
            else
            {
                VideoControl.SourceProvider.MediaPlayer.Play();
                ThumbButton.ImageSource = Application.Current.Resources["PauseImage"] as BitmapImage;
            }
        }

        /// <summary>
        /// Capture the mouse for the position slider.
        /// </summary>
        private void TimeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TimeSlider.CaptureMouse();
        }

        /// <summary>
        /// Release the mouse for the position slider.
        /// </summary>
        private void TimeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            TimeSlider.ReleaseMouseCapture();
        }

        /// <summary>
        /// Toggle TopMost on window.
        /// </summary>
        private void FavoriteButton_Click(object sender, EventArgs e)
        {
            if (Topmost)
                FavoriteButton.ImageSource = Application.Current.Resources["BookmarkImage"] as BitmapImage;
            else
                FavoriteButton.ImageSource = Application.Current.Resources["BookmarkedImage"] as BitmapImage;

            Topmost = !Topmost;
        }

        #endregion //Events

        #region Methods

        /// <summary>
        /// Private Play method.
        /// </summary>
        /// <param name="uri"></param>
        private void Play(Uri uri)
        {
            if (DropMenu.Visibility == Visibility.Visible)
                DropMenu.Visibility = Visibility.Collapsed;

            var filename = new FileInfo(uri.PathAndQuery);
            string[] mediaExtensions = { ".AVI", ".MP4", ".WMV", };
            if (!mediaExtensions.Contains(filename.Extension.ToUpper()))
                return;

            VideoControl.SourceProvider.MediaPlayer.Play(uri);
            TimeSlider.Value = 0;
            VolumeSlider.Value = VideoControl.SourceProvider.MediaPlayer.Audio.Volume;
            if (VolumeSlider.Value == 0)
                MuteIcon.Kind = PackIconKind.VolumeMute;
            else if (VolumeSlider.Value > 0 && VolumeSlider.Value < 100)
                MuteIcon.Kind = PackIconKind.VolumeMedium;
            else
                MuteIcon.Kind = PackIconKind.VolumeHigh;

            Keyboard.Focus(this);
        }

        #endregion //Methods
    }
}
