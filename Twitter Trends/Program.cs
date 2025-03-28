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
            tweetsPath = "D:\\TwitterTends\\Data\\Tweets";
            sentimentsPath = "D:\\TwitterTends\\Data\\sentiments.csv";
            statesPath = "D:\\TwitterTends\\Data\\states.json";

            List<Tweet> tweets = DataParser.LoadTweets(tweetsPath);
            Dictionary<string, double> sentiments = DataParser.LoadSentiments(sentimentsPath);
            List<State> statess = DataParser.LoadStates(statesPath);
        }
    }
}
