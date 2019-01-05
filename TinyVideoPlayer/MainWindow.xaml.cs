﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Vlc.DotNet.Core;

namespace TinyVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window/*, INotifyPropertyChanged*/
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

        #endregion //Properties

        //#region NotifyPropertyChanged

        ///// <summary>
        ///// The <see cref="INotifyPropertyChanged"/> event handler.
        ///// </summary>
        //public event PropertyChangedEventHandler PropertyChanged;

        ///// <summary>
        ///// Raise the modification event.
        ///// </summary>
        ///// <param name="propertyName"></param>
        //internal void NotifyPropertyChanged(string propertyName = "")
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //#endregion //NotifyPropertyChanged

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            IsRepeating = true;

            var vlcLibDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            var options = new []
            {
                /*https://wiki.videolan.org/Documentation:Command_line/ */
                "--file-logging", "-vvv", "--extraintf=logger", "--logfile=Logs.log" 
            };
            VideoControl.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            VideoControl.SetBinding(Canvas.TopProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "top",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = VideoControl },
                    new Binding("ActualHeight") { Source = VideoControl }
                }
            });
            VideoControl.SetBinding(Canvas.LeftProperty, new MultiBinding
            {
                Converter = new CenterConverter(),
                ConverterParameter = "left",
                Mode = BindingMode.TwoWay,
                Bindings = {
                    new Binding("ActualWidth") { Source = DropZone },
                    new Binding("ActualHeight") { Source = DropZone },
                    new Binding("ActualWidth") { Source = VideoControl },
                    new Binding("ActualHeight") { Source = VideoControl }
                }
            });
            VideoControl.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new TranslateTransform(),
                    new ScaleTransform()
                }
            };

            VideoControl.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
            DropZone.MouseWheel += DropZone_MouseWheel;
            DropZone.Drop += DropZone_Drop;
            DropZone.SizeChanged += DropZone_SizeChanged;
            DropZone.MouseLeftButtonDown += DropZone_MouseLeftButtonDown;
            VideoControl.MouseMove += VideoControl_MouseMove;
            VideoControl.MouseLeftButtonUp += DropZone_MouseLeftButtonUp;

            //CurrentFile = new Uri(@"C:\Users\axels\Downloads\Nouveau dossier\_.webm");
            //CurrentFile = new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_h264.mov");
            //VideoControl.SourceProvider.MediaPlayer.Play(CurrentFile);
        }

        /// <summary>
        /// Used for the repeat feature.
        /// </summary>
        delegate void VlcRepeatDelegate(Uri fileUri, string[] pars);

        #region Events

        /// <summary>
        /// Repeat feature implementation.
        /// </summary>
        private void MediaPlayer_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            if (!IsRepeating) return;

            VlcRepeatDelegate vlcDelegate = VideoControl.SourceProvider.MediaPlayer.Play;
            vlcDelegate.BeginInvoke(CurrentFile, new string[] {}, null, null);
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

            #region Dont need?

            //var height = VideoControl.ActualHeight * scaleTransform.ScaleY;
            //var width = VideoControl.ActualWidth * scaleTransform.ScaleX;
            //var relativePoint = VideoControl.TranslatePoint(new Point(0, 0), DropZone);

            //var previousHeight = Math.Round(e.PreviousSize.Height, 2);
            //var previousWidth = Math.Round(e.PreviousSize.Width, 2);
            //var newHeight = Math.Round(e.NewSize.Height, 2);
            //var newWidth = Math.Round(e.NewSize.Width, 2);

            //if (e.NewSize.Height > e.PreviousSize.Height)
            //{
            //    if (height < DropZone.ActualHeight)
            //        translateTransform.Y = 0;
            //    else if (height >= DropZone.ActualHeight)
            //    {
            //        if (relativePoint.Y > 0)
            //            translateTransform.Y -= Math.Round(newHeight - previousHeight, 2);
            //        else if (relativePoint.Y + height < DropZone.ActualHeight)
            //            translateTransform.Y += Math.Round(newHeight - previousHeight, 2);
            //    }
            //}

            //if (e.NewSize.Width > e.PreviousSize.Width)
            //{
            //    if (width < DropZone.ActualWidth)
            //        translateTransform.X = 0;
            //    else if (width >= DropZone.ActualWidth)
            //    {
            //        if (relativePoint.X > 0)
            //            translateTransform.X -= Math.Round(newWidth - previousWidth, 2);
            //        else if (relativePoint.X + width < DropZone.ActualWidth)
            //            translateTransform.X += Math.Round(newWidth - previousWidth, 2);
            //    }
            //}

            #endregion //Dont need?
        }

        /// <summary>
        /// Zoom feature implementation.
        /// </summary>
        private void DropZone_MouseWheel(object sender, MouseWheelEventArgs args)
        {
            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var zoom = args.Delta > 0 ? ZoomRatio : -ZoomRatio;
            var relativePointCache = VideoControl.TranslatePoint(new Point(0, 0), DropZone);

            if (scaleTransform.ScaleX + zoom > ZoomMinTreshold && scaleTransform.ScaleX + zoom < ZoomMaxTreshold &&
                scaleTransform.ScaleY + zoom > ZoomMinTreshold && scaleTransform.ScaleY + zoom < ZoomMaxTreshold)
            {
                scaleTransform.ScaleX += zoom;
                scaleTransform.ScaleY += zoom;
            }

            if (zoom > 0)
                return;

            var height = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var width = VideoControl.ActualWidth * scaleTransform.ScaleX;
            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            var relativePoint = VideoControl.TranslatePoint(new Point(0, 0), DropZone);
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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length > 1) return;

            CurrentFile = new Uri(files.First());
            VideoControl.SourceProvider.MediaPlayer.Play(CurrentFile);
        }

        /// <summary>
        /// Move the media element with the mouse.
        /// </summary>
        private void VideoControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!VideoControl.IsMouseCaptured) return;

            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var controlHeight = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var controlWidth = VideoControl.ActualWidth * scaleTransform.ScaleX;

            //TransformToAncestor
            Point relativePoint = VideoControl.TransformToVisual(DropZone).Transform(new Point(0, 0));
            var mathRelativeY = Math.Round(relativePoint.Y, MidpointRounding.AwayFromZero);
            var mathRelativeX = Math.Round(relativePoint.X, MidpointRounding.AwayFromZero);
            var mathMouseY = Math.Round(MousePosition.Y, MidpointRounding.AwayFromZero);
            var mathMouseX = Math.Round(MousePosition.X, MidpointRounding.AwayFromZero);

            Vector vector = new Vector { X = 0, Y = 0 };
            if (controlHeight >= DropZone.ActualHeight)
            {
                var vectorY = Math.Round(e.GetPosition(DropZone).Y, MidpointRounding.AwayFromZero) - mathMouseY;
                if (mathRelativeY <= 0 && mathRelativeY + vectorY <= 0 && vectorY > 0 ||
                    mathRelativeY + controlHeight >= DropZone.ActualHeight && mathRelativeY + controlHeight + vectorY >= DropZone.ActualHeight && vectorY < 0)
                    vector.Y = vectorY;
            }
            if (controlWidth >= DropZone.ActualWidth)
            {
                var vectorX = Math.Round(e.GetPosition(DropZone).X, MidpointRounding.AwayFromZero) - mathMouseX;
                if (mathRelativeX <= 0 && mathRelativeX + vectorX <= 0 && vectorX > 0 ||
                    mathRelativeX + controlWidth >= DropZone.ActualWidth && mathRelativeX + controlWidth + vectorX >= DropZone.ActualWidth && vectorX < 0)
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
        private void DropZone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            VideoControl.ReleaseMouseCapture();
            VideoControl.Cursor = Cursors.Arrow;
            SystemParametersInfo(0x0071, 0, OriginMouseSpeed, 0);
        }

        /// <summary>
        /// Defines the media element to drag.
        /// </summary>
        private void DropZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scaleTransform = (ScaleTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var elementHeight = VideoControl.ActualHeight * scaleTransform.ScaleY;
            var elementWidth = VideoControl.ActualWidth * scaleTransform.ScaleX;

            if (elementWidth <= DropZone.ActualWidth && elementHeight <= DropZone.ActualHeight) return;

            VideoControl.Cursor = Cursors.SizeAll;
            VideoControl.CaptureMouse();

            var translateTransform = (TranslateTransform)((TransformGroup)VideoControl.RenderTransform).Children.First(tr => tr is TranslateTransform);
            MousePosition = e.GetPosition(DropZone);
            OriginPosition = new Point(Math.Round(translateTransform.X, MidpointRounding.AwayFromZero), Math.Round(translateTransform.Y, MidpointRounding.AwayFromZero));

            OriginMouseSpeed = (uint)System.Windows.Forms.SystemInformation.MouseSpeed;
            SystemParametersInfo(0x0071, 0, 3, 0);
        }

        #endregion //Events
    }
}
