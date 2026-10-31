using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace NetshWfpViewer.Utilities
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    internal static class ThemeManager
    {
        private const string ThemeSettingsKey = "Theme";
        private const string SyntaxColorKeyPrefix = "SyntaxColor";

        // AvalonEdit XML-Mode color names recolored by the active theme dictionary.
        private static readonly string[] SyntaxColorNames =
        {
            "Comment", "CData", "DocType", "XmlDeclaration", "XmlTag",
            "AttributeName", "AttributeValue", "Entity", "BrokenEntity"
        };

        private static ResourceDictionary _themeDictionary;
        private static IHighlightingDefinition _darkXmlHighlighting;

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public static event EventHandler ThemeChanged;

        public static AppTheme LoadSavedTheme()
        {
            var value = AppSettings.ReadFileSettings(ThemeSettingsKey);
            return Enum.TryParse(value, true, out AppTheme theme) ? theme : AppTheme.Light;
        }

        public static void SaveTheme(AppTheme theme)
        {
            AppSettings.WriteFileSettings(ThemeSettingsKey, theme.ToString());
        }

        /// <summary>
        /// Swaps the active theme resource dictionary
        /// (Themes/LightTheme.xaml or Themes/DarkTheme.xaml).
        /// </summary>
        public static void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}Theme.xaml", UriKind.Relative)
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            var existing = _themeDictionary ?? merged.FirstOrDefault(d =>
                d.Source != null && d.Source.OriginalString.Contains("Themes/"));

            if (existing != null && merged.Contains(existing))
            {
                merged[merged.IndexOf(existing)] = newDictionary;
            }
            else
            {
                merged.Add(newDictionary);
            }

            _themeDictionary = newDictionary;
            _darkXmlHighlighting = null; // rebuild with the new theme's syntax colors

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Applies theme-dependent editor settings that cannot be expressed
        /// as DynamicResource: the syntax highlighting definition.
        /// Editor brushes are bound in XAML via DynamicResource.
        /// </summary>
        public static void ApplyToEditor(TextEditor editor)
        {
            if (CurrentTheme == AppTheme.Dark)
            {
                editor.SyntaxHighlighting = GetDarkXmlHighlighting()
                                            ?? HighlightingManager.Instance.GetDefinition("XML");
            }
            else
            {
                // Restore the value coming from the TextEditor style.
                editor.ClearValue(TextEditor.SyntaxHighlightingProperty);
            }
        }

        /// <summary>
        /// Builds a dark variant of the built-in AvalonEdit XML highlighting
        /// by recoloring the stock XML-Mode.xshd embedded resource
        /// with the SyntaxColor* values from the active theme dictionary.
        /// </summary>
        private static IHighlightingDefinition GetDarkXmlHighlighting()
        {
            if (_darkXmlHighlighting != null)
            {
                return _darkXmlHighlighting;
            }

            try
            {
                var assembly = typeof(HighlightingManager).Assembly;
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Contains("XML-Mode") && n.EndsWith(".xshd"));
                if (resourceName == null)
                {
                    return null;
                }

                string xshd;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    xshd = reader.ReadToEnd();
                }

                foreach (var colorName in SyntaxColorNames)
                {
                    if (!(Application.Current.TryFindResource(SyntaxColorKeyPrefix + colorName) is string colorValue))
                    {
                        continue;
                    }

                    xshd = Regex.Replace(
                        xshd,
                        $"(<Color\\s+name=\"{colorName}\"[^>]*?\\bforeground=\")[^\"]*(\")",
                        $"$1{colorValue}$2",
                        RegexOptions.IgnoreCase);
                }

                XshdSyntaxDefinition definition;
                using (var xmlReader = XmlReader.Create(new StringReader(xshd)))
                {
                    definition = HighlightingLoader.LoadXshd(xmlReader);
                }

                _darkXmlHighlighting = HighlightingLoader.Load(definition, HighlightingManager.Instance);
                return _darkXmlHighlighting;
            }
            catch
            {
                return null;
            }
        }
    }
}
