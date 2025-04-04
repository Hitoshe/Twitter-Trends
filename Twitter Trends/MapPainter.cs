using NetTopologySuite.Geometries;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using Twitter_Trends;

namespace Twitter_Trends
{
    public class MapPainter
    {
        private readonly RenderWindow _window;
        private readonly List<State> _states;
        private readonly Dictionary<string, Color> _stateColors;
        private readonly Random _random = new Random();

        // ��������� ������
        private Vector2f _panOffset;
        private Vector2f _lastMousePos;
        private float _zoom = 1.0f;
        private const float ZoomStep = 0.1f;
        private Font _font;

        public MapPainter(RenderWindow window, List<State> states)
        {
            _window = window;
            _states = states;
            _stateColors = new Dictionary<string, Color>();

            InitializeStateColors();
            SetupEventHandlers();
            LoadFont();
        }

        private void LoadFont()
        {
            try
            {
                _font = new Font("D:\\TwitterTends\\Font\\Sansita-Regular.ttf"); // ������� ���� � ������ ������
            }
            catch
            {
                Console.WriteLine("�� ������� ��������� �����");
            }
        }

        private void InitializeStateColors()
        {
            foreach (var state in _states)
            {
                // ��������� ���������� ����� ��� ������� �����
                _stateColors[state.PostalCode] = new Color(
                    (byte)_random.Next(50, 200),
                    (byte)_random.Next(50, 200),
                    (byte)_random.Next(50, 200));
            }
        }

        private void SetupEventHandlers()
        {
            _window.MouseWheelScrolled += OnMouseWheelScrolled;
            _window.MouseButtonPressed += OnMouseButtonPressed;
            _window.MouseMoved += OnMouseMoved;
        }

        private void OnMouseWheelScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            if (e.Delta > 0)
                _zoom += ZoomStep; // �����������
            else
                _zoom = Math.Max(0.1f, _zoom - ZoomStep); // ��������� (�� ����� 0.1)
        }

        private void OnMouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == Mouse.Button.Left)
            {
                _lastMousePos = new Vector2f(e.X, e.Y);
            }
        }

        private void OnMouseMoved(object sender, MouseMoveEventArgs e)
        {
            if (Mouse.IsButtonPressed(Mouse.Button.Left))
            {
                var currentMousePos = new Vector2f(e.X, e.Y);
                _panOffset += currentMousePos - _lastMousePos;
                _lastMousePos = currentMousePos;
            }
        }

        public void Draw()
        {
            _window.Clear(Color.White);

            // ����� ������
            var center = new Vector2f(_window.Size.X / 2f, _window.Size.Y / 2f);

            // ������� �������������
            var transform = Transform.Identity;
            transform.Translate(center + _panOffset);
            transform.Scale(_zoom, -_zoom); // ����������� Y ��� �������������� ���������

            // ��������� ���� ������
            foreach (var state in _states)
            {
                DrawState(state, transform);
            }

            _window.Display();
        }

        private void DrawState(State state, Transform transform)
        {
            // ����������� ��������� � ������� ��� SFML
            var vertices = ConvertToVertices(state.Shape, transform);

            if (vertices.Count == 0) return;

            // ������� �������
            var polygon = new ConvexShape((uint)vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                polygon.SetPoint((uint)i, vertices[i]);
            }

            // ����������� ������� ���
            polygon.FillColor = _stateColors[state.PostalCode];
            polygon.OutlineColor = Color.Black;
            polygon.OutlineThickness = 0.5f;

            // ���������
            _window.Draw(polygon);

            // ��������� ������������ �����
            DrawStateLabel(state, transform);
        }

        private List<Vector2f> ConvertToVertices(MultiPolygon multiPolygon, Transform transform)
        {
            var vertices = new List<Vector2f>();

            foreach (var polygon in multiPolygon.Geometries)
            {
                if (polygon is Polygon poly)
                {
                    foreach (var coord in poly.ExteriorRing.Coordinates)
                    {
                        var point = transform.TransformPoint(new Vector2f(
                            (float)coord.X,
                            (float)coord.Y));
                        vertices.Add(point);
                    }
                }
            }

            return vertices;
        }

        private void DrawStateLabel(State state, Transform transform)
        {
            if (_font == null) return;

            var centroid = state.Shape.Centroid;
            var screenPos = transform.TransformPoint(new Vector2f(
                (float)centroid.X,
                (float)centroid.Y));

            var text = new Text(state.PostalCode, _font, 12)
            {
                FillColor = Color.Black,
                Position = screenPos,
                Style = Text.Styles.Bold
            };

            // ������������� ������
            var bounds = text.GetLocalBounds();
            text.Origin = new Vector2f(bounds.Width / 2, bounds.Height / 2);

            _window.Draw(text);
        }
    }
}