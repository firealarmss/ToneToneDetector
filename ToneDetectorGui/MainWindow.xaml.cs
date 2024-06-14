using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using ToneDetectorLib;
using ToneDetectorAlerting;

namespace ToneDetectorGui
{
    public partial class MainWindow : Window
    {
        private ToneDetector _toneDetector;
        private UdpAlertServer _alertServer;
        private List<QcIIPair> _tones = new List<QcIIPair>();
        private double _threshold = 5.0;
        private int _selectedDevice = 0;
        private bool _isListening = false;
        private bool _enableUdpServer = false;
        private string _udpAddress = "127.0.0.1";
        private int _udpPort = 11000;
        private const string AuthToken = "secret"; // TODO: Move to config window

        public MainWindow()
        {
            InitializeComponent();
            _toneDetector = new ToneDetector();
            _toneDetector.FrequencyADetected += OnFrequencyADetected;
            _toneDetector.FrequencyBDetected += OnFrequencyBDetected;
            _toneDetector.TonePairDetected += OnTonePairDetected;
            _toneDetector.DetectionTimeout += OnDetectionTimeout;

            LoadConfiguration();
            UpdateListeningStatus();

            if (_enableUdpServer)
            {
                _alertServer = new UdpAlertServer(_udpPort, AuthToken);
                _alertServer.StartAsync();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _toneDetector.Start(_selectedDevice);
            _isListening = true;
            UpdateListeningStatus();
            TextBoxFrequencyA.Text = string.Empty;
            TextBoxFrequencyB.Text = string.Empty;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _toneDetector.Stop();
            _isListening = false;
            UpdateListeningStatus();
        }

        private void UpdateListeningStatus()
        {
            Dispatcher.Invoke(() =>
            {
                if (_isListening)
                {
                    Title = "ToneDetector - Listening";
                }
                else
                {
                    Title = "ToneDetector - Stopped";
                }
            });
        }

        private void OnFrequencyADetected(double frequency)
        {
            Dispatcher.Invoke(() =>
            {
                TextBoxFrequencyA.Text = $"{frequency:F2} Hz";
            });
        }

        private void OnFrequencyBDetected(double frequency)
        {
            Dispatcher.Invoke(() =>
            {
                TextBoxFrequencyB.Text = $"{frequency:F2} Hz";
            });
        }

        private void OnTonePairDetected(double frequencyA, double frequencyB)
        {
            Dispatcher.Invoke(async () =>
            {
                TextBoxFrequencyA.Text = $"{frequencyA:F2} Hz";
                TextBoxFrequencyB.Text = $"{frequencyB:F2} Hz";
                CheckForAlert(frequencyA, frequencyB);
                if (_enableUdpServer && _alertServer != null)
                {
                    await _alertServer.SendToneReportAsync(frequencyA, frequencyB);
                }
            });
        }

        private void OnDetectionTimeout(string message)
        {
            Dispatcher.Invoke(() =>
            {
                //MessageBox.Show(message, "Detection Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        private void MenuItem_Configuration_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigurationWindow(_tones, _threshold, _selectedDevice, _enableUdpServer, _udpAddress, _udpPort);
            if (configWindow.ShowDialog() == true)
            {
                _tones = configWindow.Tones;
                _threshold = configWindow.Threshold;
                _selectedDevice = configWindow.SelectedDevice;
                _enableUdpServer = configWindow.EnableUdpServer;
                _udpAddress = configWindow.UdpAddress;
                _udpPort = configWindow.UdpPort;
                SaveConfiguration();
                if (_enableUdpServer)
                {
                    if (_alertServer == null)
                    {
                        _alertServer = new UdpAlertServer(_udpPort, AuthToken);
                        _alertServer.StartAsync();
                    }
                }
                else
                {
                    _alertServer?.Stop();
                    _alertServer = null;
                }
            }
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckForAlert(double frequencyA, double frequencyB)
        {
            foreach (var tonePair in _tones)
            {
                if (Math.Abs(tonePair.ToneA - frequencyA) <= _threshold && Math.Abs(tonePair.ToneB - frequencyB) <= _threshold)
                {
                    FlashScreen();
                    break;
                }
            }
        }

        private void FlashScreen()
        {
            Background = System.Windows.Media.Brushes.Red;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) =>
            {
                Background = System.Windows.Media.Brushes.White;
                timer.Stop();
            };
            timer.Start();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists("config.json"))
                {
                    var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
                    _tones = config.Tones;
                    _threshold = config.Threshold;
                    _selectedDevice = config.SelectedDevice;
                    _enableUdpServer = config.EnableUdpServer;
                    _udpAddress = config.UdpAddress;
                    _udpPort = config.UdpPort;
                }
            }
            catch (Exception)
            {
                //
            }
        }

        private void SaveConfiguration()
        {
            var config = new Config
            {
                Tones = _tones,
                Threshold = _threshold,
                SelectedDevice = _selectedDevice,
                EnableUdpServer = _enableUdpServer,
                UdpAddress = _udpAddress,
                UdpPort = _udpPort
            };
            File.WriteAllText("config.json", JsonSerializer.Serialize(config));
        }
    }

    public class Config
    {
        public List<QcIIPair> Tones { get; set; }
        public double Threshold { get; set; }
        public int SelectedDevice { get; set; }
        public bool EnableUdpServer { get; set; }
        public string UdpAddress { get; set; }
        public int UdpPort { get; set; }
    }
}
