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
        public static List<Tweet> LoadTweets(string filePath)
        {
            var tweets = new List<Tweet>();

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

            return tweets;
        }

        public static Dictionary<string, double> LoadSentiments(string sentimentsPath)
        {
            var sentiments = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(sentimentsPath))
            {
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

            return sentiments;
        }

        public static List<State> LoadStates(string statesPath)
        {
            var states = new List<State>();

            if (!File.Exists(statesPath))
            {
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
                if (TryParseNestedLists(state, out List<List<List<double>>> parsedData))
                {
                    states.Add(new State(stateName, ConvertToMultiPolygon(parsedData)));
                }
            }

            return states;
        }

        private static bool TryParseNestedLists(JsonElement element, out List<List<List<double>>> result)
        {
            result = new List<List<List<double>>>();
            var polygon = new List<List<double>>();
            var coordinates = new List<double>();

            var isLevel4 = false;

            if (element.ValueKind != JsonValueKind.Array) { return false; }

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
                                    isLevel4 = true;

                                    foreach (var level4 in level3.EnumerateArray())
                                    {
                                        if (level4.ValueKind == JsonValueKind.Number)
                                        {
                                            coordinates.Add(level4.GetSingle());
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    }

                                    polygon.Add(new List<double>(coordinates));
                                    coordinates.Clear();
                                }
                                else if (level3.ValueKind == JsonValueKind.Number)
                                {
                                    coordinates.Add(level3.GetSingle());
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            if (!isLevel4)
                            {
                                polygon.Add(new List<double>(coordinates));
                                coordinates.Clear();
                            }
                        }
                    }

                    result.Add(new List<List<double>>(polygon));
                    polygon.Clear();
                }
            }

            return true;
        }

        public static MultiPolygon ConvertToMultiPolygon(List<List<List<double>>> coordinates)
        {
            var polygons = new List<Polygon>();

            foreach (var polygonCoords in coordinates)
            {
                var points = new List<Coordinate>();

                foreach (var point in polygonCoords)
                {
                    if (point.Count >= 2)
                    {
                        double x = point[0];
                        double y = point[1];
                        points.Add(new Coordinate(x, y));
                    }
                }

                // Замкнём кольцо, если не замкнуто
                if (points.Count > 0 && !points[0].Equals2D(points[^1]))
                {
                    points.Add(new Coordinate(points[0].X, points[0].Y));
                }

                if (points.Count >= 4)
                {
                    var shell = new LinearRing(points.ToArray());
                    polygons.Add(new Polygon(shell));
                }
            }

            return new MultiPolygon(polygons.ToArray());
        }

    }
}

