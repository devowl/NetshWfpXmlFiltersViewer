using System;
using System.Windows;
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
        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.ThemeChanged += OnThemeChanged;
            ApplyThemeToEditors();
            UpdateThemeMenuChecks();

            DataContext = new MainWindowViewModel(
                wfpXml => SetEditorText(WfpFilters, wfpXml),
                formattedWfpXml => SetEditorText(FormattedWfpFilters, formattedWfpXml));
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeToEditors();
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
    }
}
