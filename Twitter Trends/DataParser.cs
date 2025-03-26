using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Twitter_Trends
{
    public class DataParser
    {
        public static List<Tweet> LoadTweets(string tweetsPath)
        {
            var tweets = new List<Tweet>();

            if (!Directory.Exists(tweetsPath))
            {
                Console.WriteLine($"Папка {tweetsPath} не найдена.");
                return tweets;
            }

            // Получаем все файлы с расширением .txt в папке
            var txtFiles = Directory.GetFiles(tweetsPath, "*.txt");

            foreach (var filePath in txtFiles)
            {
                // Считываем все строки файла
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Разделяем строку по символу табуляции
                    var parts = line.Split('\t');

                    //  "Location"    "_"   "DateTime"   "Text"
                    if (parts.Length < 4)
                        continue;

                    // Очистка кооридинат от лишних символов
                    var coordinatePart = parts[0].Trim().Trim('[', ']');

                    // Разделяем по запятой, чтобы получить lat и lon
                    var coords = coordinatePart.Split(',');
                    if (coords.Length < 2)
                        continue;

                    if (!double.TryParse(coords[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude))
                        continue;
                    if (!double.TryParse(coords[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
                        continue;


                    var datePart = parts[2].Trim();
                    DateTime.TryParse(datePart, out DateTime tweetDate);

                    // Текст твита – всё, что в parts[3]
                    var tweetText = parts[3].Trim();

                    tweets.Add(new Tweet
                    {
                        Text = tweetText,
                        Location = new Point(latitude, longitude),
                        TweetDate = tweetDate
                    });
                }
            }

            Console.WriteLine(tweets.Count + " tweets\n");

            return tweets;
        }

        public static Dictionary<string, double> LoadSentiments(string sentimentsPath)
        {
            var sentiments = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(sentimentsPath))
            {
                Console.WriteLine($"Файл {sentimentsPath} не найден.");
                return sentiments;
            }

            var lines = File.ReadAllLines(sentimentsPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 2)
                    continue;

                string word = parts[0].Trim();
                if (double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                {
                    sentiments[word] = weight;
                }
            }

            Console.WriteLine(sentiments.Count + " sentiments\n");

            return sentiments;
        }

        public static List<State> LoadStates(string statesPath)
        {
            if (!File.Exists(statesPath))
            {
                Console.WriteLine($"Файл {statesPath} не найден.");
                return new List<State>();
            }

            string jsonContent = File.ReadAllText(statesPath);

            try
            {
                var states = JsonSerializer.Deserialize<List<State>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine(states.Count + " states\n");

                return states ?? new List<State>();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Ошибка при разборе JSON: " + exception.Message);

                return new List<State>();
            }
        }
    }
}
