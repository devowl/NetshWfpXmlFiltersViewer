using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using NetshWfpViewer.Annotations;
using NetshWfpViewer.Utilities;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Windows.Forms.Timer;

namespace NetshWfpViewer.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
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
        private bool _requestUpdate;
        private bool _processingApplyFormatting;
        private bool _userFilterAnyWord;
        private bool _userInvertFilterAnyWord;
        private int _timerSecondsLeft;
        private bool _timerWorks;
        private int _timerTotalSeconds;
        private XmlWriterSettings _writerSettings;

        public int TimerTotalSeconds
        {
            get => _timerTotalSeconds;
            set => SetFieldRaisePropertyChanged(ref _timerTotalSeconds, value);
        }

        public bool TimerWorks
        {
            get => _timerWorks;
            set => SetFieldRaisePropertyChanged(ref _timerWorks, value);
        }

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

        public int TimerSecondsLeft
        {
            get => _timerSecondsLeft;
            set => SetFieldRaisePropertyChanged(ref _timerSecondsLeft, value);
        }

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

        public RelayCommand SaveLeftCommand { get; }

        public RelayCommand SaveRightCommand { get; }

        public RelayCommand RefreshCommand { get; }

        public RelayCommand RefreshStopCommand { get; }

        public MainWindowViewModel(Action<string> showFilters, Action<string> showFormattedFilters)
        {
            _showFilters = showFilters;
            _showFormattedFilters = showFormattedFilters;
            LoadSettings();
            UpdateFilters();

            SaveLeftCommand = new RelayCommand(SaveWfpFiltersHandler);
            SaveRightCommand = new RelayCommand(SaveWfpFormattedFiltersHandler);
            RefreshCommand = new RelayCommand(RefreshFiltersHandler, o => ReadingWfpFilters == false);
            RefreshStopCommand = new RelayCommand(o => TimerStop());

            _writerSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
            };

            _timer = new Timer
            {
                Enabled = false,
            };

            _timer.Tick += TimerUpdateTick;
        }

        private void TimerUpdateTick(object sender, EventArgs e)
        {
            TimerSecondsLeft--;

            if (TimerSecondsLeft == 0)
            {
                UpdateFilters();
                TimerSecondsLeft = TimerTotalSeconds;
            }
        }

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
                            XmlDocument document = new();
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

                            const string filtersItemXPath = "//filters/item";
                            const string providersItemXPath = "//providers/item";

                            WfpXmlFilter.ApplyFilterToItem(document, filtersItemXPath, UserFilter, UserFilterAnyWord,
                                false);
                            WfpXmlFilter.ApplyFilterToItem(document, filtersItemXPath, UserInvertFilter,
                                UserInvertFilterAnyWord, true);

                            WfpXmlFilter.ApplyFilterToItem(document, providersItemXPath, UserFilter, UserFilterAnyWord,
                                false);
                            WfpXmlFilter.ApplyFilterToItem(document, providersItemXPath, UserInvertFilter,
                                UserInvertFilterAnyWord, true);

                            WfpFormattedXmlFilters = GetFormattedDocumentXml(document);
                        }
                        catch (Exception ex)
                        {
                            ShowMessage("XML filter apply", ex);
                        }
                    }
                    catch (Exception exception)
                    {
                        ShowMessage("Apply formatting", exception);
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

        private string GetFormattedDocumentXml(XmlDocument document)
        {
            var stringBuilder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(stringBuilder, _writerSettings))
            {
                document.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }

        private void RefreshFiltersHandler(object timeout)
        {
            if (timeout == null)
            {
                UpdateFilters();
            }
            else
            {
                TimerStart(Convert.ToInt32(timeout));
            }
        }

        private void TimerStart(int timeout)
        {
            if (_timer.Enabled)
            {
                TimerStop();
            }

            TimerWorks = true;

            TimerSecondsLeft = TimerTotalSeconds = timeout;
            _timer.Interval = 1000;
            _timer.Start();
        }

        private void TimerStop()
        {
            TimerWorks = false;
            _timer.Stop();
        }

        private void SaveWfpFiltersHandler(object obj)
        {
            SaveFilter(WfpXmlFilters);
        }

        private void SaveWfpFormattedFiltersHandler(object obj)
        {
            SaveFilter(WfpFormattedXmlFilters);
        }

        private static void SaveFilter(string xml)
        {
            try
            {
                using var fileDialog = new SaveFileDialog();
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = "xml";
                fileDialog.Filter = "XML files(*.xml)|*.xml|All files(*.*)|*.*";
                if (fileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string fileName = fileDialog.FileName;

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                File.WriteAllText(fileName, xml);
            }
            catch (Exception exception)
            {
                ShowMessage("File to save operation", exception);
            }
        }

        private void SaveSettings()
        {
            AppSettings.WriteFileSettings(nameof(UserFilter), _userFilter);
            AppSettings.WriteFileSettings(nameof(UserFilterAnyWord), _userFilterAnyWord);

            AppSettings.WriteFileSettings(nameof(UserInvertFilter), _userInvertFilter);
            AppSettings.WriteFileSettings(nameof(UserInvertFilterAnyWord), _userInvertFilterAnyWord);
        }

        private void LoadSettings()
        {
            _userFilter = AppSettings.ReadFileSettings(nameof(UserFilter));
            _userFilterAnyWord = AppSettings.ReadBoolean(nameof(UserFilterAnyWord));

            _userInvertFilter = AppSettings.ReadFileSettings(nameof(UserInvertFilter));
            _userInvertFilterAnyWord = AppSettings.ReadBoolean(nameof(UserInvertFilterAnyWord));
        }

        public static void ShowMessage(string source, Exception exception)
        {
            var message = exception.Message;
            message = $"Source: {source}{Environment.NewLine}Error:{message}";
            MessageBox.Show(message, "Error occurred", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    try
                    {
                        
                        var wfpXml = WfpExecutor.GetWfpXml();
                        XmlDocument document = new();
                        document.LoadXml(wfpXml);

                        WfpXmlFilters = GetFormattedDocumentXml(document);
                        ApplyFormatting();
                    }
                    catch (Exception exception)
                    {
                        ShowMessage("netsh command execute", exception);
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Reading WFP xml", ex);
                }
                finally
                {
                    ReadingWfpFilters = false;
                    _applyingFilters = false;
                }
            });
        }

        private static void RunDispatcher(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }

            Application.Current.Dispatcher.BeginInvoke(action);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void SetFieldRaisePropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;

            RunDispatcher(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}
