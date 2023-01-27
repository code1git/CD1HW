using CD1HW.Hardware;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CD1HW.WinFormUi
{
    public partial class NotifyIconForm : Form
    {
        private DemoUI demoUI;
        public NotifyIconForm()
        {
            InitializeComponent();
            demoUI = new DemoUI();
            //demoUI.TopLevel = false;
            //this.Controls.Add(demoUI);
            //demoUI.ControlBox = false;
            //demoUI.ShowInTaskbar= true;
            //demoUI.Opacity= 100;
            demoUI.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(demoUI == null || demoUI.IsDisposed)
            {
                demoUI = new DemoUI();
                demoUI.Show();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.camIdx = 0;
            Cv2Camera cv2Camera = Cv2Camera.Instance;
            lock(cv2Camera)
            {
                cv2Camera.ResetCamera();
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.camIdx = 1;
            Cv2Camera cv2Camera = Cv2Camera.Instance;
            lock (cv2Camera)
            {
                cv2Camera.ResetCamera();
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.camIdx = 2;
            Cv2Camera cv2Camera = Cv2Camera.Instance;
            lock (cv2Camera)
            {
                cv2Camera.ResetCamera();
            }
        }

        private void dSSHOWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.cameraBackEnd = VideoCaptureAPIs.DSHOW;
        }

        private void mSMFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.cameraBackEnd = VideoCaptureAPIs.MSMF;
        }
    }
}
