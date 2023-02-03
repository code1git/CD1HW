using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using SharpDX.MediaFoundation;
using Microsoft.Extensions.Logging;
using CD1HW.Grpc;
using WebSocketsSample.Controllers;

namespace CD1HW.Hardware
{
    public sealed class Cv2Camera
    {
        private readonly ILogger<Cv2Camera> _logger;
        private readonly OcrCamera _appSettings;
        public Cv2Camera(ILogger<Cv2Camera> logger, OcrCamera appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }
        //private static readonly Lazy<Cv2Camera> _insteance = new Lazy<Cv2Camera>(() => new Cv2Camera());
        //public static Cv2Camera Instance { get { return _insteance.Value; } }
        private static Thread cameraThread;
        private VideoCapture capture;

        private void CaptureCameraCallback()
        {
            //_logger.LogDebug("#########");
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
            //AppSettings appSettings = AppSettings.Instance;
            int camIdx = 0;
            Mat frame = new Mat();
            capture = new VideoCapture();
            while (true)
            {
                try
                {
                    
                    try
                    {
                        lock(capture)
                        {
                            if (capture==null || !capture.IsOpened())
                            {
                                Console.WriteLine("try open camera");
                                camIdx = _appSettings.CamIdx;
                                Console.WriteLine(camIdx);
                                capture.Open(camIdx, _appSettings.CameraBackEnd);
                                capture.FrameWidth = 1920;
                                capture.FrameHeight = 1440;
                                capture.Fps = 30;
                            }
                            capture.Read(frame);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    if (frame.Size().Width != 0 && frame.Size().Height != 0)
                    {
                        try
                        {
                            //Console.WriteLine(frame.Size());
                            Mat src = new Mat();
                            if (_appSettings.camera_rotate != 0)
                            {
                                Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(frame.Width / 2, frame.Height / 2), _appSettings.camera_rotate, 1.0);
                                Cv2.WarpAffine(frame, src, matrix, new OpenCvSharp.Size(frame.Width, frame.Height));
                            }
                            if (_appSettings.camera_crop)
                            {
                                float cropLeft = src.Width * 0.2f;
                                float cropRight = src.Width * 0.2f;
                                float cropTop = src.Height * 0.1f;
                                float cropBotom = src.Height * 0.1f;
                                Rect cropRectangle = new Rect((int) cropLeft, (int) cropTop, (int)(src.Width - cropLeft - cropRight), (int) (src.Height - cropTop - cropBotom));
                                src = src.SubMat(cropRectangle);
                            }
                            Bitmap bitmapImage = BitmapConverter.ToBitmap(src);

                            MemoryStream memoryStream = new MemoryStream();
                            bitmapImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] imgBuf = memoryStream.ToArray();
                            
                            
                            _appSettings.imgBase64Str = Convert.ToBase64String(imgBuf);
                            _appSettings.cameraBitmap = bitmapImage;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
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
                Console.WriteLine("camera closing");
                if(capture.IsEnabledDispose)
                {
                    capture.Dispose();
                    capture = new VideoCapture();
                    Console.WriteLine("camera closed");
                }
                else
                {
                    Console.WriteLine("camera close fail");
                }
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
