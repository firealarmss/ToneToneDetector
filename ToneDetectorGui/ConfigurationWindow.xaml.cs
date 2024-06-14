using System;
using System.Collections.Generic;
using System.Windows;
using ToneDetectorLib;

namespace ToneDetectorGui
{
    public partial class ConfigurationWindow : Window
    {
        public List<QcIIPair> Tones { get; private set; }
        public double Threshold { get; private set; }
        public int SelectedDevice { get; private set; }
        public bool EnableUdpServer { get; private set; }
        public string UdpAddress { get; private set; }
        public int UdpPort { get; private set; }

        public ConfigurationWindow(List<QcIIPair> tones, double threshold, int selectedDevice, bool enableUdpServer, string udpAddress, int udpPort)
        {
            InitializeComponent();
            Tones = tones;
            Threshold = threshold;
            SelectedDevice = selectedDevice;
            EnableUdpServer = enableUdpServer;
            UdpAddress = udpAddress;
            UdpPort = udpPort;

            TextBoxThreshold.Text = threshold.ToString();
            CheckBoxEnableUdp.IsChecked = enableUdpServer;
            TextBoxUdpAddress.Text = udpAddress;
            TextBoxUdpPort.Text = udpPort.ToString();

            foreach (var tone in tones)
            {
                ListBoxTones.Items.Add(tone);
            }
            LoadAudioDevices();
        }

        private void LoadAudioDevices()
        {
            ComboBoxAudioDevices.Items.Clear();
            var devices = ToneDetector.GetAudioDevices();
            foreach (var device in devices)
            {
                ComboBoxAudioDevices.Items.Add(device);
            }
            if (ComboBoxAudioDevices.Items.Count > 0)
            {
                ComboBoxAudioDevices.SelectedIndex = SelectedDevice;
            }
        }

        private void AddTone_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TextBoxToneA.Text, out double toneA) && double.TryParse(TextBoxToneB.Text, out double toneB))
            {
                var newTonePair = new QcIIPair
                {
                    Alias = TextBoxAlias.Text,
                    ToneA = toneA,
                    ToneB = toneB
                };
                Tones.Add(newTonePair);
                ListBoxTones.Items.Add(newTonePair);
                TextBoxAlias.Clear();
                TextBoxToneA.Clear();
                TextBoxToneB.Clear();
            }
            else
            {
                MessageBox.Show("Invalid tone frequency", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTone_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxTones.SelectedItem is QcIIPair selectedTone)
            {
                Tones.Remove(selectedTone);
                ListBoxTones.Items.Remove(selectedTone);
            }
            else
            {
                MessageBox.Show("Select a tone pair to delete", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TextBoxThreshold.Text, out double threshold) && int.TryParse(TextBoxUdpPort.Text, out int udpPort))
            {
                Threshold = threshold;
                SelectedDevice = ComboBoxAudioDevices.SelectedIndex;
                EnableUdpServer = CheckBoxEnableUdp.IsChecked ?? false;
                UdpAddress = TextBoxUdpAddress.Text;
                UdpPort = udpPort;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid input values", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
