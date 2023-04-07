using CD1HW.Grpc;
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
        private readonly IServiceProvider _serviceProvider;

        public NotifyIconForm(Cv2Camera cv2Camera, OcrCamera ocrCamera, IdScanRpcClient idScanRpcClient, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _cv2Camera = cv2Camera;
            _ocrCamera = ocrCamera;
            _serviceProvider = serviceProvider;

            if (_ocrCamera.DemoUIOnStart)
            {
                //demoUI = new DemoUI(ocrCamera, idScanRpcClient);
                demoUI = _serviceProvider.GetRequiredService<DemoUI>();
                demoUI.Show();
            }

            switch (_cv2Camera._camIdx)
            {
                case 0:
                    sel_cam_0.Checked = true;
                    break;
                case 1:
                    sel_cam_1.Checked = true;
                    break;
                case 2:
                    sel_cam_2.Checked = true;
                    break;
                default:
                    break;
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
                //demoUI = new DemoUI(_ocrCamera, _idScanRpcClient);
                demoUI = _serviceProvider.GetRequiredService<DemoUI>();
                demoUI.Show();
            }
        }

        private void sel_cam_0_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cv2Camera._camIdx = 0;
            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
            sel_cam_0.Checked = true;
            sel_cam_1.Checked = false;
            sel_cam_2.Checked = false;
        }

        private void sel_cam_1_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cv2Camera._camIdx = 1;
            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
            sel_cam_0.Checked = false;
            sel_cam_1.Checked = true;
            sel_cam_2.Checked = false;
        }

        private void sel_cam_2_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cv2Camera._camIdx = 2;
            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
            sel_cam_0.Checked = false;
            sel_cam_1.Checked = false;
            sel_cam_2.Checked = true;
        }

        private void dSSHOWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cv2Camera._cameraBackEnd = VideoCaptureAPIs.DSHOW;
        }

        private void mSMFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cv2Camera._cameraBackEnd = VideoCaptureAPIs.MSMF;
        }
    }
}
