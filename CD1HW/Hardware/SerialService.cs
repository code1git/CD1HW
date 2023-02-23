using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Collections;
using System.Threading;
using System.Windows.Forms;

namespace CD1HW.Hardware
{
    /// <summary>
    /// serial port service
    /// 바코드 리더기 지원용
    /// </summary>
    class SerialService
    {
        private readonly ILogger<SerialService> _logger;

        public SerialService(ILogger<SerialService> logger)
        {
            _logger = logger;
        }

        private List<SerialPort> serialPortList = new List<SerialPort>();
        private byte[] chkBuffer;

        private static readonly Lazy<SerialService> _insteance = new Lazy<SerialService>(() => new SerialService());
        private SerialService()
        {
            InitSerial();
        }
        public static SerialService Instance { get { return _insteance.Value; } }

        private void InitSerial()
        {
            chkBuffer = CreateCheckBuffer();
            string[] comlist = SerialPort.GetPortNames();
            foreach (string portName in comlist)
            {
                _logger.LogInformation(portName);
                SerialPort serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                try
                {
                    serialPort.WriteTimeout = 500;
                    serialPort.ReadTimeout = 500;
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                       
                        serialPort.Write(chkBuffer, 0, chkBuffer.Length);
                        byte[] tempBuffer = new byte[128];
                        int readCnt = serialPort.Read(tempBuffer, 0, tempBuffer.Length);
                        if (readCnt != 0)
                        {
                            serialPort.WriteTimeout = -1;
                            serialPort.ReadTimeout = -1;
                            _logger.LogInformation("Serial Port {0} is Init OK", portName);
                            serialPortList.Add(serialPort);
                            serialPort.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message);
                }
            }
        }

        public void StartSerialService()
        {
            foreach (SerialPort serialPort in serialPortList)
            {
                Thread thread = new Thread(() => { ReadSerialPort(serialPort, 100); });
                thread.Start();
            }
        }

        private void ReadSerialPort(SerialPort serialPort, int interval)
        {
            while (true)
            {
                try
                {
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        serialPort.Write(chkBuffer, 0, chkBuffer.Length);
                        byte[] parseBuffer = new byte[1024];
                        int parseLen = 0;
                        byte[] tempBuffer = new byte[128];
                        int tempParseLen = 0;
                        byte[] resultBuffer = new byte[128];
                        while (serialPort.IsOpen)
                        {
                            int readCnt = serialPort.Read(tempBuffer, 0, tempBuffer.Length);

                            for (int r = 0; r < readCnt; r++)
                            {
                                tempParseLen = parseLen;
                                parseBuffer[parseLen++] = tempBuffer[r];
                                if (parseBuffer[tempParseLen] == 0xC3) //mEND_BYTE
                                {
                                    for (int p = parseLen - 1; 0 <= p; p--)
                                    {
                                        if (parseBuffer[p] == 0xCD) //mSTART_BYTE_MCU
                                        {
                                            string rev = "";
                                            for (int i = p; i < parseLen; i++)
                                            {
                                                rev += parseBuffer[i].ToString("X2") + " ";
                                            }
                                            //logger.Debug("REV : " + rev);
                                        }

                                        if (parseBuffer[p + 1] == 0x42) // mCMD_QR_DATA
                                        {
                                            for (int k = 0; k < resultBuffer.Length; k++)
                                            {
                                                if (parseBuffer[k + p + 3] == 0xD || parseBuffer[k + p + 3] == 0xC3)
                                                {
                                                    resultBuffer[k] = 0xD;
                                                    k++;
                                                    break;
                                                }
                                                resultBuffer[k] = parseBuffer[k + p + 3];
                                            }
                                            string res = "";
                                            for (int i = 0; i < resultBuffer.Length; i++)
                                            {
                                                res += Convert.ToChar(resultBuffer[i]);

                                            }
                                            _logger.LogInformation("QR : " + res);
                                            // do somethig qr logic
                                            try
                                            {
                                                string qrSaveTxt = @".\recode\QrRead.txt";
                                                using (StreamWriter sw = new StreamWriter(qrSaveTxt, true))
                                                {
                                                    sw.WriteLine(res);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                _logger.LogWarning(e.StackTrace);
                                            }
                                            try
                                            {
                                                /*if (DemoUI.demoUI.textBox_qr.InvokeRequired)
                                                {
                                                    DemoUI.demoUI.textBox_qr.Invoke(new MethodInvoker(delegate { DemoUI.demoUI.textBox_qr.Text = res; }));
                                                }
                                                else
                                                {
                                                    DemoUI.demoUI.textBox_qr.Text = res;
                                                }*/

                                            }
                                            catch (Exception e)
                                            {
                                                _logger.LogWarning(e.StackTrace);
                                            }
                                            parseBuffer = new byte[1024];
                                            serialPort.Close();
                                        }

                                    }

                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                   _logger.LogCritical(e.StackTrace);
                }
                finally
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                }

                Thread.Sleep(interval);
            }
        }

        private byte[] CreateCheckBuffer()
        {
            byte[] buffer = new byte[6];
            //byte mCHKSUM_BYTE = 0x7F;
            int len = 0;
            buffer[len++] = 0xCE; //mSTART_BYTE_PC
            buffer[len++] = 0x01; //mCMD_RQ_READY
            buffer[len++] = 0x01;
            buffer[len++] = 0x00;
            buffer[len++] = (0xCE + 0x01 + 0x01 + 0x00) & 0x7F;
            buffer[len++] = 0xC3; //mEND_BYTE
            return buffer;
        }

    }
}
