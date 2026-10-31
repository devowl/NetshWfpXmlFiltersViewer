using System;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace NetshWfpViewer.Utilities
{
    /// <summary>
    /// Draws background markers over text ranges (search match highlighting).
    /// </summary>
    internal sealed class TextMarkerService : IBackgroundRenderer
    {
        private sealed class TextMarker : TextSegment
        {
            public Color BackgroundColor { get; set; }
        }

        private readonly TextSegmentCollection<TextMarker> _markers;

        public TextMarkerService(TextEditor editor)
        {
            _markers = new TextSegmentCollection<TextMarker>(editor.Document);
        }

        // Drawn behind the text, above the editor background.
        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (drawingContext == null)
            {
                throw new ArgumentNullException(nameof(drawingContext));
            }

            if (!textView.VisualLinesValid)
            {
                return;
            }

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
            {
                return;
            }

            int viewStart = visualLines.First().FirstDocumentLine.Offset;
            int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var marker in _markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                var geoBuilder = new BackgroundGeometryBuilder
                {
                    AlignToWholePixels = true,
                    CornerRadius = 2
                };
                geoBuilder.AddSegment(textView, marker);

                var geometry = geoBuilder.CreateGeometry();
                if (geometry == null)
                {
                    continue;
                }

                var brush = new SolidColorBrush(marker.BackgroundColor);
                brush.Freeze();
                drawingContext.DrawGeometry(brush, null, geometry);
            }
        }

        public void Create(int startOffset, int length, Color color)
        {
            if (length <= 0)
            {
                return;
            }

            _markers.Add(new TextMarker
            {
                StartOffset = startOffset,
                Length = length,
                BackgroundColor = color
            });
        }

        public void RemoveAll()
        {
            _markers.Clear();
        }
    }
}
