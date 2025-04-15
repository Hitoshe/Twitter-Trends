using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;


namespace Twitter_Trends
{
    public class Tweet
    {
        public string Text { get; set; }
        public Point Location { get; set; }
        public DateTime TweetDate { get; set; }

        public double? Sentiment { get; set; }
    }
}
