using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.Model;
using NAudio.Wave;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace CD1HW.WinFormUi
{
    /// <summary>
    /// Windows용 데모 UI
    /// </summary>
    public partial class DemoUI : Form
    {
        public static DemoUI demoUI;
        private static Bitmap bitmapImage;
        private Thread fingerprintThread;
        private WaveInEvent waveIn;
        private bool isNowRec = false;
        private int BUTTON_IMAGE_WIDTH = 30;
        private int BUTTON_IMAGE_HEIGHT = 30;

        private readonly OcrCamera _ocrCamera;
        private readonly IdScanRpcClient _idScanRpcClient;
        private readonly AudioDevice _audioDevice;
        private readonly SerialService _serialService;

        public DemoUI(OcrCamera ocrCamera, IdScanRpcClient idScanRpcClient, AudioDevice audioDevice, SerialService serialService)
        {
            InitializeComponent();
            _ocrCamera = ocrCamera;
            _ocrCamera.OcrResultUpdate += new EventHandler<OcrResult>(updateOcrResult);
            _idScanRpcClient = idScanRpcClient;
            _serialService = serialService;
            demoUI = this;
            upadateCameraFrame();
            SetImageOnButton(button_photo, Properties.Resources.scan_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
            SetImageOnButton(button_fingerprint, Properties.Resources.User_Interface_Fingerprint_Scan_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
            SetImageOnButton(button_record, Properties.Resources.mic_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
        }

        private void upadateCameraFrame()
        {
            Thread cameraThread = new Thread(new ThreadStart(upadateCameraFrameTherad));
            cameraThread.IsBackground = true;
            cameraThread.Start();
        }
        private void upadateCameraFrameTherad()
        {
            while (true)
            {
                try
                {
                    
                    Bitmap cameraBitmap = _ocrCamera.cameraBitmap;
                    if (cameraBitmap != null)
                    {
                        if (camera_image1.InvokeRequired)
                        {
                            camera_image1.Invoke(new MethodInvoker(delegate { camera_image1.Image = cameraBitmap; }));
                        }
                        else
                        {
                            camera_image1.Image = cameraBitmap;
                        }
                    }
                    else
                    {
                    }

                    if (_ocrCamera.fingerBitmap != null)
                    {
                        try
                        {
                            Bitmap fingerBitmap = _ocrCamera.fingerBitmap;
                            Bitmap resizedFingerImage = new Bitmap(pictureBox_finger.Width, pictureBox_finger.Height);
                            using (Graphics g = Graphics.FromImage(resizedFingerImage))
                            {
                                g.DrawImage(fingerBitmap, new RectangleF(0, 0, resizedFingerImage.Width, resizedFingerImage.Height), new RectangleF(new PointF(0, 0), fingerBitmap.Size), GraphicsUnit.Pixel);
                            }
                            if (pictureBox_finger.InvokeRequired)
                            {
                                pictureBox_finger.Invoke(new MethodInvoker(delegate { pictureBox_finger.Image = resizedFingerImage; }));
                            }
                            else
                            {
                                pictureBox_finger.Image = resizedFingerImage;
                            }

                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        if (pictureBox_finger.InvokeRequired)
                        {
                            pictureBox_finger.Invoke(new MethodInvoker(delegate { pictureBox_finger.Image = null; }));
                        }
                        else
                        {
                            pictureBox_finger.Image = null;
                        }
                    }

                    Thread.Sleep(15);
                    
                }
                catch (Exception e)
                {
                    
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void updateOcrResult(object sender, OcrResult ocrResult)
        {
            try
            {
                if (ocrResult.id_card_type != null)
                {
                    string idCardType = "";
                    switch (ocrResult.id_card_type)
                    {
                        case "jumin_card":
                            idCardType = "주민등록증";
                            break;
                        case "driver_card":
                            idCardType = "운전면허증";
                            break;
                        case "welfare_card":
                            idCardType = "복지카드";
                            break;
                        case "alien_card":
                            idCardType = "외국인등록증";
                            break;
                        case "overseas_card":
                            idCardType = "국내거소신고증";
                            break;
                        default:
                            break;
                    }
                    if (textBox_idcard_type.InvokeRequired)
                    {
                        textBox_idcard_type.Invoke(new MethodInvoker(delegate { textBox_idcard_type.Text = idCardType; }));
                    }
                    else
                    {
                        textBox_idcard_type.Text = idCardType;
                    }
                }
                else
                {
                    if (textBox_idcard_type.InvokeRequired)
                    {
                        textBox_idcard_type.Invoke(new MethodInvoker(delegate { textBox_idcard_type.Text = ""; }));
                    }
                    else
                    {
                        textBox_idcard_type.Text = "";
                    }
                }
                if (ocrResult.name != null)
                {
                    if (textBox_name.InvokeRequired)
                    {
                        textBox_name.Invoke(new MethodInvoker(delegate { textBox_name.Text = ocrResult.name; }));
                    }
                    else
                    {
                        textBox_name.Text = ocrResult.name;
                    }
                }
                else
                {
                    if (textBox_name.InvokeRequired)
                    {
                        textBox_name.Invoke(new MethodInvoker(delegate { textBox_name.Text = ""; }));
                    }
                    else
                    {
                        textBox_name.Text = "";
                    }
                }
                if (ocrResult.regnum != null)
                {
                    try
                    {
                        string[] regnumArr = ocrResult.regnum.Split('-');

                        if (regnumArr.Length > 0)
                        {
                            if (textBox_regnum.InvokeRequired)
                            {
                                textBox_regnum.Invoke(new MethodInvoker(delegate { textBox_regnum.Text = regnumArr[0]; }));
                            }
                            else
                            {
                                textBox_regnum.Text = regnumArr[0];
                            }

                        }
                        if (regnumArr.Length > 1)
                        {
                            if (textBox_regnum2.InvokeRequired)
                            {
                                textBox_regnum2.Invoke(new MethodInvoker(delegate { textBox_regnum2.Text = regnumArr[1]; }));
                            }
                            else
                            {
                                textBox_regnum2.Text = regnumArr[1];
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    if (textBox_regnum.InvokeRequired)
                    {
                        textBox_regnum.Invoke(new MethodInvoker(delegate { textBox_regnum.Text = ""; }));
                    }
                    else
                    {
                        textBox_regnum.Text = "";
                    }
                    if (textBox_regnum2.InvokeRequired)
                    {
                        textBox_regnum2.Invoke(new MethodInvoker(delegate { textBox_regnum2.Text = ""; }));
                    }
                    else
                    {
                        textBox_regnum2.Text = "";
                    }

                }
                if (ocrResult.addr != null)
                {
                    if (richTextBox_addr.InvokeRequired)
                    {
                        richTextBox_addr.Invoke(new MethodInvoker(delegate { richTextBox_addr.Text = ocrResult.addr; }));
                    }
                    else
                    {
                        richTextBox_addr.Text = ocrResult.addr;
                    }
                }
                else
                {
                    if (richTextBox_addr.InvokeRequired)
                    {
                        richTextBox_addr.Invoke(new MethodInvoker(delegate { richTextBox_addr.Text = ""; }));
                    }
                    else
                    {
                        richTextBox_addr.Text = "";
                    }
                }
                if (ocrResult.issue_date != null)
                {
                    try
                    {
                        string[] issueDateArr = ocrResult.issue_date.Split('-');
                        if (issueDateArr != null)
                        {
                            if (textBox_issue_date_yyyy.InvokeRequired)
                            {
                                textBox_issue_date_yyyy.Invoke(new MethodInvoker(delegate { textBox_issue_date_yyyy.Text = issueDateArr[0]; }));
                            }
                            else
                            {
                                textBox_issue_date_yyyy.Text = issueDateArr[0];
                            }
                        }
                        if (issueDateArr.Length > 1)
                        {
                            if (textBox_issue_date_MM.InvokeRequired)
                            {
                                textBox_issue_date_MM.Invoke(new MethodInvoker(delegate { textBox_issue_date_MM.Text = issueDateArr[1]; }));
                            }
                            else
                            {
                                textBox_issue_date_MM.Text = issueDateArr[1];
                            }

                        }
                        if (issueDateArr.Length > 2)
                        {
                            if (textBox_issue_date_dd.InvokeRequired)
                            {
                                textBox_issue_date_dd.Invoke(new MethodInvoker(delegate { textBox_issue_date_dd.Text = issueDateArr[2]; }));
                            }
                            else
                            {
                                textBox_issue_date_dd.Text = issueDateArr[2];
                            }
                        }

                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    if (textBox_issue_date_yyyy.InvokeRequired)
                    {
                        textBox_issue_date_yyyy.Invoke(new MethodInvoker(delegate { textBox_issue_date_yyyy.Text = ""; }));
                    }
                    else
                    {
                        textBox_issue_date_yyyy.Text = "";
                    }
                    if (textBox_issue_date_MM.InvokeRequired)
                    {
                        textBox_issue_date_MM.Invoke(new MethodInvoker(delegate { textBox_issue_date_MM.Text = ""; }));
                    }
                    else
                    {
                        textBox_issue_date_MM.Text = "";
                    }
                    if (textBox_issue_date_dd.InvokeRequired)
                    {
                        textBox_issue_date_dd.Invoke(new MethodInvoker(delegate { textBox_issue_date_dd.Text = ""; }));
                    }
                    else
                    {
                        textBox_issue_date_dd.Text = "";
                    }
                }

                if (ocrResult.qrResult != null)
                {
                    if (textBox_qr.InvokeRequired)
                    {
                        textBox_qr.Invoke(new MethodInvoker(delegate { textBox_qr.Text = ocrResult.qrResult; }));
                    }
                    else
                    {
                        textBox_qr.Text = ocrResult.name;
                    }
                }
                else
                {
                    if (textBox_name.InvokeRequired)
                    {
                        textBox_qr.Invoke(new MethodInvoker(delegate { textBox_qr.Text = ""; }));
                    }
                    else
                    {
                        textBox_qr.Text = "";
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        private void button_photo_Click(object sender, EventArgs e)
        {
            OcrResult ocrResult = new OcrResult();
            OCRInfo oCRInfo = new OCRInfo();
            try
            {
                switch (_ocrCamera.IRMode)
                {
                    case "off":
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                    case "on":
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);

                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);

                        break;
                    case "both":
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        oCRInfo.IrImgBase64= _ocrCamera.imageBase64;
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                    case "dev":
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        break;
                    default:
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                }
            }
            catch (Exception)
            {
            }
            _ocrCamera.ocrResult = ocrResult;
            _ocrCamera.OcrResultUpdated();
        }

        private void button_fingerprint_Click(object sender, EventArgs e)
        {
            
            Bitmap finger = _serialService.ScanFingerSFM();
            _ocrCamera.fingerBitmap = finger;
        }

        private void button_record_Click(object sender, EventArgs e)
        {
            try
            {
                if (isNowRec)
                {
                    Console.WriteLine("rec stop");
                    waveIn.StopRecording();
                    isNowRec = false;
                    SetImageOnButton(button_record, Properties.Resources.mic_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
                    button_record.Text = "녹음";


                }
                else
                {
                    Console.WriteLine("rec start");
                    DateTime now = DateTime.Now;
                    string recodeFileName = Path.Combine(_ocrCamera.ResultPath, now.ToString("yyyy-MM-dd_HH-mm-dd.wav"));
                    Console.WriteLine(recodeFileName);
                    waveIn = _audioDevice.RecordStart(recodeFileName);
                    isNowRec = true;
                    SetImageOnButton(button_record, Properties.Resources.Stop_Normal_Red_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
                    button_record.Text = "녹음종료";
                }
            }
            catch (Exception)
            {

            }
        }

        public void SetImageOnButton(Button button, Bitmap image, int width, int heidth)
        {
            Bitmap resizedImage = new Bitmap(width, heidth);
            using(Graphics g = Graphics.FromImage(resizedImage))
            {
                g.DrawImage(image, new RectangleF(0, 0, width, heidth), new RectangleF(new PointF(0, 0), image.Size), GraphicsUnit.Pixel);
            }
            button.Image = resizedImage;
            
        }

        private void button_reset_form_Click(object sender, EventArgs e)
        {
            _ocrCamera.ocrResult = new OcrResult();
            _ocrCamera.OcrResultUpdated();
            richTextBox_addr.Text = "";
            /*foreach(Control c in this.Controls)
            {
                TextBox tb = c as TextBox;
                if(null != tb)
                {
                    tb.Text = "";
                }
            }*/
            _ocrCamera.finger_img = null;
            _ocrCamera.fingerBitmap = null;
            
        }

        private void button_shutdown_Click(object sender, EventArgs e)
        {
           //System.Environment.Exit(0);
            this.Close();
        }

        private void DemoUI_Load(object sender, EventArgs e)
        {

        }
    }
}
