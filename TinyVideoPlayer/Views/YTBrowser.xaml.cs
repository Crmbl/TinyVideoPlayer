using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AngleSharp.Text;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using TinyVideoPlayer.Utils;

namespace TinyVideoPlayer.Views
{
    /// <summary>
    /// Interaction logic for YTBrowser.xaml
    /// </summary>
    public partial class YTBrowser : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

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

        #endregion //INotifyPropertyChanged

        #region Instance variables

        private bool _isSearching;

        private List<SearchResult> _channels;

        private List<SearchResult> _videos;

        private List<SearchResult> _playlists;

        private string _previousSearch;

        private string[] _oldSearches;

        private string _apiKey = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\apiKey"));

        #endregion //Instance variables

        #region Properties

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                NotifyPropertyChanged("IsSearching");
            }
        }

        public List<SearchResult> Playlists
        {
            get => _playlists;
            set
            {
                _playlists = value;
                NotifyPropertyChanged("Playlists");
            }
        }

        public List<SearchResult> Channels
        {
            get => _channels;
            set
            {
                _channels = value;
                NotifyPropertyChanged("Channels");
            }
        }

        public List<SearchResult> Videos
        {
            get => _videos;
            set
            {
                _videos = value;
                NotifyPropertyChanged("Videos");
            }
        }

        public string PreviousSearch
        {
            get => _previousSearch;
            set
            {
                _previousSearch = value;
                NotifyPropertyChanged("PreviousSearch");
            }
        }

        public MainWindow Main { get; set; }

        public string[] OldSearches
        {
            get => _oldSearches;
            set
            {
                _oldSearches = value;
                NotifyPropertyChanged("OldSearches");
            }
        }

        #endregion //Properties

        public YTBrowser(MainWindow main)
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Main = main;
            IsSearching = false;
            SearchBox.SelectedValue = null;
            SearchBox.KeyDown += SearchBox_KeyDown;
            SearchButton.Click += SearchButton_Click;
            SearchBox.Loaded += SearchBox_Loaded;
            SearchBox.PreviewTextInput += SearchBox_PreviewTextInput;

            OldSearches = new[] {""};
            if (File.Exists(string.Concat(Environment.CurrentDirectory, "\\searches")))
                OldSearches = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\searches"))
                    .Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Reverse().ToArray();
            else
                File.Create(string.Concat(Environment.CurrentDirectory, "\\searches"));
        }

        private void SearchBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString($"https://suggestqueries.google.com/complete/search?client=youtube&ds=yt&q={e.Text}&callback=suggestCallback");
                Console.WriteLine(json);
                //OldSearches = json.Split()
                //var test = JsonConvert.DeserializeObject<YoutubeSuggestion>(json);
            }
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            SearchBox.Focusable = true;
            Keyboard.Focus(SearchBox);
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsSearching || string.IsNullOrWhiteSpace(SearchBox.Text) || PreviousSearch == SearchBox.Text) return;

            await Search();
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || IsSearching || string.IsNullOrWhiteSpace(SearchBox.Text) || PreviousSearch == SearchBox.Text) return;

            await Search();
        }

        public async Task Search()
        {
            Scrollviewer.ScrollToTop();
            IsSearching = true;
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = this.GetType().ToString()
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = SearchBox.Text;
            searchListRequest.MaxResults = 50;
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.None;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            PreviousSearch = SearchBox.Text;
            WriteWebHistory(PreviousSearch);

            Videos = new List<SearchResult>();
            Channels = new List<SearchResult>();
            Playlists = new List<SearchResult>();
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        Videos.Add(searchResult);
                        break;

                    case "youtube#channel":
                        Channels.Add(searchResult);
                        break;

                    case "youtube#playlist":
                        Playlists.Add(searchResult);
                        break;
                }
            }

            ResultGrid.ItemsSource = Videos;
            IsSearching = false;
        }

        private void Play_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            Main.PlayYoutubeVideo($"https://www.youtube.com/watch?v={((ResourceId)button?.Tag)?.VideoId}");
            Close();
        }

        private void WriteWebHistory(string newSearch)
        {
            if (OldSearches.Contains(newSearch)) return;
            using (var fileStream = File.AppendText(string.Concat(Environment.CurrentDirectory, "\\searches")))
                fileStream.Write(newSearch + ";");

            OldSearches = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\searches"))
                .Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Reverse().ToArray();
        }
    }
}
