using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
//using Twitter-Trends.Core;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.UI.Wpf;
using Mapsui.UI;

namespace Twitter_Trends.UI
{
    public partial class MainWindow : Window
    {
        private List<Tweet> tweets;
        private Dictionary<string, double> sentiments;
        private List<State> states;

        public MainWindow()
        {
            InitializeComponent();
            MapControl.Map.BackColor = Mapsui.Styles.Color.FromString("#2A2A3A");
        }

        private void LoadTweets_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("Text files|*.txt");
            if (filePath != null)
            {
                tweets = DataParser.LoadTweets(filePath);
                TweetsCountText.Text = $"Твиты: {tweets.Count}";
            }
        }

        private void LoadSentiments_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("Excel files|*.csv");
            if (filePath != null)
            {
                sentiments = DataParser.LoadSentiments(filePath);
                SentimentsCountText.Text = $"Сентименты: {sentiments.Count}";
            }
        }

        private async void PaintMap_Click(object sender, RoutedEventArgs e)
        {
            tweets = DataAnalyzer.AnalyzeTweetsSentiment(tweets, sentiments);
            await MapDrawer.ColorStatesBySentimentAsync(DataAnalyzer.CalculateStateSentiment(DataAnalyzer.GroupTweetsByState(tweets, states)), MapControl.Map);

            MapControl.RefreshGraphics(); // <-- Чтобы перерисовать карту
        }

        private void LoadStates_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("JSON files|*.json");
            if (filePath != null)
            {
                states = DataParser.LoadStates(filePath);
                StatesCountText.Text = $"Штатов: {states.Count}";

                // Создаём карту с отрисованными штатами
                var map = MapDrawer.CreateMapWithStateLayers(states);
                MapControl.Map = map; // Важно!
                MapControl.ZoomToBox(new MPoint(-180, 30), new MPoint(-50, 50));
            }
        }

        private string ShowFileDialog(string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private async void UnPaintMap_Click(object sender, RoutedEventArgs e)
        {
            tweets = null;
            sentiments = null;
            MapControl.RefreshGraphics();
            SentimentsCountText.Text = $"Сентименты: {0}";
            TweetsCountText.Text = $"Твиты: {0}";

            var map = MapDrawer.CreateMapWithStateLayers(states);
            MapControl.Map = map; 
            MapControl.ZoomToBox(new MPoint(-180, 30), new MPoint(-50, 50));
        }
    }
}