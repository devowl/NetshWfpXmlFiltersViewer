using System;
using System.Reflection;
using NetshWfpViewer.ViewModels;

namespace NetshWfpViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
            Dispatcher.UnhandledException += (_, args) =>
            {
                ProcessException(args.Exception);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                ProcessException(args.ExceptionObject as Exception);
            };
        }

        private void ProcessException(Exception exception)
        {
            MainWindowViewModel.ShowMessage("Unhandled exception", exception);
        }

        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("ICSharp"))
            {
                return Assembly.Load(NetshWfpViewer.Properties.Resources.ICSharpCode_AvalonEdit);
            }

            return null;
        }
    }
}
