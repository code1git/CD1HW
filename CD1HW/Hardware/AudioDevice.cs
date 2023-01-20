using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Midi;
using Microsoft.VisualBasic.Logging;
using WebSocketsSample.Controllers;

namespace CD1HW.Hardware
{
    class AudioDevice
    {
        private readonly ILogger<AudioDevice> _logger;

        public AudioDevice(ILogger<AudioDevice> logger)
        {
            _logger = logger;
        }

        private int inputDeviceIdx = -1;
        private MMDevice outputDevice;
        private WaveInEvent waveSource;
        private WaveFileWriter waveFile;

        public void PlaySound(string waveFilepath)
        {
            if (outputDevice == null)
                SelectOutputDevice();
            AudioFileReader audioFileReader = new AudioFileReader(waveFilepath);

            WasapiOut wasapiOut = new WasapiOut(outputDevice, AudioClientShareMode.Shared, false, 0);
            wasapiOut.Init(audioFileReader);
            wasapiOut.Play();
            while (wasapiOut.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        public WaveInEvent RecordStart(string saveFilePath)
        {
            if (inputDeviceIdx == -1)
                SelectInputDevice();
            waveSource = new WaveInEvent();
            waveSource.DeviceNumber = inputDeviceIdx;
            waveSource.WaveFormat = new WaveFormat(44100, 1);
            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(RecordStop);

            waveFile = new WaveFileWriter(saveFilePath, waveSource.WaveFormat);

            _logger.LogInformation("st rec");
            waveSource.StartRecording();
            //Thread.Sleep(timemills);
            //logger.Debug("ed rec");
            return waveSource;
        }

        void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        void RecordStop(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }


        }

        public void SelectInputDevice()
        {
            for (int idx = 0; idx < WaveIn.DeviceCount; ++idx)
            {
                string devName = WaveIn.GetCapabilities(idx).ProductName;
                if (devName.Contains("USB Audio Device"))
                {
                    inputDeviceIdx = idx;
                    break;
                }
            }
            if (inputDeviceIdx == -1)
            {
                _logger.LogInformation("Audio Input Device Not Found");
            }
            else
            {
                _logger.LogInformation("Audio In : " + WaveIn.GetCapabilities(inputDeviceIdx).ProductName);
            }
        }

        public void SelectOutputDevice()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All))
            {
                if (device.FriendlyName.Contains("USB Audio Device") && device.State == DeviceState.Active)
                {
                    outputDevice = device;
                }
            }

            if (outputDevice == null)
            {
                _logger.LogInformation("Audio output Device Not Found");
            }
            else
            {
                _logger.LogInformation("Audio out : " + outputDevice.DeviceFriendlyName);
            }
            //enumerator.Dispose();
        }
    }
}
