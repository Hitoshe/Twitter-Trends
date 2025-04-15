using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;


namespace Twitter_Trends
{
    public class State
    {
        public State (string PostalCode, MultiPolygon Shape)
        {
            this.PostalCode = PostalCode;
            this.Shape = Shape;
        }

        public string PostalCode { get; set; }

        public MultiPolygon Shape { get; set; }
    }
}
