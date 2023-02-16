using CD1HW.Hardware;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
        private readonly Cv2Camera _cv2Camera;
        private readonly OcrCamera _ocrCamera;
        public NotifyIconForm(Cv2Camera cv2Camera, OcrCamera ocrCamera)
        {
            InitializeComponent();
            _cv2Camera = cv2Camera;
            _ocrCamera = ocrCamera;

            if (_ocrCamera.DemoUIOnStart)
            {
                demoUI = new DemoUI(ocrCamera);
                demoUI.Show();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (_cv2Camera)
            {
            System.Environment.Exit(0);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (demoUI == null || demoUI.IsDisposed)
            {
                demoUI = new DemoUI(_ocrCamera);
                demoUI.Show();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            _ocrCamera.CamIdx = 0;
            lock(_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            _ocrCamera.CamIdx = 1;
            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            _ocrCamera.CamIdx = 2;
            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
        }

        private void dSSHOWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ocrCamera.CameraBackEnd = VideoCaptureAPIs.DSHOW;
        }

        private void mSMFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ocrCamera.CameraBackEnd = VideoCaptureAPIs.MSMF;
        }
    }
}
