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
                    lock(appSettings)
                    {
                        Bitmap cameraBitmap = appSettings.cameraBitmap;
                        if (cameraBitmap != null)
                        {
                            Console.WriteLine(cameraBitmap.ToString());
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
                    }
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
