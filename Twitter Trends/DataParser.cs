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
            var states = new List<State>();

            if (!File.Exists(statesPath))
            {
                Console.WriteLine($"Файл {statesPath} не найден.");
                return states;
            }

            string json = File.ReadAllText(statesPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            // Десериализация в словарь с JsonElement 
            var statesData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);

            var factory = new GeometryFactory();

            foreach (var stateEntry in statesData)
            {

                var polygons = new List<Polygon>();

                // Обработка массива полигонов для каждого штата
                foreach (var polygonElement in stateEntry.Value.EnumerateArray())
                {
                    var rings = new List<LinearRing>();

                    // Обработка каждого кольца в полигоне
                    foreach (var ringElement in polygonElement.EnumerateArray())
                    {
                        var coordinates = new List<Coordinate>();

                        // Обработка каждой точки в кольце
                        foreach (var pointElement in ringElement.EnumerateArray())
                        {
                            if (pointElement.ValueKind == JsonValueKind.Array && pointElement.GetArrayLength() >= 2)
                            {
                                var x = pointElement[0].GetDouble();
                                var y = pointElement[1].GetDouble();
                                coordinates.Add(new Coordinate(x, y));
                            }
                        }

                        // Создание кольца (минимум 4 точки, включая замыкающую)
                        if (coordinates.Count >= 3)
                        {
                            // Замыкаем полигон, если не замкнут
                            if (!coordinates[0].Equals2D(coordinates[^1]))
                                coordinates.Add(new Coordinate(coordinates[0]));

                            rings.Add(factory.CreateLinearRing(coordinates.ToArray()));
                        }
                    }

                    // Создание полигона (первое кольцо - внешнее, остальные - отверстия)
                    if (rings.Count > 0)
                    {
                        var shell = rings[0];
                        var holes = rings.Skip(1).ToArray();
                        polygons.Add(factory.CreatePolygon(shell, holes));
                    }
                }

                // Создание мультиполигона для штата
                if (polygons.Count > 0)
                {
                    states.Add(new State
                    {
                        PostalCode = stateEntry.Key,
                        Shape = factory.CreateMultiPolygon(polygons.ToArray())
                    });
                }
            }

            Console.WriteLine(states.Count + " states\n");

            return states;
        }
    }
}

