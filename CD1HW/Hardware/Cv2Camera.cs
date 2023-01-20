using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using SharpDX.MediaFoundation;
using Microsoft.Extensions.Logging;
using CD1HW.Grpc;

namespace CD1HW.Hardware
{
    public sealed class Cv2Camera
    {
        private static readonly Lazy<Cv2Camera> _insteance = new Lazy<Cv2Camera>(() => new Cv2Camera());
        public static Cv2Camera Instance { get { return _insteance.Value; } }
        private static Thread cameraThread;
        private VideoCapture capture;

        private void CaptureCameraCallback()
        {

            /*string[] supportCamList = { "OSID-SQ100", "OSID-100", "OSID-100", "SF5A136", "ODI-100", "WebCam SCB-0350M" };
            string[] camList = ListOfAttachedCameras();

            for (int i = 0; i < camList.Length; i++)
            {
                string camName = camList[i];
                if (supportCamList.Contains(camName))
                {
                    camIdx = i;
                    Console.WriteLine("camera : " + camName);
                    break;
                }
            }*/
            AppSettings appSettings = AppSettings.Instance;
            int camIdx = 0;
            capture = new VideoCapture();
            Mat frame = new Mat();
            while (true)
            {
                try
                {
                    
                    try
                    {
                        lock (capture)
                        {

                            if (!capture.IsOpened())
                            {
                                lock(appSettings)
                                {
                                    camIdx = appSettings.camIdx;
                                }
                                capture.Open(camIdx, VideoCaptureAPIs.ANY);
                                capture.FrameWidth = 1920;
                                capture.FrameHeight = 1440;
                                capture.Fps = 30;
                            }
                            capture.Read(frame);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                    if (frame.Size().Width != 0 && frame.Size().Height != 0)
                    {
                        try
                        {
                            Mat dst = new Mat();
                            Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(frame.Width / 2, frame.Height / 2), 180.0, 1.0);
                            Cv2.WarpAffine(frame, dst, matrix, new OpenCvSharp.Size(frame.Width, frame.Height));
                            Bitmap bitmapImage = BitmapConverter.ToBitmap(dst);
                            MemoryStream memoryStream = new MemoryStream();
                            bitmapImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] imgBuf = memoryStream.ToArray();
                            
                            lock (appSettings)
                            {
                                appSettings.imgBase64Str = Convert.ToBase64String(imgBuf);
                                appSettings.cameraBitmap = bitmapImage;
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }

                }
                catch (Exception e)
                {
                }
            }
        }

        public void CameraStart()
        {
            cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            cameraThread.Start();
        }

        public void ResetCamera()
        {
            lock (capture)
            {
                Console.WriteLine("camera close");
                capture.Release();
            }
        }

        public string GetCameraBackendName()
        {
            return capture.GetBackendName();
        }

/*        public byte[] GetImgBuf()
        {
            return imgBuf;
        }*/

        /*public int GetCameraIndexForPartName(string partName)
        {
            var cameras = ListOfAttachedCameras();
            for (var i = 0; i < cameras.Count(); i++)
            {
                if (cameras[i].ToLower().Contains(partName.ToLower()))
                {
                    return i;
                }
            }
            return -1;
        }
        public string[] ListOfAttachedCameras()
        {
            var cameras = new List<string>();
            var attributes = new MediaAttributes(1);
            attributes.Set(CaptureDeviceAttributeKeys.SourceType.Guid, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);
            var devices = MediaFactory.EnumDeviceSources(attributes);
            
            for (var i = 0; i < devices.Count(); i++)
            {
                var friendlyName = devices[i].Get(CaptureDeviceAttributeKeys.FriendlyName);
                cameras.Add(friendlyName);
            }
            return cameras.ToArray();
        }*/

    }
}
