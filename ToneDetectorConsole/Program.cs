using NAudio.Wave;
using System;
using ToneDetectorLib;

namespace ToneToneDetectorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ToneDetectorConsole is starting...");

            var toneDetector = new ToneDetector();
            toneDetector.FrequencyADetected += freq => Console.WriteLine($"Detected Frequency A: {freq:F2} Hz");
            toneDetector.FrequencyBDetected += freq => Console.WriteLine($"Detected Frequency B: {freq:F2} Hz");
            toneDetector.TonePairDetected += (freqA, freqB) => Console.WriteLine($"Complete Tone Pair Detected: A = {freqA:F2} Hz, B = {freqB:F2} Hz");
            toneDetector.DetectionTimeout += message => Console.WriteLine(message);

            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine($"\t{waveInDevice}\t- {deviceInfo.ProductName}");
            }

            int deviceNumber = 5; // TODO: Command line arg
            toneDetector.Start(deviceNumber);

            Console.WriteLine("Listening for QuickCall 2 tones. Press any key to stop...");
            Console.ReadKey();

            toneDetector.Stop();
        }
    }
}
