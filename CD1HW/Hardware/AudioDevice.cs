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
using Windows.ApplicationModel.VoiceCommands;

namespace CD1HW.Hardware
{
    /// <summary>
    /// 오디오장치 접근을 위한 class
    /// </summary>
    public class AudioDevice
    {
        private readonly ILogger<AudioDevice> _logger;
        public AudioDevice(ILogger<AudioDevice> logger)
        {
            _logger = logger;
            
        }

        /*private AudioDevice() { }
        private static readonly Lazy<AudioDevice> _insteance = new Lazy<AudioDevice>(() => new AudioDevice());
        public static AudioDevice Instance { get { return _insteance.Value; } }*/

        private int inputDeviceIdx = -1;
        public  MMDevice outputDevice;
        private WaveInEvent waveSource;
        private WaveFileWriter waveFile;
        private WasapiOut wasapiOut;

        /// <summary>
        /// 음성파일 출력
        /// </summary>
        /// <param name="waveFilepath">출력할 음성파일의 경로</param>
        public void PlaySound(string waveFilepath)
        {
            //if (outputDevice == null)
            //    outputDevice = SelectOutputDevice("USB Audio Device");
            AudioFileReader audioFileReader = new AudioFileReader(waveFilepath);
            wasapiOut = new WasapiOut(AudioClientShareMode.Shared, false, 0);
            //swasapiOut = new WasapiOut(outputDevice, AudioClientShareMode.Shared, false, 0);
            wasapiOut.Init(audioFileReader);
            wasapiOut.Play();

            //동기 처리시의 코드 (사용x)
            /*while (wasapiOut.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }*/
        }

        // 출력중인 음성 정지
        public void StopSound()
        {
            try
            {
                wasapiOut.Stop();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 음성 녹음
        /// </summary>
        /// <param name="saveFilePath">녹음된 음성파일의 path</param>
        /// <returns>NAudio.Wave.WaveInEvent</returns>
        public WaveInEvent RecordStart(string saveFilePath)
        {
            if (inputDeviceIdx == -1)
                inputDeviceIdx = SelectInputDevice("USB Audio Device");
            if (inputDeviceIdx == -1)
            {
                _logger.LogInformation("Audio Input Device Not Found");
                return null;
            }
            waveSource = new WaveInEvent();
            waveSource.DeviceNumber = inputDeviceIdx;
            waveSource.WaveFormat = new WaveFormat(44100, 1);
            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(RecordStop);

            waveFile = new WaveFileWriter(saveFilePath, waveSource.WaveFormat);

            //_logger.LogInformation("st rec");
            waveSource.StartRecording();
            //Thread.Sleep(timemills);
            //logger.Debug("ed rec");
            return waveSource;
        }

        /// <summary>
        /// 음성녹음 완료시의 EventHandler 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        /// <summary>
        /// 음성녹음 중지시의 EventHandler 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// device 이름으로 음성 녹음 device 선택
        /// </summary>
        /// <param name="deviceName">선택할 input device의 이름</param>
        /// <returns>audio device의 index</returns>
        public int SelectInputDevice(string deviceName)
        {
            for (int idx = 0; idx < WaveIn.DeviceCount; ++idx)
            {
                string devName = WaveIn.GetCapabilities(idx).ProductName;
                if (devName.Contains(deviceName))
                {
                    _logger.LogInformation("Audio In : " + devName);
                    return idx;
                }
            }
            return -1;
        }

        /// <summary>
        /// 음성 출력 디바이스 선택
        /// </summary>
        /// <param name="deviceName">검색할 output device의 이름</param>
        /// <returns>MMDevice</returns>
        public MMDevice SelectOutputDevice(string deviceName)
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All))
            {
                if (device.FriendlyName.Contains(deviceName) && device.State == DeviceState.Active)
                {
                    _logger.LogInformation("Audio out : " + outputDevice.DeviceFriendlyName);
                    return device;
                }
            }
            _logger.LogInformation("Audio output Device Not Found");
            enumerator.Dispose();
            return null;
        }
    }
}
