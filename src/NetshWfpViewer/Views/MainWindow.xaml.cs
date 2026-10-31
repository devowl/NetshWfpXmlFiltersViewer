using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
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

        public MainWindow()
        {
            InitializeComponent();

            _formattedMarkers = new TextMarkerService(FormattedWfpFilters);
            FormattedWfpFilters.TextArea.TextView.BackgroundRenderers.Add(_formattedMarkers);

            ThemeManager.ThemeChanged += OnThemeChanged;
            ApplyThemeToEditors();
            UpdateThemeMenuChecks();

            UserFilterBox.TextChanged += (_, __) => UpdateFormattedHighlight();
            UserFilterAnyWordBox.Checked += (_, __) => UpdateFormattedHighlight();
            UserFilterAnyWordBox.Unchecked += (_, __) => UpdateFormattedHighlight();

            DataContext = new MainWindowViewModel(
                wfpXml => SetEditorText(WfpFilters, wfpXml),
                formattedWfpXml => SetEditorText(FormattedWfpFilters, formattedWfpXml));
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

        private void SetEditorText(TextEditor editor, string xml)
        {
            try
            {
                var caretOffset = editor.CaretOffset;
                editor.Text = xml;

                editor.CaretOffset =
                    caretOffset <= editor.Document.TextLength ? caretOffset : editor.Document.TextLength;

                if (ReferenceEquals(editor, FormattedWfpFilters))
                {
                    UpdateFormattedHighlight();
                }
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

            if (TryFindResource("SearchMatchBrush") is not SolidColorBrush matchBrush)
            {
                return;
            }

            var color = matchBrush.Color;
            var text = FormattedWfpFilters.Text;

            if (!string.IsNullOrEmpty(text))
            {
                foreach (var word in GetSearchWords())
                {
                    int index = 0;
                    while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        _formattedMarkers.Create(index, word.Length, color);
                        index += word.Length;
                    }
                }
            }

            FormattedWfpFilters.TextArea.TextView.InvalidateVisual();
        }

        private IEnumerable<string> GetSearchWords()
        {
            if (DataContext is not MainWindowViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.UserFilter))
            {
                yield break;
            }

            if (viewModel.UserFilterAnyWord)
            {
                var words = viewModel.UserFilter
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => word.Trim());

                foreach (var word in words.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    yield return word;
                }
            }
            else
            {
                yield return viewModel.UserFilter;
            }
        }
    }
}
