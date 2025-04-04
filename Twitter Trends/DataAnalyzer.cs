﻿using System;
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
        public static List<string> TokenizeTweet(string tweet)
        {
            // Токенизация с учётом апострофов и дефисов
            var matches = Regex.Matches(tweet, @"[\p{L}'-]+");
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
            foreach (var state in states)
            {
                if (state.Shape.Covers(tweet.Location))
                {
                    return state;
                }
            }

            return null;
        }

        public static Dictionary<string, List<Tweet>> GroupTweetsByState(List<Tweet> tweets, List<State> states)
        {
            var tweetsByState = new Dictionary<string, List<Tweet>>();

            foreach (var tweet in tweets)
            {
                State nearestState = FindTweetState(tweet, states);

                if (nearestState != null)
                {
                    if (!tweetsByState.ContainsKey(nearestState.PostalCode))
                        tweetsByState[nearestState.PostalCode] = new List<Tweet>();

                    tweetsByState[nearestState.PostalCode].Add(tweet);
                }
            }

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
