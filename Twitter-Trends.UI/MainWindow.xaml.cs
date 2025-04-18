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
using Mapsui;

namespace Twitter_Trends.UI
{
    public partial class MainWindow : Window
    {
        private List<Tweet> tweets;
        private Dictionary<string, double> sentiments;
        private List<State> states;
        private Dictionary<string, double?> stateSentiments;

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

                FileNameTextBlock.Text = $"Загружен файл: {System.IO.Path.GetFileName(filePath)}";
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
            if (tweets != null && sentiments != null && states != null)
            {
                var existingLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Tweets");
                if (existingLayer != null)
                {
                    MapControl.Map.Layers.Remove(existingLayer);
                }

                tweets = DataAnalyzer.AnalyzeTweetsSentiment(tweets, sentiments);
                stateSentiments = DataAnalyzer.CalculateStateSentiment(DataAnalyzer.GroupTweetsByState(tweets, states));
                await MapDrawer.ColorStatesBySentimentAsync(stateSentiments, MapControl.Map);
                await MapDrawer.AddTweetPointsToMap(tweets, MapControl.Map);

                MapControl.RefreshGraphics(); // <-- Чтобы перерисовать карту
            }
            else
            {
                string missing = "";

                if (tweets == null)
                    missing += "- Твиты\n";
                if (sentiments == null)
                    missing += "- Сентименты\n";
                if (states == null)
                    missing += "- Штаты\n";

                MessageBox.Show($"Перед раскраской карты необходимо загрузить:\n{missing}",
                                "Недостаточно данных",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
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
                MapControl.Map.BackColor = Mapsui.Styles.Color.FromString("#2A2A3A");
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
            MapControl.Map.BackColor = Mapsui.Styles.Color.FromString("#2A2A3A");
            MapControl.ZoomToBox(new MPoint(-180, 30), new MPoint(-50, 50));
        }
    }
}