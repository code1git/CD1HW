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

namespace CD1HW.WinFormUi
{
    public partial class DemoUI : Form
    {
        public static DemoUI demoUI;
        private static Bitmap bitmapImage;
        private Thread fingerprintThread;
        private WaveInEvent waveIn;
        private bool isNowRec = false;
        private int BUTTON_IMAGE_WIDTH = 50;
        private int BUTTON_IMAGE_HEIGHT = 50;

        private void upadateCameraFrame()
        {
            Thread cameraThread = new Thread(new ThreadStart(upadateCameraFrameTherad));
            cameraThread.Start();
        }
        private void upadateCameraFrameTherad()
        {
            while (true)
            {
                try
                {
                    AppSettings appSettings = AppSettings.Instance;
                    Bitmap cameraBitmap = appSettings.cameraBitmap;
                    if (cameraBitmap != null)
                    {
                        if (camera_image1.InvokeRequired)
                        {
                            //camera_image1.Invoke(new MethodInvoker(delegate { camera_image1.Image = bitmapImage; }));
                            camera_image1.Invoke(new MethodInvoker(delegate { camera_image1.Image = cameraBitmap; }));
                        }
                        else
                        {
                            //camera_image1.Image = bitmapImage;
                            camera_image1.Image = cameraBitmap;
                        }
                        
 
                    }
                    else
                    {
                        //init camera image

                    }
                    if (appSettings.id_card_type != null)
                    {
                        string idCardType = "";
                        switch (appSettings.id_card_type)
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
                            textBox_idcard_type.Invoke(new MethodInvoker(delegate { textBox_name.Text = idCardType; }));
                        }
                        else
                        {
                            textBox_idcard_type.Text = idCardType;
                        }
                    }
                    if (appSettings.name != null)
                    {
                        if (textBox_name.InvokeRequired)
                        {
                            textBox_name.Invoke(new MethodInvoker(delegate { textBox_name.Text = appSettings.name; }));
                        }
                        else
                        {
                            textBox_name.Text = appSettings.name;
                        }
                    }
                    if (appSettings.regnum != null)
                    {
                        try
                        {
                            string[] regnumArr = appSettings.regnum.Split('-');
                            if (textBox_regnum.InvokeRequired)
                            {
                                textBox_regnum.Invoke(new MethodInvoker(delegate { textBox_regnum.Text = regnumArr[0]; }));
                            }
                            else
                            {
                                textBox_regnum.Text = regnumArr[0];
                            }
                            if (textBox_regnum2.InvokeRequired)
                            {
                                textBox_regnum2.Invoke(new MethodInvoker(delegate { textBox_regnum.Text = regnumArr[1]; }));
                            }
                            else
                            {
                                textBox_regnum2.Text = regnumArr[1];
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (appSettings.addr != null)
                    {
                        if (richTextBox_addr.InvokeRequired)
                        {
                            richTextBox_addr.Invoke(new MethodInvoker(delegate { textBox_name.Text = appSettings.addr; }));
                        }
                        else
                        {
                            richTextBox_addr.Text = appSettings.addr;
                        }
                    }
                    if (appSettings.issue_date != null)
                    {
                        try
                        {
                            string[] issueDateArr = appSettings.issue_date.Split('-');
                            if (textBox_issue_date_yyyy.InvokeRequired)
                            {
                                textBox_issue_date_yyyy.Invoke(new MethodInvoker(delegate { textBox_issue_date_yyyy.Text = issueDateArr[0]; }));
                            }
                            else
                            {
                                textBox_issue_date_yyyy.Text = issueDateArr[0];
                            }
                            if (textBox_issue_date_MM.InvokeRequired)
                            {
                                textBox_issue_date_MM.Invoke(new MethodInvoker(delegate { textBox_issue_date_MM.Text = issueDateArr[1]; }));
                            }
                            else
                            {
                                textBox_issue_date_MM.Text = issueDateArr[1];
                            }
                            if (textBox_issue_date_dd.InvokeRequired)
                            {
                                textBox_issue_date_dd.Invoke(new MethodInvoker(delegate { textBox_issue_date_dd.Text = issueDateArr[2]; }));
                            }
                            else
                            {
                                textBox_issue_date_dd.Text = issueDateArr[2];
                            }

                        }
                        catch (Exception)
                        {
                        }
                    }
                    Thread.Sleep(15);
                    
                }
                catch (Exception)
                {
                }
            }
        }
        public DemoUI()
        {

            InitializeComponent();
            demoUI = this;
            upadateCameraFrame();
            SetImageOnButton(button_photo, Properties.Resources.scan_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
            SetImageOnButton(button_fingerprint, Properties.Resources.User_Interface_Fingerprint_Scan_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
            SetImageOnButton(button_record, Properties.Resources.mic_icon, BUTTON_IMAGE_WIDTH, BUTTON_IMAGE_HEIGHT);
            SetImageOnButton(button_setting, Properties.Resources.gear_icon, button_setting.Width, button_setting.Height);
        }

        private void button_photo_Click(object sender, EventArgs e)
        {

        }

        private void button_fingerprint_Click(object sender, EventArgs e)
        {

        }

        private void button_record_Click(object sender, EventArgs e)
        {

        }

        public void SetImageOnButton(Button button, Image image, int width, int heidth)
        {
            Image resizedImage = new Bitmap(width, heidth);
            using(Graphics g = Graphics.FromImage(resizedImage))
            {
                g.DrawImage(image, new RectangleF(0, 0, width, heidth), new RectangleF(new PointF(0, 0), image.Size), GraphicsUnit.Pixel);
            }
            button.Image = resizedImage;
            
        }

        private void button_reset_form_Click(object sender, EventArgs e)
        {
            richTextBox_addr.Text = "";
            foreach(Control c in this.Controls)
            {
                TextBox tb = c as TextBox;
                if(null != tb)
                {
                    tb.Text = "";
                }
            }
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
