using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;


namespace Twitter_Trends
{
    public class DataAnalizer
    {
        public static List<string> TokenizeTweet(string tweet)
        {
            var matches = Regex.Matches(tweet, @"\p{L}+");
            return matches.Select(m => m.Value.ToLower()).ToList();
        }

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

        public static State FindNearestState(Tweet tweet, List<State> states)
        {
            foreach (var state in states)
            {
                if (state.Shape.Contains(tweet.Location))
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
                State nearestState = FindNearestState(tweet, states);
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
