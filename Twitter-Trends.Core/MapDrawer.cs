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
using Twitter_Trends;

public class MapDrawer
{
    public static Map CreateMapWithStates(List<State> states)
    {
        var map = new Map();

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
        // Вычисляем объединённый envelope всех геометрий
        var envelope = features
            .Where(f => f.Geometry != null)
            .Select(f => f.Geometry.EnvelopeInternal)
            .Aggregate((e1, e2) => e1.ExpandedBy(e2));

        // Создаем прямоугольник (bbox) в тех координатах, в которых заданы геометрии
        var bbox = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);

        // Вычисляем центр прямоугольника
        double centerX = (bbox.Left + bbox.Right) / 2.0;
        double centerY = (bbox.Bottom + bbox.Top) / 2.0;

        // Вычисляем "разрешение": можно взять, например, максимальную длину стороны
        double resolution = Math.Max(bbox.Width, bbox.Height);

        // Устанавливаем домашнее положение карты
        map.Home = n => n.ZoomTo(resolution);


        return map;
    }
}
