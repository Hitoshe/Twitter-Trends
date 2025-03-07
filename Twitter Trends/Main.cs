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
            // Фаза 1: Анализ чувств в твитах

            // Задача 2: Загрузка и парсинг файла sentiments.csv
            var sentimentDictionary = LoadSentiments("sentiments.csv");
            Console.WriteLine("Словарь тональностей загружен.");

            // Фаза 2: Определение настроений по штатам

            // Задача 3: Загрузка и парсинг файла states.json
            var states = LoadStates("states.json");
            Console.WriteLine("Данные штатов загружены.");

            // Вычисляем центр для каждого штата
            var stateCenters = CalculateStateCenters(states);

            // Создадим список твитов для демонстрации
            var tweets = new List<Tweet>
            {
                new Tweet { Text = "I love sunny days", Latitude = 34.0522, Longitude = -118.2437 },
                new Tweet { Text = "It is a gloomy and sad day", Latitude = 40.7128, Longitude = -74.0060 },
                new Tweet { Text = "Neutral feelings, nothing special", Latitude = 41.8781, Longitude = -87.6298 }
            };

            // Задача 1 и 2: Анализ тональности каждого твита
            foreach (var tweet in tweets)
            {
                tweet.Sentiment = AnalyzeSentiment(tweet.Text, sentimentDictionary);
                Console.WriteLine($"Твит: \"{tweet.Text}\" | Тональность: {(tweet.Sentiment.HasValue ? tweet.Sentiment.Value.ToString("F2") : "нет данных")}");
            }

            // Задача 4: Группировка твитов по ближайшим штатам
            var tweetsByState = GroupTweetsByState(tweets, stateCenters);

            // Задача 5: Вычисление средней тональности твитов по штатам
            var stateSentiments = CalculateStateSentiment(tweetsByState);

            // Фаза 3: Визуализация данных на карте США
            RenderUSMap(stateSentiments);

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
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

            // Если файла не существует, выводим сообщение и возвращаем пустой словарь
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден.");
                return sentiments;
            }

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                // Пропускаем пустые строки
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Разделяем строку по запятой
                var parts = line.Split(',');
                if (parts.Length != 2)
                    continue;

                string word = parts[0].Trim();
                if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                {
                    sentiments[word] = weight;
                }
            }

            return sentiments;
        }

        /// <summary>
        /// Загрузка географических данных штатов из JSON-файла.
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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var states = JsonSerializer.Deserialize<List<State>>(jsonContent, options);
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
        /// <returns>Словарь, где ключ – код штата, значение – кортеж (средняя широта, средняя долгота)</returns>
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
            // Регулярное выражение \p{L}+ находит последовательности символов, являющихся буквами (включая символы из разных алфавитов)
            var matches = Regex.Matches(tweet, @"\p{L}+");
            return matches.Select(m => m.Value.ToLower()).ToList();
        }

        /// <summary>
        /// Анализирует тональность твита.
        /// Находит все слова твита, ищет их в словаре тональностей и вычисляет среднее значение.
        /// Если ни одно слово не найдено в словаре, возвращает null.
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
        /// Находит ближайший штат к заданным координатам твита.
        /// Используется евклидова дистанция между центром штата и координатами твита.
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
        /// Для каждого твита определяется ближайший штат, и он добавляется в словарь с ключом – код штата.
        /// </summary>
        /// <param name="tweets">Список твитов</param>
        /// <param name="stateCenters">Словарь центров штатов</param>
        /// <returns>Словарь, где ключ – код штата, значение – список твитов</returns>
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
        /// <param name="tweetsByState">Словарь твитов, сгруппированных по штатам</param>
        /// <returns>Словарь, где ключ – код штата, значение – средняя тональность (null, если данных нет)</returns>
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
        /// Отображает карту США с цветовой индикацией тональности каждого штата.
        /// Логика цветовой индикации:
        /// - Если тональность не определена (null) – Серый.
        /// - Если тональность > 0 – Жёлтый (положительная).
        /// - Если тональность < 0 – Синий (отрицательная).
        /// - Если тональность равна 0 – Белый (нейтральная).
        /// В данном примере карта выводится в консоль.
        /// </summary>
        /// <param name="stateSentiments">Словарь средней тональности по штатам</param>
        public static void RenderUSMap(Dictionary<string, double?> stateSentiments)
        {
            Console.WriteLine("\nВизуализация карты США с тональностями штатов:");
            foreach (var kvp in stateSentiments)
            {
                string color;
                if (!kvp.Value.HasValue)
                {
                    color = "Серый"; // Нет данных
                }
                else if (kvp.Value > 0)
                {
                    color = "Жёлтый"; // Положительная тональность
                }
                else if (kvp.Value < 0)
                {
                    color = "Синий"; // Отрицательная тональность
                }
                else
                {
                    color = "Белый"; // Нейтральная тональность
                }

                Console.WriteLine($"Штат: {kvp.Key} | Средняя тональность: {(kvp.Value.HasValue ? kvp.Value.Value.ToString("F2") : "нет данных")} | Цвет: {color}");
            }
        }
    }
}
