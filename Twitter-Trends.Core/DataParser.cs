using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

            var JSONContent = File.ReadAllText(statesPath);
            var doc = JsonDocument.Parse(JSONContent);
            var root = doc.RootElement;

            foreach (var stateProperty in root.EnumerateObject())
            {
                string stateName = stateProperty.Name;
                JsonElement state = stateProperty.Value;

                // Проверяем уровень вложенности
                if (TryParseNestedLists(state, out List<List<List<List<double>>>> parsedData))
                {
                    states.Add(new State(stateName, ConvertToMultiPolygon(parsedData)));
                }
                else
                {
                    Console.WriteLine($"Ошибка в данных штата {stateName}: неверная структура");
                }
            }

            Console.WriteLine(states.Count + " states:\n");

            return states;
        }

        public static bool TryParseNestedLists(JsonElement element, out List<List<List<List<double>>>> result)
        {
            result = new List<List<List<List<double>>>>();
            var UpperList = new List<List<List<double>>>();
            var MiddleList = new List<List<double>>();
            var LowerList = new List<double>();

            // Если элемент не массив, возвращаем false
            if (element.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var level1 in element.EnumerateArray())
            {
                if (level1.ValueKind == JsonValueKind.Array)
                {
                    foreach (var level2 in level1.EnumerateArray())
                    {
                        if (level2.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var level3 in level2.EnumerateArray())
                            {
                                if (level3.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var level4 in level3.EnumerateArray())
                                    {
                                        if (level4.ValueKind == JsonValueKind.Number)
                                        {
                                            LowerList.Add(level4.GetSingle());
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    }
                                }
                                else if (level3.ValueKind == JsonValueKind.Number)
                                {
                                    LowerList.Add(level3.GetSingle());
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                MiddleList.Add(LowerList);
                UpperList.Add(MiddleList);
                result.Add(UpperList);

            }

            return true;
        }

        public static MultiPolygon ConvertToMultiPolygon(List<List<List<List<double>>>> coordinates)
        {
            var polygons = new List<Polygon>();

            // Проход по всем полигонам
            foreach (var polygonCoords in coordinates)
            {
                var rings = new List<LinearRing>();

                // Проход по всем кольцам полигона (внешнее + дырки)
                foreach (var ringCoords in polygonCoords)
                {
                    var points = new List<Coordinate>();

                    // Преобразование координат
                    foreach (var point in ringCoords)
                    {
                        if (point.Count >= 2)
                        {
                            double x = point[0];
                            double y = point[1];
                            points.Add(new Coordinate(x, y));
                        }
                    }

                    // Проверка замкнутости (первая = последняя точка)
                    if (points.Count > 0 && !points[0].Equals(points[^1]))
                    {
                        points.Add(new Coordinate(points[0].X, points[0].Y));
                    }

                    if (points.Count >= 4) // Минимум 4 точки для кольца
                    {
                        rings.Add(new LinearRing(points.ToArray()));
                    }
                }

                if (rings.Count > 0)
                {
                    // Первое кольцо - внешний контур, остальные - дырки
                    var shell = rings[0];
                    var holes = rings.Count > 1 ? rings.Skip(1).ToArray() : null;
                    polygons.Add(new Polygon(shell, holes));
                }
            }

            return new MultiPolygon(polygons.ToArray());
        }
    }
}

