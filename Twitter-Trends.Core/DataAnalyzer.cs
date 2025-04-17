using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;


namespace Twitter_Trends
{
    public class DataAnalyzer
    {
        private static readonly Regex TokenRegex = new Regex(@"[\p{L}'-]+", RegexOptions.Compiled);

        public static List<string> TokenizeTweet(string tweet)
        {
            // Токенизация с учётом апострофов и дефисов
            var matches = TokenRegex.Matches(tweet);
            return matches.Select(m => m.Value.ToLower()).ToList();
        }

        public static double? AnalyzeSentiment(string tweet, Dictionary<string, double> sentimentDictionary)
        {
            var words = TokenizeTweet(tweet);
            if (words.Count == 0)
                return null;

            // Определяем максимальную длину фразы в словаре
            int maxPhraseLength = GetMaxPhraseLength(sentimentDictionary);

            double sum = 0;
            int count = 0;
            int i = 0;

            while (i < words.Count)
            {
                bool found = false;
                int currentMaxLength = Math.Min(maxPhraseLength, words.Count - i);

                // Проверяем фразы от самой длинной до самой короткой
                for (int length = currentMaxLength; length >= 1; length--)
                {
                    string phrase = string.Join(" ", words.Skip(i).Take(length));
                    if (sentimentDictionary.TryGetValue(phrase, out double weight))
                    {
                        sum += weight;
                        count++;
                        i += length; // Перемещаемся на конец найденной фразы
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Если фраза не найдена, проверяем только текущее слово
                    if (sentimentDictionary.TryGetValue(words[i], out double weight))
                    {
                        sum += weight;
                        count++;
                    }
                    i++;
                }
            }

            return count > 0 ? sum / count : (double?)null;
        }

        public static List<Tweet> AnalyzeTweetsSentiment(List<Tweet> tweets, Dictionary<string, double> sentimentDictionary)
        {
            Parallel.ForEach(tweets, tweet =>
            {
                tweet.Sentiment = AnalyzeSentiment(tweet.Text, sentimentDictionary);
            });

            return tweets;
        }


        private static int GetMaxPhraseLength(Dictionary<string, double> sentimentDictionary)
        {
            int maxLength = 1;
            foreach (var phrase in sentimentDictionary.Keys)
            {
                int length = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (length > maxLength)
                    maxLength = length;
            }
            return maxLength;
        }

        public static State FindTweetState(Tweet tweet, List<State> states)
        {
            var tweetLocation = ReverseCoordinates(tweet.Location);

            foreach (var state in states)
            {
                if (state.Shape.Contains(tweetLocation))
                {
                    return state;
                }
            }

            return null;
        }

        public static Point ReverseCoordinates(Point original)
        {
            return new Point(original.Y, original.X); // Меняем местами X и Y
        }

        public static Dictionary<string, List<Tweet>> GroupTweetsByState(List<Tweet> tweets, List<State> states)
        {
            var lockObj = new object();
            var tweetsByState = new Dictionary<string, List<Tweet>>();

            tweets.AsParallel().ForAll(tweet =>
            {
                var state = FindTweetState(tweet, states);

                if (state != null)
                {
                    lock (lockObj)
                    {
                        if (!tweetsByState.ContainsKey(state.PostalCode))
                        {
                            tweetsByState[state.PostalCode] = new List<Tweet>();
                        }

                        tweetsByState[state.PostalCode].Add(tweet);
                    }
                }
            });

            return tweetsByState;
        }

        public static Dictionary<string, double?> CalculateStateSentiment(Dictionary<string, List<Tweet>> tweetsByState)
        {
            var stateSentiments = new Dictionary<string, double?>();

            foreach (var state in tweetsByState)
            {
                var sentiments = state.Value
                    .Where(t => t.Sentiment.HasValue)
                    .Select(t => t.Sentiment.Value)
                    .ToList();

                if (sentiments.Count > 0)
                {
                    stateSentiments[state.Key] = sentiments.Average();
                }
                else
                {
                    stateSentiments[state.Key] = null;
                }
            }

            return stateSentiments;
        }
    }
}
