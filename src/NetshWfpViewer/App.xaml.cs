using System;
using System.Reflection;
using System.Windows;
using NetshWfpViewer.Utilities;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.ApplyTheme(ThemeManager.LoadSavedTheme());
        }

        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("ICSharp"))
            {
                return Assembly.Load(NetshWfpViewer.Properties.Resources.ICSharpCode_AvalonEdit);
            }

            if (args.Name.StartsWith("Microsoft.Xaml"))
            {
                return Assembly.Load(NetshWfpViewer.Properties.Resources.Microsoft_Xaml_Behaviors);
            }

            return null;
        }
    }
}
