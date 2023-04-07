using CD1HW.Grpc;
using CD1HW.Model;
using CD1HW.WinFormUi;
using OpenCvSharp.ML;
using SharpDX;
using Suprema.SFM_SDK_NET;
using System.Drawing;
using System.IO.Ports;
using Windows.Networking;

namespace CD1HW.Hardware
{
    /// <summary>
    /// 시리얼 포트를 사용하는 하드웨어 제어
    /// 슈프리마 기기의 사양 문제로 새로 구성함
    /// </summary>
    public class SerialService
    {
        private readonly ILogger<SerialService> _logger;
        private readonly OcrCamera _ocrCamera;
        private readonly IdScanRpcClient _idScanRpcClient;
        public SerialService(ILogger<SerialService> logger, OcrCamera ocrCamera, IdScanRpcClient idScanRpcClient)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
            _idScanRpcClient = idScanRpcClient;
        }


        private List<SerialPort> serialPortList = new List<SerialPort>();
        private byte[] chkBuffer;
        private string sfmPortName = "COM2";

        private bool isInterrupted = false;
        private String interrupMsg = "IR_ON";
        private byte[] interrupMsgBuffer;

        

        // 오성기기 통신 프로토콜
        private byte mSTART_BYTE_MCU = 0XCD;
        private byte mSTART_BYTE_PC = 0xCE;
        private byte mEND_BYTE = 0xC3;
        private byte mCHKSUM_BYTE = 0x7F;

        private int mSTART_INDEX = 0;
        private int mCMD_INDE = 1;
        private int mDATA_LT_INDEX = 2;

        private byte mCMD_RQ_READY = 0x01;
        private byte mCMD_READY = 0x02;
        private byte mCMD_ACK = 0x03;

        private byte mCMD_SAVE_POS = 0x10;
        private byte mCMD_REQUEST_POS = 0x11;
        private byte mCMD_SEND_POS = 0x12;

        private byte mCMD_CAPTURE = 0x20;
        private byte mCMD_BUTTON = 0x21;
        private byte mCMD_IR_LED = 0x22;
        private byte mCMD_CAM_MODEL = 0x23; //0:GC2053 , 1:OS05A10_FHD , 2:OS05A10_QHD

        private byte mCMD_PF_RQ_READY = 0x30;
        private byte mCMD_PF_READY = 0x31;
        private byte mCMD_PF_C_READY = 0x32;
        private byte mCMD_PF_CAPTURE = 0x33;

        private byte mCMD_QR_RQ_READY = 0x40;
        private byte mCMD_QR_READY = 0x41;
        private byte mCMD_QR_DATA = 0x42;

