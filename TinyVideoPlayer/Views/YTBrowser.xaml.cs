using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AngleSharp.Common;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private List<string> _searches;

        private List<string> _suggestions;

        private List<string> _mixedValues;

        private string _apiKey = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\apiKey"));

        #endregion //Instance variables

        #region Properties

        public MainWindow Main { get; set; }

        public string CurrentSearch { get; set; }

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

        public List<string> Searches
        {
            get => _searches;
            set
            {
                _searches = value;
                NotifyPropertyChanged("Searches");
            }
        }

        public List<string> Suggestions
        {
            get => _suggestions;
            set
            {
                _suggestions = value;
                NotifyPropertyChanged("Suggestions");
            }
        }

        public List<string> MixedValues
        {
            get => _mixedValues;
            set
            {
                _mixedValues = value;
                NotifyPropertyChanged("MixedValues");
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
            Suggestions = new List<string>();
            Searches = new List<string>();

            SearchButton.Click += SearchButton_Click;
            SearchBox.KeyDown += SearchBox_KeyDown;
            SearchBox.Loaded += SearchBox_Loaded;

            if (File.Exists(string.Concat(Environment.CurrentDirectory, "\\searches")))
                Searches = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\searches"))
                    .Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Reverse().ToList();
            else
                File.Create(string.Concat(Environment.CurrentDirectory, "\\searches"));

            MixedValues = Searches.ToList();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchBox.SelectedItem = null;
            SearchBox.IsDropDownOpen = true;
            var textBox = SearchBox.Template.FindName("PART_EditableTextBox", SearchBox) as TextBox;
            textBox.SelectionLength = 0;
            textBox.CaretIndex = textBox.Text.Length;
            CurrentSearch = textBox.Text;

            if (string.IsNullOrWhiteSpace(CurrentSearch))
                MixedValues = Searches.ToList();
            else
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var response = await client.GetAsync($"https://suggestqueries.google.com/complete/search?&client=youtube&ds=yt&q={CurrentSearch}");
                        var jsonp = await response.Content.ReadAsStringAsync();
                        var json = jsonp.Replace("window.google.ac.h(", "").Replace(")", "");

                        if (!(JsonConvert.DeserializeObject(json) is JArray deserialized)) return;
                        Suggestions.Clear();
                        foreach (var suggestion in deserialized[1])
                            Suggestions.Add(suggestion.Values().GetItemByIndex(0).ToString());

                        if (Searches.Any())
                        {
                            var tmpList = Searches.Where(x => x.StartsWith(CurrentSearch.ToLower())).Take(3).ToList();
                            tmpList.AddRange(Suggestions.Where(x => !tmpList.Contains(x.ToLower())).Take(10 - tmpList.Count));
                            MixedValues = tmpList;
                        }
                        else
                        {
                            MixedValues = Suggestions.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"Message :{ex.Message}");
                    }
                }
            }
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            SearchBox.Focusable = true;
            Keyboard.Focus(SearchBox);
            SearchBox.IsDropDownOpen = true;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsSearching || string.IsNullOrWhiteSpace(CurrentSearch) || PreviousSearch == CurrentSearch) return;
            await Search();
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || IsSearching || string.IsNullOrWhiteSpace(CurrentSearch) || PreviousSearch == CurrentSearch) return;
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
            searchListRequest.Q = CurrentSearch;
            searchListRequest.MaxResults = 50;
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.None;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            PreviousSearch = CurrentSearch;
            WriteWebHistory(PreviousSearch.ToLower());

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
            if (Searches.Contains(newSearch)) return;
            using (var fileStream = File.AppendText(string.Concat(Environment.CurrentDirectory, "\\searches")))
                fileStream.Write(newSearch + ";");

            Searches = File.ReadAllText(string.Concat(Environment.CurrentDirectory, "\\searches"))
                .Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Reverse().ToList();
            MixedValues = Searches.ToList();
        }
    }
}
