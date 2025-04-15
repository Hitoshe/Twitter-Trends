using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Utilities;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using Twitter_Trends;

public class MapDrawer
{
    public static Map CreateMapWithStates(List<State> states)
    {
        var map = new Map();

        // Добавим тайловый слой
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Создаём фичи
        var features = new List<GeometryFeature>();

        foreach (var state in states)
        {
            foreach (var geom in state.Shape.Geometries)
            {
                if (geom is Polygon polygon)
                {
                    var feature = new GeometryFeature
                    {
                        Geometry = polygon
                    };

                    feature.Styles.Add(new VectorStyle
                    {
                        Fill = new Brush(Color.FromArgb(100, 0, 120, 255)),
                        Outline = new Pen(Color.Black, 1)
                    });

                    feature["PostalCode"] = state.PostalCode;
                    features.Add(feature);
                }
            }
        }

        // Поставим слой на карту
        var stateLayer = new Layer("States")
        {
            DataSource = new MemoryProvider(features)
        };

        map.Layers.Add(stateLayer);

        // Центруем карту
        var envelope = features
            .Where(f => f.Geometry != null)
            .Select(f => f.Geometry.EnvelopeInternal)
            .Aggregate((e1, e2) => e1.ExpandedBy(e2));

        var bbox = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);

        //map.Home = () => map.Navigator.NavigateTo(bbox, ScaleMethod.Fit);

        return map;
    }
}
