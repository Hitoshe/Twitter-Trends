using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Utilities;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitter_Trends;
using Mapsui.Fetcher;
using Mapsui.Styles.Thematics;


public class MapDrawer
{
    private static readonly List<(double Value, Color Color)> ColorStops = new List<(double, Color)>
    {
        (-5, Color.Purple),
        (-0.3, Color.Blue),
        (-0.2, Color.Cyan),
        (-0.1, Color.LightGreen),
        (0.0, Color.Green),
        (0.1, Color.Yellow),
        (0.2, Color.Orange),
        (0.3, Color.Red),
        (5, Color.Pink)
    };


    public static Map CreateMapWithStateLayers(List<State> states)
    {
        var map = new Map();

        foreach (var state in states)
        {
            var features = new List<GeometryFeature>();

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

            var stateLayer = new Layer(state.PostalCode) // Уникальное имя слоя — по аббревиатуре
            {
                DataSource = new MemoryProvider(features)
            };

            map.Layers.Add(stateLayer);
        }

        // Центровка карты — аналогично
        var allEnvelopes = states.SelectMany(s => s.Shape.Geometries)
            .OfType<Polygon>()
            .Select(p => p.EnvelopeInternal);

        var envelope = allEnvelopes.Aggregate((e1, e2) => e1.ExpandedBy(e2));
        var bbox = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
        double resolution = Math.Max(bbox.Width, bbox.Height);

        map.Home = n => n.ZoomTo(resolution);

        return map;
    }


    // Функция для получения цвета на основе сентимента
    private static Color InterpolateColor(Color c1, Color c2, double t)
    {
        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);
        byte a = (byte)(c1.A + (c2.A - c1.A) * t);
        return Color.FromArgb(a, r, g, b);
    }

    private static Color GetColorForSentiment(double? sentiment)
    {
        if (!sentiment.HasValue)
            return Color.LightGray;

        double value = sentiment.Value;

        // Ограничим значение
        if (value <= ColorStops.First().Value)
            return ColorStops.First().Color;
        if (value >= ColorStops.Last().Value)
            return ColorStops.Last().Color;

        // Найдем ближайшие точки градиента
        for (int i = 0; i < ColorStops.Count - 1; i++)
        {
            var (v1, c1) = ColorStops[i];
            var (v2, c2) = ColorStops[i + 1];

            if (value >= v1 && value <= v2)
            {
                double t = (value - v1) / (v2 - v1); // нормализуем
                return InterpolateColor(c1, c2, t);
            }
        }

        // fallback (не должен вызываться)
        return Color.Gray;
    }

    public static async Task ColorStatesBySentimentAsync(Dictionary<string, double?> stateSentiments, Map map)
    {
        foreach (var layer in map.Layers)
        {
            if (layer is Layer stateLayer && stateLayer.DataSource is MemoryProvider memoryProvider)
            {
                var extent = stateLayer.Extent ?? new MRect(-180, -90, 180, 90);
                var resolution = 1.0;

                var section = new MSection(extent, resolution);
                var fetchInfo = new FetchInfo(section);

                var features = await memoryProvider.GetFeaturesAsync(fetchInfo);

                foreach (var feature in features)
                {
                    if (feature["PostalCode"] is string postalCode)
                    {
                        var sentiment = stateSentiments.TryGetValue(postalCode, out var s) ? s : null;
                        var color = GetColorForSentiment(sentiment);

                        feature.Styles.Clear();
                        feature.Styles.Add(new VectorStyle
                        {
                            Fill = new Brush(color),
                            Outline = new Pen(Color.Black, 1)
                        });
                    }
                }
            }
        }
    }



}
