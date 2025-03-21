using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TweetSentimentAnalysis
{
    /// <summary>
    /// Класс, представляющий твит с текстом, координатами и рассчитанной тональностью.
    /// </summary>
    public class Tweet
    {
        public string Text { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        // Можно добавить поле для даты, если нужно
        // public DateTime? TweetDate { get; set; }

        // Рассчитанная тональность твита; null, если не удалось вычислить
        public double? Sentiment { get; set; }
    }

    /// <summary>
    /// Класс, представляющий точку с координатами.
    /// </summary>
    public class Point
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Класс, представляющий штат с его кодом и географическими полигонами.
    /// Каждый полигон – это список точек.
    /// </summary>
    public class State
    {
        public string Code { get; set; }
        public List<List<Point>> Polygons { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Базовая директория (папка, где запущен .exe)
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Путь к папке "Data" в той же директории, что и приложение
            string dataFolderPath = Path.Combine(baseDirectory, "Data");

            // Путь к файлам sentiments.csv и states.json
            string sentimentsFilePath = Path.Combine(baseDirectory, "sentiments.csv");
            string statesFilePath = Path.Combine(baseDirectory, "states.json");

            // 1) Загружаем твиты из папки Data
            var tweets = LoadAllTweets(dataFolderPath);
            Console.WriteLine($"Загружено твитов: {tweets.Count}");

            // 2) Загрузка словаря тональностей
            var sentimentDictionary = LoadSentiments(sentimentsFilePath);
            Console.WriteLine($"Словарь тональностей загружен. Количество слов: {sentimentDictionary.Count}");

            // 3) Загрузка данных штатов
            var states = LoadStates(statesFilePath);
            Console.WriteLine($"Данные штатов загружены. Количество штатов: {states.Count}");

            // 4) Вычисляем центр для каждого штата
            var stateCenters = CalculateStateCenters(states);

            // 5) Анализ тональности каждого твита
            foreach (var tweet in tweets)
            {
                tweet.Sentiment = AnalyzeSentiment(tweet.Text, sentimentDictionary);
            }

            // 6) Группировка твитов по ближайшим штатам
            var tweetsByState = GroupTweetsByState(tweets, stateCenters);

            // 7) Вычисление средней тональности твитов по штатам
            var stateSentiments = CalculateStateSentiment(tweetsByState);

            // 8) Условная визуализация (вывод) результатов
            RenderUSMap(stateSentiments);

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Изменённый метод для вашего формата:
        /// Считывает все твиты из всех .txt-файлов в указанной папке.
        /// Формат строки (4 части, разделённые табуляцией):
        ///   [lat, lon]    _    2014-02-16 03:11:33    Текст твита
        /// </summary>
        /// <param name="directoryPath">Путь к папке, где лежат файлы *.txt</param>
        /// <returns>Список твитов</returns>
        public static List<Tweet> LoadAllTweets(string directoryPath)
        {
            var allTweets = new List<Tweet>();

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Папка {directoryPath} не найдена.");
                return allTweets;
            }

            // Получаем все файлы с расширением .txt в папке
            var txtFiles = Directory.GetFiles(directoryPath, "*.txt");

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
                    // Ожидаем не менее 4 частей: 
                    //  [lat, lon]    _    2014-02-16 03:11:33    Text
                    if (parts.Length < 4)
                        continue;

                    // Пример: parts[0] = "[38.29835689, -122.28522354]"
                    var coordinatePart = parts[0].Trim();
                    // Удаляем квадратные скобки
                    coordinatePart = coordinatePart.Trim('[', ']');

                    // Разделяем по запятой, чтобы получить lat и lon
                    var coords = coordinatePart.Split(',');
                    if (coords.Length < 2)
                        continue;

                    if (!double.TryParse(coords[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                        continue;
                    if (!double.TryParse(coords[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        continue;

                    // Если нужна дата, можно парсить parts[2]
                    // var datePart = parts[2].Trim();
                    // DateTime? tweetDate = null;
                    // if (DateTime.TryParse(datePart, out DateTime dt)) 
                    // {
                    //     tweetDate = dt;
                    // }

                    // Текст твита – всё, что в parts[3]
                    var tweetText = parts[3].Trim();

                    allTweets.Add(new Tweet
                    {
                        Text = tweetText,
                        Latitude = lat,
                        Longitude = lon
                        // TweetDate = tweetDate
                    });
                }
            }

            return allTweets;
        }

        /// <summary>
        /// Загрузка тональностей из CSV-файла.
        /// Каждая строка имеет формат: слово,вес
        /// </summary>
        /// <param name="filePath">Путь к файлу sentiments.csv</param>
        /// <returns>Словарь, где ключ – слово, значение – вес тональности</returns>
        public static Dictionary<string, double> LoadSentiments(string filePath)
        {
            var sentiments = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден.");
                return sentiments;
            }

            var lines = File.ReadAllLines(filePath);

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

        /// <summary>
        /// Загрузка географических данных штатов из JSON-файла.
        /// Формат файла должен соответствовать классу State.
        /// </summary>
        /// <param name="filePath">Путь к файлу states.json</param>
        /// <returns>Список объектов State</returns>
        public static List<State> LoadStates(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден.");
                return new List<State>();
            }

            string jsonContent = File.ReadAllText(filePath);

            try
            {
                var states = JsonSerializer.Deserialize<List<State>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return states ?? new List<State>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при разборе JSON: " + ex.Message);
                return new List<State>();
            }
        }

        /// <summary>
        /// Вычисление центра каждого штата как среднего арифметического всех точек во всех полигонах.
        /// </summary>
        /// <param name="states">Список штатов</param>
        /// <returns>Словарь, где ключ – код штата, значение – (средняя широта, средняя долгота)</returns>
        public static Dictionary<string, (double lat, double lon)> CalculateStateCenters(List<State> states)
        {
            var centers = new Dictionary<string, (double lat, double lon)>();

            foreach (var state in states)
            {
                double sumLat = 0;
                double sumLon = 0;
                int count = 0;

                if (state.Polygons != null)
                {
                    foreach (var polygon in state.Polygons)
                    {
                        foreach (var point in polygon)
                        {
                            sumLat += point.Latitude;
                            sumLon += point.Longitude;
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    centers[state.Code] = (sumLat / count, sumLon / count);
                }
            }

            return centers;
        }

        /// <summary>
        /// Разделяет текст твита на слова, оставляя только буквы.
        /// Использует регулярное выражение для поиска последовательностей символов Unicode, являющихся буквами.
        /// </summary>
        /// <param name="tweet">Текст твита</param>
        /// <returns>Список слов (токенов)</returns>
        public static List<string> TokenizeTweet(string tweet)
        {
            var matches = Regex.Matches(tweet, @"\p{L}+");
            return matches.Select(m => m.Value.ToLower()).ToList();
        }

        /// <summary>
        /// Анализирует тональность твита.
        /// Находит все слова твита в словаре тональностей и вычисляет среднее значение.
        /// Если ни одно слово не найдено, возвращает null.
        /// </summary>
        /// <param name="tweet">Текст твита</param>
        /// <param name="sentimentDictionary">Словарь тональностей</param>
        /// <returns>Средняя тональность или null</returns>
        public static double? AnalyzeSentiment(string tweet, Dictionary<string, double> sentimentDictionary)
        {
            var tokens = TokenizeTweet(tweet);
            double sum = 0;
            int count = 0;

            foreach (var token in tokens)
            {
                if (sentimentDictionary.TryGetValue(token, out double weight))
                {
                    sum += weight;
                    count++;
                }
            }

            if (count > 0)
                return sum / count;
            else
                return null;
        }

        /// <summary>
        /// Находит ближайший штат к заданным координатам твита (евклидова дистанция до центра штата).
        /// </summary>
        /// <param name="tweetLat">Широта твита</param>
        /// <param name="tweetLon">Долгота твита</param>
        /// <param name="stateCenters">Словарь центров штатов</param>
        /// <returns>Код ближайшего штата</returns>
        public static string FindNearestState(double tweetLat, double tweetLon, Dictionary<string, (double lat, double lon)> stateCenters)
        {
            string nearestState = null;
            double minDistanceSquared = double.MaxValue;

            foreach (var kvp in stateCenters)
            {
                double dLat = tweetLat - kvp.Value.lat;
                double dLon = tweetLon - kvp.Value.lon;
                double distanceSquared = dLat * dLat + dLon * dLon;

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearestState = kvp.Key;
                }
            }

            return nearestState;
        }

        /// <summary>
        /// Группирует твиты по ближайшим штатам.
        /// Для каждого твита определяется ближайший штат, и твит добавляется в словарь по коду штата.
        /// </summary>
        /// <param name="tweets">Список твитов</param>
        /// <param name="stateCenters">Словарь центров штатов</param>
        /// <returns>Словарь: ключ – код штата, значение – список твитов</returns>
        public static Dictionary<string, List<Tweet>> GroupTweetsByState(List<Tweet> tweets, Dictionary<string, (double lat, double lon)> stateCenters)
        {
            var tweetsByState = new Dictionary<string, List<Tweet>>();

            foreach (var tweet in tweets)
            {
                string nearestState = FindNearestState(tweet.Latitude, tweet.Longitude, stateCenters);
                if (nearestState != null)
                {
                    if (!tweetsByState.ContainsKey(nearestState))
                        tweetsByState[nearestState] = new List<Tweet>();
                    tweetsByState[nearestState].Add(tweet);
                }
            }

            return tweetsByState;
        }

        /// <summary>
        /// Вычисляет среднюю тональность твитов для каждого штата.
        /// Исключает твиты, для которых тональность не определена.
        /// Если для штата нет твитов с известной тональностью, результатом будет null.
        /// </summary>
        /// <param name="tweetsByState">Словарь твитов по штатам</param>
        /// <returns>Словарь: ключ – код штата, значение – средняя тональность (null, если данных нет)</returns>
        public static Dictionary<string, double?> CalculateStateSentiment(Dictionary<string, List<Tweet>> tweetsByState)
        {
            var stateSentiments = new Dictionary<string, double?>();

            foreach (var kvp in tweetsByState)
            {
                var sentiments = kvp.Value
                    .Where(t => t.Sentiment.HasValue)
                    .Select(t => t.Sentiment.Value)
                    .ToList();

                if (sentiments.Count > 0)
                {
                    stateSentiments[kvp.Key] = sentiments.Average();
                }
                else
                {
                    stateSentiments[kvp.Key] = null;
                }
            }

            return stateSentiments;
        }

        /// <summary>
        /// Условная "визуализация": выводим в консоль средние тональности штатов и цвет, соответствующий тональности.
        /// - null -> Серый
        /// - > 0 -> Жёлтый
        /// - < 0 -> Синий
        /// - = 0 -> Белый
        /// </summary>
        /// <param name="stateSentiments">Словарь средней тональности по штатам</param>
        public static void RenderUSMap(Dictionary<string, double?> stateSentiments)
        {
            Console.WriteLine("\nВизуализация карты США (условно):");
            foreach (var kvp in stateSentiments)
            {
                string color;
                double? sentiment = kvp.Value;

                if (!sentiment.HasValue)
                {
                    color = "Серый"; // Нет данных
                }
                else if (sentiment > 0)
                {
                    color = "Жёлтый"; // Положительная тональность
                }
                else if (sentiment < 0)
                {
                    color = "Синий"; // Отрицательная тональность
                }
                else
                {
                    color = "Белый"; // Нейтральная тональность (0)
                }

                Console.WriteLine($"Штат: {kvp.Key}, Средняя тональность: {(sentiment.HasValue ? sentiment.Value.ToString("F2") : "нет данных")}, Цвет: {color}");
            }
        }
    }
}