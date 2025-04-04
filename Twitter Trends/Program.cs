using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitter_Trends
{
    class Program
    {
        public List<State> states;
        public Dictionary<string, double> sentiments;
        public List<Tweet> tweets;

        private static string? tweetsPath;          //  Directory
        private static string? sentimentsPath;
        private static string? statesPath;

        static void Main(string[] args)
        {
            tweetsPath = "D:\\twitter\\Tweets";
            sentimentsPath = "D:\\twitter\\sentiments.csv";
            statesPath = "D:\\twitter\\states.json";

            List<Tweet> tweets = DataParser.LoadTweets(tweetsPath);
            Dictionary<string, double> sentiments = DataParser.LoadSentiments(sentimentsPath);
            //List<State> statess = DataParser.LoadStates(statesPath);


            string tweet = tweets[89].Text;
            double? sentiment = DataAnalyzer.AnalyzeSentiment(tweet, sentiments);

            Console.WriteLine(sentiment);
        }
    }
}
