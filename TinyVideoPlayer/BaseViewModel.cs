using System.ComponentModel;

namespace TinyVideoPlayer
{
    /// <summary>
    /// BaseViewModel user for legacy.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The <see cref="INotifyPropertyChanged"/> event handler.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the modification event.
        /// </summary>
        /// <param name="propertyName"></param>
        internal void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
