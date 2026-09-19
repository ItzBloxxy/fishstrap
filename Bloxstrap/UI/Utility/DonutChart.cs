using System.Windows;
using System.Windows.Media;

namespace Bloxstrap.UI.Utility
{
    public class DonutSlice
    {
        public Geometry Geometry { get; set; } = Geometry.Empty;

        public Brush Brush { get; set; } = Brushes.Transparent;

        public string Label { get; set; } = String.Empty;

        public string ValueText { get; set; } = String.Empty;

        public string PercentText { get; set; } = String.Empty;
    }

    public static class DonutChart
    {
        public const double Size = 220;

        public const double Thickness = 24;

        private const double GapDegrees = 2.5;

        private const double MinimumSweep = 1.5;

        private const int MaxSlices = 7;

        private static readonly Color[] Palette =
        {
            Color.FromRgb(0x4C, 0x8B, 0xF5),
            Color.FromRgb(0xE8, 0x45, 0x3C),
            Color.FromRgb(0xF9, 0xAB, 0x00),
            Color.FromRgb(0x34, 0xA8, 0x53),
            Color.FromRgb(0xFF, 0x70, 0x43),
            Color.FromRgb(0xE9, 0x1E, 0x8C),
            Color.FromRgb(0x9C, 0x27, 0xB0),
            Color.FromRgb(0x00, 0xAC, 0xC1)
        };

        private static readonly Color OtherColor = Color.FromRgb(0x9A, 0xA0, 0xA6);

        public static List<DonutSlice> Build(
            IReadOnlyList<(string Label, double Value, string ValueText)> entries,
            string otherLabel,
            Func<double, string> format)
        {
            var slices = new List<DonutSlice>();

            var ordered = entries.Where(x => x.Value > 0).OrderByDescending(x => x.Value).ToList();

            bool collapsed = ordered.Count > MaxSlices;

            if (collapsed)
            {
                double rest = ordered.Skip(MaxSlices - 1).Sum(x => x.Value);

                ordered = ordered.Take(MaxSlices - 1).ToList();
                ordered.Add((otherLabel, rest, format(rest)));
            }

            if (!ordered.Any())
                return slices;

            double total = ordered.Sum(x => x.Value);
            double radius = (Size - Thickness) / 2;
            double centre = Size / 2;
            double angle = 0;

            if (ordered.Count == 1)
            {
                var only = ordered[0];

                slices.Add(new DonutSlice
                {
                    Geometry = Ring(centre, radius),
                    Brush = Fill(Palette[0]),
                    Label = only.Label,
                    ValueText = only.ValueText,
                    PercentText = "100%"
                });

                return slices;
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                var entry = ordered[i];

                double sweep = entry.Value / total * 360;
                double drawn = Math.Max(sweep - GapDegrees, MinimumSweep);

                slices.Add(new DonutSlice
                {
                    Geometry = Arc(centre, radius, angle + GapDegrees / 2, drawn),
                    Brush = Fill(collapsed && i == ordered.Count - 1 ? OtherColor : Palette[i % Palette.Length]),
                    Label = entry.Label,
                    ValueText = entry.ValueText,
                    PercentText = $"{Math.Round(entry.Value / total * 100)}%"
                });

                angle += sweep;
            }

            return slices;
        }

        private static SolidColorBrush Fill(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            return brush;
        }

        private static Geometry Ring(double centre, double radius)
        {
            var geometry = new EllipseGeometry(new Point(centre, centre), radius, radius);
            geometry.Freeze();

            return geometry;
        }

        private static Geometry Arc(double centre, double radius, double startAngle, double sweep)
        {
            Point start = OnCircle(centre, radius, startAngle);
            Point end = OnCircle(centre, radius, startAngle + sweep);

            var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };

            figure.Segments.Add(new ArcSegment(
                end,
                new Size(radius, radius),
                0,
                sweep > 180,
                SweepDirection.Clockwise,
                true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();

            return geometry;
        }

        private static Point OnCircle(double centre, double radius, double degrees)
        {
            double radians = (degrees - 90) * Math.PI / 180;

            return new Point(centre + radius * Math.Cos(radians), centre + radius * Math.Sin(radians));
        }
    }
}
