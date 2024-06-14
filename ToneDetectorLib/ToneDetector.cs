using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Dsp;
using System.Diagnostics;

namespace ToneDetectorLib
{
    public class ToneDetector
    {
        public event Action<double> FrequencyADetected;
        public event Action<double> FrequencyBDetected;
        public event Action<double, double> TonePairDetected;
        public event Action<string> DetectionTimeout;

        private bool detectingATone = true;
        private double detectedFreqA = 0;
        private double detectedFreqB = 0;
        private const double SquelchLevel = 0.1; // Hardcoded for now, maybe add to config later
        private Stopwatch stopwatch = new Stopwatch();
        private const int ToneA_MinDuration = 700;  // Minimum duration in milliseconds for Tone A
        private const int ToneA_MaxDuration = 1000; // Maximum duration in milliseconds for Tone A
        private const int ToneB_MinDuration = 2500; // Minimum duration in milliseconds for Tone B
        private const int ToneB_MaxDuration = 3000; // Maximum duration in milliseconds for Tone B

        private WaveInEvent waveIn;

        public void Start(int deviceNumber)
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1),
                DeviceNumber = deviceNumber
            };
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();
        }

        public void Stop()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.DataAvailable -= OnDataAvailable;
                waveIn.Dispose();
                waveIn = null;
            }
        }

        public static List<string> GetAudioDevices()
        {
            List<string> devices = new List<string>();
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                devices.Add(deviceInfo.ProductName);
            }
            return devices;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            const int FFTLength = 4096;
            var fftBuffer = new Complex[FFTLength];
            var buffer = new float[e.Buffer.Length / 2];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
            }

            int sampleRate = 44100;

            for (int i = 0; i < FFTLength; i++)
            {
                fftBuffer[i].X = buffer[i];
                fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(FFTLength, 2.0), fftBuffer);

            var magnitudes = fftBuffer.Select(c => Math.Sqrt(c.X * c.X + c.Y * c.Y)).ToArray();
            var maxMagnitude = magnitudes.Max();
            var peakIndex = Array.IndexOf(magnitudes, maxMagnitude);

            if (maxMagnitude > SquelchLevel)
            {
                double detectedFrequency = Math.Round((double)(peakIndex * sampleRate) / FFTLength, 1);

                if (detectingATone)
                {
                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Start();
                    }
                    else if (stopwatch.ElapsedMilliseconds >= ToneA_MinDuration && stopwatch.ElapsedMilliseconds <= ToneA_MaxDuration)
                    {
                        detectedFreqA = detectedFrequency;
                        detectingATone = false;
                        stopwatch.Restart();
                        FrequencyADetected?.Invoke(detectedFreqA);
                    }
                    else if (stopwatch.ElapsedMilliseconds > ToneA_MaxDuration)
                    {
                        detectingATone = true;
                        stopwatch.Reset();
                        DetectionTimeout?.Invoke("Timeout: No A tone detected in expected time frame.");
                    }
                }
                else
                {
                    if (stopwatch.ElapsedMilliseconds >= ToneB_MinDuration && stopwatch.ElapsedMilliseconds <= ToneB_MaxDuration)
                    {
                        detectedFreqB = detectedFrequency;
                        detectingATone = true;
                        stopwatch.Reset();
                        FrequencyBDetected?.Invoke(detectedFreqB);
                        TonePairDetected?.Invoke(detectedFreqA, detectedFreqB);
                    }
                    else if (stopwatch.ElapsedMilliseconds > ToneB_MaxDuration)
                    {
                        detectingATone = true;
                        stopwatch.Reset();
                        DetectionTimeout?.Invoke("Timeout: No B tone detected in expected time frame.");
                    }
                }
            }
            else
            {
                if (!detectingATone && stopwatch.ElapsedMilliseconds > ToneB_MaxDuration)
                {
                    detectingATone = true;
                    stopwatch.Reset();
                    DetectionTimeout?.Invoke("Timeout: No B tone detected in expected time frame.");
                }
            }
        }
    }
}
