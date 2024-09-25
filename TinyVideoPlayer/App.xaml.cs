using System.Windows;

namespace TinyVideoPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Arg { get; set; }

        public App() {}

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length != 1) return;
            Arg = e.Args[0];
        }
    }
}
