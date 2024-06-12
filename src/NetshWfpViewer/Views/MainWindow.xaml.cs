using System;
using System.Windows;
using ICSharpCode.AvalonEdit;
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

            DataContext = new MainWindowViewModel(
                wfpXml => SetEditorText(WfpFilters, wfpXml),
                formattedWfpXml => SetEditorText(FormattedWfpFilters, formattedWfpXml));
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
