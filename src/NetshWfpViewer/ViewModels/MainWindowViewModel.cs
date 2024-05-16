using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using NetshWfpViewer.Utilities;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace NetshWfpViewer.ViewModels
{
    internal class MainWindowViewModel
    {
        private readonly Action<string> _showFilters;
        private readonly Action<string> _showFormattedFilters;
        private string _wfpXmlFilters;
        private bool _applyingFilters;
        private bool _readingWfpFilters;
        private string _wfpFormattedXmlFilters;
        private string _userFilter;
        private string _userInvertFilter;
        private readonly Timer _timer;

        public bool ReadingWfpFilters
        {
            get => _readingWfpFilters;
            set
            {
                _readingWfpFilters = value;
                RunDispatcher(() =>
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                });
            }
        }

        public MainWindowViewModel(Action<string> showFilters, Action<string> showFormattedFilters)
        {
            _showFilters = showFilters;
            _showFormattedFilters = showFormattedFilters;
            LoadSettings();
            UpdateFilters();

            SaveLeftCommand = new RelayCommand(SaveWfpFiltersHandler);
            SaveRightCommand = new RelayCommand(SaveWfpFormattedFiltersHandler);
            RefreshCommand = new RelayCommand(RefreshFiltersHandler, o => ReadingWfpFilters == false);
            RefreshStopCommand = new RelayCommand(o => _timer?.Stop());

            _timer = new Timer
            {
                Enabled = false,
            };

            _timer.Tick += (sender, args) => UpdateFilters();
        }

        public RelayCommand SaveLeftCommand { get; }

        public RelayCommand SaveRightCommand { get; }

        public RelayCommand RefreshCommand { get; }

        public RelayCommand RefreshStopCommand { get; }

        public string WfpXmlFilters
        {
            get => _wfpXmlFilters;
            set
            {
                if (string.Equals(_wfpXmlFilters, value))
                {
                    return;
                }

                _wfpXmlFilters = value;
                RunDispatcher(() =>
                {
                    _showFilters(value);
                });

                ApplyFormatting();
            }
        }

        public string WfpFormattedXmlFilters
        {
            get => _wfpFormattedXmlFilters;
            set
            {
                if (string.Equals(_wfpFormattedXmlFilters, value))
                {
                    return;
                }

                _wfpFormattedXmlFilters = value;
                RunDispatcher(() =>
                {
                    _showFormattedFilters(value);
                });
            }
        }

        public bool UserFilterAnyWord
        {
            get => _userFilterAnyWord;
            set
            {
                if (_userFilterAnyWord == value)
                {
                    return;
                }

                _userFilterAnyWord = value;
                ApplyFormatting();
            }
        }

        public bool UserInvertFilterAnyWord
        {
            get => _userInvertFilterAnyWord;
            set
            {
                if (_userInvertFilterAnyWord == value)
                {
                    return;
                }

                _userInvertFilterAnyWord = value;
                ApplyFormatting();
            }
        }

        public string UserInvertFilter
        {
            get => _userInvertFilter;
            set
            {
                _userInvertFilter = value;
                ApplyFormatting();
            }
        }

        public string UserFilter
        {
            get => _userFilter;
            set
            {
                _userFilter = value; 
                ApplyFormatting();
            }
        }

        private bool _requestUpdate;

        private bool _processingApplyFormatting;
        private bool _userFilterAnyWord;
        private bool _userInvertFilterAnyWord;

        private void ApplyFormatting()
        {
            void RunApplyFormatting()
            {
                Task.Run(() =>
                {
                    try
                    {
                        SaveSettings();

                        if (string.IsNullOrEmpty(UserFilter) && string.IsNullOrEmpty(UserInvertFilter))
                        {
                            WfpFormattedXmlFilters = WfpXmlFilters;
                            return;
                        }

                        try
                        {
                            XmlDocument document = new XmlDocument();
                            document.LoadXml(WfpXmlFilters);
                            /*
                             * <wfpdiag>
                             *  <filters>
                             *  	<item>
                             *      ...
                             *  </filters>
                             *  <providers>
                             *      <item>
                             *      ...
                             *  </providers>
                             * </wfpdiag>
                             */

                            void ApplyFilterToItem(string xpath, string userFilterText, bool anyWord, bool removeWhenFound)
                            {
                                if (string.IsNullOrEmpty(userFilterText))
                                {
                                    return;
                                }

                                var items = document.SelectNodes(xpath);
                                if (items == null)
                                {
                                    return;
                                }

                                bool XmlContains(XmlElement element, string text)
                                {
                                    return element.InnerXml.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1;
                                }

                                foreach (var i in items)
                                {
                                    var item = (XmlElement)i;
                                    if (item == null)
                                    {
                                        continue;
                                    }

                                    if (anyWord)
                                    {
                                        string[] words =
                                            userFilterText
                                                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(x => x.Trim()).ToArray();

                                        bool foundAnyWord = words.Any(word => XmlContains(item, word));

                                        if (removeWhenFound)
                                        {
                                            if (foundAnyWord)
                                            {
                                                item.ParentNode?.RemoveChild(item);
                                            }
                                        }
                                        else
                                        {
                                            if (!foundAnyWord)
                                            {
                                                item.ParentNode?.RemoveChild(item);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (removeWhenFound)
                                        {
                                            if (XmlContains(item, userFilterText))
                                            {
                                                item.ParentNode?.RemoveChild(item);
                                            }
                                        }
                                        else
                                        {
                                            if (!XmlContains(item, userFilterText))
                                            {
                                                item.ParentNode?.RemoveChild(item);
                                            }
                                        }
                                    }
                                }
                            }

                            const string filtersItemXPath = "//filters/item";
                            const string providersItemXPath = "//providers/item";

                            ApplyFilterToItem(filtersItemXPath, UserFilter, UserFilterAnyWord, false);
                            ApplyFilterToItem(filtersItemXPath, UserInvertFilter, UserInvertFilterAnyWord, true);

                            ApplyFilterToItem(providersItemXPath, UserFilter, UserFilterAnyWord, false);
                            ApplyFilterToItem(providersItemXPath, UserInvertFilter, UserInvertFilterAnyWord, true);

                            var stringBuilder = new StringBuilder();
                            var settings = new XmlWriterSettings
                            {
                                OmitXmlDeclaration = true,
                                Indent = true,
                            };

                            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
                            {
                                document.Save(xmlWriter);
                            }

                            WfpFormattedXmlFilters = stringBuilder.ToString();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Unable apply filter. Error: {ex.Message}");
                        }
                    }
                    finally
                    {
                        lock (this)
                        {
                            if (_requestUpdate)
                            {
                                _requestUpdate = false;
                                RunApplyFormatting();
                            }
                            else
                            {
                                _processingApplyFormatting = false;
                            }
                        }
                    }
                });
            }

            lock (this)
            {
                if (_processingApplyFormatting)
                {
                    _requestUpdate = true;
                    return;
                }

                _processingApplyFormatting = true;
            }
            
            RunApplyFormatting();
        }

        private void RefreshFiltersHandler(object timeout)
        {
            if (timeout == null)
            {
                UpdateFilters();
            }
            else
            {
                if (_timer.Enabled)
                {
                    _timer.Stop();
                }
                
                int seconds = Convert.ToInt32(timeout.ToString());

                _timer.Interval = (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds;
                _timer.Start();
            }
        }

        private void SaveWfpFiltersHandler(object obj)
        {
            SaveFilter(WfpXmlFilters);
        }

        private void SaveWfpFormattedFiltersHandler(object obj)
        {
            SaveFilter(WfpFormattedXmlFilters);
        }

        private void SaveFilter(string xml)
        {
            try
            {
                using (var fileDialog = new SaveFileDialog())
                {
                    fileDialog.AddExtension = true;
                    fileDialog.DefaultExt = "xml";
                    fileDialog.Filter = "XML files(*.xml)|*.xml|All files(*.*)|*.*";
                    if (fileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string fileName = fileDialog.FileName;

                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }

                        File.WriteAllText(fileName, xml);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Unable save the file, error: {exception.Message}");
            }
        }

        private void SaveSettings()
        {
            void WriteFileSettings(string key, object value)
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
                configuration.Save(ConfigurationSaveMode.Full, true);
                ConfigurationManager.RefreshSection("appSettings");
            }
            
            WriteFileSettings(nameof(UserFilter), _userFilter);
            WriteFileSettings(nameof(UserFilterAnyWord), _userFilterAnyWord);

            WriteFileSettings(nameof(UserInvertFilter), _userInvertFilter);
            WriteFileSettings(nameof(UserInvertFilterAnyWord), _userInvertFilterAnyWord);
        }

        private void LoadSettings()
        {
            string ReadFileSettings(string key)
            {
                return ConfigurationManager.AppSettings[key] ?? string.Empty;
            }

            bool ReadBoolean(string key)
            {
                var value = ReadFileSettings(key);
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }

                return bool.Parse(value);
            }

            _userFilter = ReadFileSettings(nameof(UserFilter));
            _userFilterAnyWord = ReadBoolean(nameof(UserFilterAnyWord));

            _userInvertFilter = ReadFileSettings(nameof(UserInvertFilter));
            _userInvertFilterAnyWord = ReadBoolean(nameof(UserInvertFilterAnyWord));
        }

        private void UpdateFilters()
        {
            if (_applyingFilters)
            {
                return;
            }

            _applyingFilters = true;

            Task.Run(() =>
            {
                try
                {
                    ReadingWfpFilters = true;
                    RunDispatcher(() =>
                    {
                        RefreshCommand.RaiseCanExecuteChanged();
                    });
                    
                    if (WfpExecutor.TryGetWfpXml(out string wfpXml, out string error))
                    {
                        WfpXmlFilters = wfpXml;
                        ApplyFormatting();
                    }
                    else
                    {
                        MessageBox.Show(error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to show filters, error: {ex.Message}");
                }
                finally
                {
                    ReadingWfpFilters = false;
                    _applyingFilters = false;
                }
            });
        }

        private void RunDispatcher(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }

            Application.Current.Dispatcher.BeginInvoke(action);
        }

    }
}
