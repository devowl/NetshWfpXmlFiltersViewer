using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using NetshWfpViewer.Utilities;
using NetshWfpViewer.ViewModels;

namespace NetshWfpViewer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly TextMarkerService _formattedMarkers;
        private readonly Color _searchMatchColor;

        public MainWindow()
        {
            InitializeComponent();

            _searchMatchColor = ((SolidColorBrush)FindResource("SearchMatchBrush")).Color;
            _formattedMarkers = new TextMarkerService(FormattedWfpFilters);

            FormattedWfpFilters.TextArea.TextView.BackgroundRenderers.Add(_formattedMarkers);

            SearchPanel.Install(WfpFilters.TextArea);
            SearchPanel.Install(FormattedWfpFilters.TextArea);

            ThemeManager.ThemeChanged += OnThemeChanged;
            ApplyThemeToEditors();
            UpdateThemeMenuChecks();

            UserFilterBox.TextChanged += (_, __) => UpdateFormattedHighlight();
            UserFilterAnyWordBox.Checked += (_, __) => UpdateFormattedHighlight();
            UserFilterAnyWordBox.Unchecked += (_, __) => UpdateFormattedHighlight();

            DataContext = new MainWindowViewModel(
                wfpXml => SetEditorText(WfpFilters, wfpXml),
                formattedWfpXml =>
                {
                    SetEditorText(FormattedWfpFilters, formattedWfpXml);
                    UpdateFormattedHighlight();
                });
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeToEditors();
            UpdateFormattedHighlight();
        }

        private void ApplyThemeToEditors()
        {
            ThemeManager.ApplyToEditor(WfpFilters);
            ThemeManager.ApplyToEditor(FormattedWfpFilters);
        }

        private void UpdateThemeMenuChecks()
        {
            LightThemeMenuItem.IsChecked = ThemeManager.CurrentTheme == AppTheme.Light;
            DarkThemeMenuItem.IsChecked = ThemeManager.CurrentTheme == AppTheme.Dark;
        }

        private void LightThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(AppTheme.Light);
        }

        private void DarkThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(AppTheme.Dark);
        }

        private void SetTheme(AppTheme theme)
        {
            ThemeManager.ApplyTheme(theme);
            ThemeManager.SaveTheme(theme);
            UpdateThemeMenuChecks();
        }

        private static void SetEditorText(TextEditor editor, string xml)
        {
            try
            {
                var caretOffset = editor.CaretOffset;
                editor.Text = xml;

                editor.CaretOffset =
                    caretOffset <= editor.Document.TextLength ? caretOffset : editor.Document.TextLength;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Highlights every occurrence of the "should have" filter text
        /// in the formatted (right) editor. Matching semantics replicate
        /// WfpXmlFilter: case-insensitive; with "Any word" the filter is
        /// split by spaces and commas, otherwise the whole phrase is used.
        /// </summary>
        private void UpdateFormattedHighlight()
        {
            _formattedMarkers.RemoveAll();

            var xmlText = FormattedWfpFilters.Text;

            if (!string.IsNullOrEmpty(xmlText))
            {
                foreach (var searchWord in GetSearchWords())
                {
                    int index = 0;
                    while ((index = xmlText.IndexOf(searchWord, index, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        _formattedMarkers.Create(index, searchWord.Length, _searchMatchColor);
                        index += searchWord.Length;
                    }
                }
            }

            FormattedWfpFilters.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        private IEnumerable<string> GetSearchWords()
        {
            var filter = UserFilterBox.Text;

            if (string.IsNullOrWhiteSpace(filter))
            {
                yield break;
            }

            if (UserFilterAnyWordBox.IsChecked == true)
            {
                var words = filter
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => word.Trim());

                foreach (var word in words.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    yield return word;
                }
            }
            else
            {
                yield return filter;
            }
        }
    }
}
