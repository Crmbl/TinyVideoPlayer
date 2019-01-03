using System;
using System.IO;
using System.Windows;

namespace TinyVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vlcLibDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            var options = new string[]
            {
                // VLC options can be given here. Please refer to the VLC command line documentation.
                "--file-logging", "-vvv", "--extraintf=logger", "--logfile=Logs.log"
            };

            this.VideoControl.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            // Load libvlc libraries and initializes stuff. It is important that the options (if you want to pass any) and lib directory are given before calling this method.

            var pathToVideo = @"C:\Users\SCHAEFAX\Documents\Perso\testVid.webm";
            var test = new FileInfo(pathToVideo);
            //pathToVideo = "http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_h264.mov";
            this.VideoControl.SourceProvider.MediaPlayer.Play("http://www.youtube.com/watch?v=vpU6jz301MQ");
        }
    }
}
