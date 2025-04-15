﻿using System.Text;
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

namespace Twitter_Trends.UI
{
    public partial class MainWindow : Window
    {
        private DataParser parser = new DataParser();
        private List<Tweet> tweets;
        private Dictionary<string, double> sentiments;
        private List<State> states;

        public MainWindow()
        {
            InitializeComponent();
            InitMap();
        }

        private void InitMap()
        {
            // Создание нового экземпляра карты
            var map = new Mapsui.Map();

            // Добавление слоев карты
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // Присвоение карты элементу MapControl
            MapControl.Children.Clear();  // Очистить все старые элементы
           // MapControl.Children.Add(Map); // Добавить карту в MapControl
        }

        private void LoadTweets_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("Text files|*.txt");
            if (filePath != null)
            {
                tweets = DataParser.LoadTweets(filePath);
                MessageBox.Show($"Загружено твитов: {tweets.Count}");
            }
        }
        
        private void LoadSentiments_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("Excel files|*.csv");
            if (filePath != null)
            {
                sentiments = DataParser.LoadSentiments(filePath);
                MessageBox.Show($"Загружено сентиментов: {sentiments.Count}");
            }
        }

        private void LoadStates_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowFileDialog("JSON files|*.json");
            if (filePath != null)
            {
                states = DataParser.LoadStates(filePath);
                MessageBox.Show($"Загружено штатов: {states.Count}");
                // Например, здесь можно добавить отрисовку на карте
            }
        }

        private string ShowFileDialog(string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}