        /// <summary>
        /// 시리얼 포트를 체크해서 상양에 맞는 포트점유
        /// 오성시스템(카메라, 버튼 등) : 체크데이터를 보내서 해당 데이터에대한 반환이 오면 오성기기로 인식
        /// 슈프리마 지문 : SDK사양상 com port 지문모듈의 데이터가 반환되면 기기로 취급
        /// </summary>
        public void CheckSerialPort()
        {
            chkBuffer = CreateCheckBuffer();


            string[] comlist = SerialPort.GetPortNames();
            foreach (string portName in comlist)
            {

                SerialPort serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                serialPort.WriteTimeout = 500;
                serialPort.ReadTimeout = 500;
                try
                {
                    SFM_SDK_NET SFM = new SFM_SDK_NET();
                    UF_RET_CODE result = new UF_RET_CODE();
                    result = SFM.UF_InitCommPort(portName, 115200, false);
                    if(result == UF_RET_CODE.UF_RET_SUCCESS)
                    {
                        SFM.UF_SetGenericCommandTimeout(500);
                        Console.WriteLine(portName + " " + result.ToString());
                        UF_MODULE_TYPE uF_MODULE_TYPE = new UF_MODULE_TYPE();
                        UF_MODULE_VERSION uF_MODULE_VERSION = new UF_MODULE_VERSION();
                        UF_MODULE_SENSOR uF_MODULE_SENSOR = new UF_MODULE_SENSOR();
                        result = SFM.UF_GetModuleInfo(ref uF_MODULE_TYPE, ref uF_MODULE_VERSION, ref uF_MODULE_SENSOR);
                        string sfmModuleInfo = SFM.UF_GetModuleString(uF_MODULE_TYPE, uF_MODULE_VERSION, uF_MODULE_SENSOR);
                        Console.WriteLine(uF_MODULE_TYPE.ToString());
                        if (!uF_MODULE_TYPE.ToString().Equals("UF_MODULE_3000"))
                        {
                            sfmPortName = portName;
                            _logger.LogInformation("SFM port : " + portName);
                        }
                        Console.WriteLine(portName + " " + sfmModuleInfo);

                    }
                    SFM.UF_CloseCommPort();

                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message);
                    Console.WriteLine(e.StackTrace);

                }
 
                try
                {
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
                            _logger.LogInformation("Serial Port {0} is OSUNG port. Init OK", portName);
                            serialPortList.Add(serialPort);
                        }

                        serialPort.Close();

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
                _logger.LogDebug("start comport " + serialPort.PortName);
                Thread thread = new Thread(() => { ReadSerialPort(serialPort, 0); });
                thread.Start();


            }
            /*SerialPort serialPortDev = new SerialPort("COM13");
            Thread thread1 = new Thread(() => { ReadSerialPort(serialPortDev, 0); });
            thread1.Start();*/
        }

        private void ReadSerialPort(SerialPort serialPort, int interval)
        {
            try
            {
                chkBuffer = CreateCheckBuffer();
                serialPort.ReadTimeout = 500;
                serialPort.Open();
                //if (serialPort.IsOpen)
                    //serialPort.Write(chkBuffer, 0, chkBuffer.Length);

                byte[] parseBuffer = new byte[1024];
                int parseLen = 0;
                byte[] tempBuffer = new byte[1024];
                int tempParseLen = 0;
                byte[] resultBuffer = new byte[128];

                while (serialPort.IsOpen)
                {
                    if (isInterrupted && (interrupMsgBuffer != null))
                    {
                        isInterrupted = false;
                        //byte[] cmdBuffer = CreateIROnOff(true);

                        serialPort.Write(interrupMsgBuffer, 0, interrupMsgBuffer.Length);
                    }
                    int byteToRead = serialPort.BytesToRead;
                    int readCnt = 0;
                    if (byteToRead == 0)
                        continue;
                    else
                        readCnt = serialPort.Read(tempBuffer, 0, tempBuffer.Length);

                    for (int r = 0; r < readCnt; r++)
                    {
                        tempParseLen = parseLen;

                        parseBuffer[parseLen++] = tempBuffer[r];

                        if (parseBuffer[tempParseLen] == mEND_BYTE) //mEND_BYTE
                        {
                            for (int p = parseLen - 1; 0 <= p; p--)
                            {
                                if (parseBuffer[p] == mSTART_BYTE_MCU) //mSTART_BYTE_MCU
                                {
                                    string rev = "";
                                    for (int i = p; i < parseLen; i++)
                                    {
                                        rev += parseBuffer[i].ToString("X2") + " ";
                                    }
                                    //_logger.LogInformation("REV : " + rev);

                                    if (parseBuffer[p + 1] == mCMD_QR_DATA) // mCMD_QR_DATA
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
                                            string qrSaveTxt = @".\Result\QrRead.txt";
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
                                            _ocrCamera.ocrResult.qrResult = res;
                                            _ocrCamera.OcrResultUpdated();

                                        }
                                        catch (Exception e)
                                        {
                                            _logger.LogWarning(e.StackTrace);
                                        }
                                    }
                                    else //not qr
                                    {
                                        byte Parity = parseBuffer[parseLen - 2];
                                        int ParseBatchLen = parseLen - p;
                                        byte CalParity = 0x00;
                                        for (int i = 0; i < ParseBatchLen -2; i++)
                                        {
                                            CalParity += parseBuffer[p + i];
                                        }
                                        CalParity = (byte)(CalParity & mCHKSUM_BYTE);
                                        if (Parity == CalParity)
                                        {
                                            if (parseBuffer[p] == mSTART_BYTE_MCU)
                                            {
                                                byte cmd = parseBuffer[p + 1];
                                                if(cmd == mCMD_BUTTON)
                                                {
                                                    OcrResult ocrResult = new OcrResult();
                                                    OCRInfo oCRInfo = new OCRInfo();
                                                    try
                                                    {
                                                        byte[] msgBuffer = CreateAutoIR(1);
                                                        switch (_ocrCamera.IRMode)
                                                        {
                                                            case "off":
                                                                oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                                                                ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                                                                break;
                                                            case "on":

                                                                serialPort.Write(msgBuffer, 0, msgBuffer.Length);
                                                                Thread.Sleep(_ocrCamera.IRTimesleep);
                                                                
                                                                oCRInfo.ImgBase64 = _ocrCamera.imageBase64;

                                                                Thread.Sleep(_ocrCamera.IRTimesleep);

                                                                ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);

                                                                break;
                                                            case "both":
                                                                oCRInfo.ImgBase64 = _ocrCamera.imageBase64;

                                                                serialPort.Write(msgBuffer, 0, msgBuffer.Length);
                                                                Thread.Sleep(_ocrCamera.IRTimesleep);
                                                                oCRInfo.IrImgBase64 = _ocrCamera.imageBase64;
                                                                Thread.Sleep(_ocrCamera.IRTimesleep);

                                                                ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                                                                break;
                                                            case "dev":
                                                                serialPort.Write(msgBuffer, 0, msgBuffer.Length);

                                                                break;
                                                            default:
                                                                oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                                                                ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                                                                break;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e.StackTrace);
                                                    }
                                                    _ocrCamera.ocrResult = ocrResult;
                                                    _ocrCamera.OcrResultUpdated();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogCritical("Parity Error");
                                        }
                                    }
                                    parseLen = 0;
                                }


                            }

                        }
                    }
                    Thread.Sleep(interval);
                    
                    
                }
                    

               
            }
            catch (Exception e)
            {
                //_logger.LogCritical(e.StackTrace);
            }
            finally
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
        }

        public void IRSwitch(bool sw)
        {
            isInterrupted = true;
            /*if (sw)
                interrupMsg = "IR_ON";
            else
                interrupMsg = "IR_OFF";*/
            //chkBuffer = CreateIROnOff(sw);
            interrupMsgBuffer = CreateAutoIR(1);
        }

        public Bitmap ScanFingerSFM()
        {
            try
            {
                if(sfmPortName != null)
                {
                    SFM_SDK_NET SFM = new SFM_SDK_NET();
                    UF_RET_CODE result = new UF_RET_CODE();
                    result = SFM.UF_InitCommPort(sfmPortName, 115200, false);
                    Console.WriteLine(sfmPortName + " " + result.ToString());

                    UFImage uFImage = new UFImage();
                    result = SFM.UF_ScanImage(ref uFImage);
                    Console.WriteLine(result.ToString());
                    Bitmap bitmap = SFM.UF_ConvertToBitmap(uFImage);
                    SFM.UF_CloseCommPort();

                    return bitmap;


                }
                else
                {
                    _logger.LogCritical("no SFM module");

                }

            }
            catch (Exception)
            {

            }

            return null;
        }

        private byte[] CreateCheckBuffer()
        {
            byte[] buffer = new byte[20];
            //byte mCHKSUM_BYTE = 0x7F;
            int len = 0;
            buffer[len++] = mSTART_BYTE_PC; //mSTART_BYTE_PC
            buffer[len++] = mCMD_RQ_READY; //mCMD_RQ_READY
            buffer[len++] = 0x01;
            buffer[len++] = 0x00;
            buffer[len++] = CreatChk(buffer, len);
            buffer[len++] = mEND_BYTE; //mEND_BYTE
            return buffer;
        }


        private byte[] CreateIROnOff(bool sw)
        {
            //Console.WriteLine(Convert.ToByte(sw));
            byte[] buffer = new byte[20];
            byte data = Convert.ToByte(sw);
            int len = 0;
            buffer[len++] = mSTART_BYTE_PC; //mSTART_BYTE_PC
            buffer[len++] = mCMD_IR_LED;
            buffer[len++] = 0x01;
            buffer[len++] = data;
            buffer[len++] = CreatChk(buffer, len);
            buffer[len++] = mEND_BYTE; //mEND_BYTE
            return buffer;
        }

        private byte[] CreateAutoIR(int s)
        {
            byte[] buffer = new byte[20];
            int len = 0;
            buffer[len++] = mSTART_BYTE_PC; //mSTART_BYTE_PC
            buffer[len++] = mCMD_CAPTURE;
            buffer[len++] = 0x01;
            buffer[len++] = (byte)(Convert.ToByte(s)|0x80);
            buffer[len++] = CreatChk(buffer, len);
            buffer[len++] = mEND_BYTE; //mEND_BYTE
            return buffer;
        }

        private byte CreatChk(byte[] buffer, int len)
        {
            if (buffer.Length < len){
                return 0;
            }
            byte rtn = 0x00;
            for(int i = 0; i < len; i++) {
                rtn += buffer[i];
            }
            return (byte)(rtn & mCHKSUM_BYTE);

        }
    }
}
