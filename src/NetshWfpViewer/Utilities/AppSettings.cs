using System.Configuration;
using System.Linq;

namespace NetshWfpViewer.Utilities
{
    internal static class AppSettings
    {
        public static void WriteFileSettings(string key, object value)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings.AllKeys.Any(k => k == key))
            {
                configuration.AppSettings.Settings[key].Value = value?.ToString();
            }
            else
            {
                configuration.AppSettings.Settings.Add(key, value?.ToString());
            }
            configuration.Save(ConfigurationSaveMode.Minimal, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static string ReadFileSettings(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }

        public static bool ReadBoolean(string key)
        {
            var value = ReadFileSettings(key);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return bool.Parse(value);
        }
    }
}
