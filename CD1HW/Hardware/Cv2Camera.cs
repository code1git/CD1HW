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
        private readonly OcrCamera _ocrCamera;
        public Cv2Camera(ILogger<Cv2Camera> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
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
            int camIdx = 0;
            Mat frame = new Mat();
            capture = new VideoCapture();

            string[] supportCamList = { "OSID-SQ100", "OSID-100", "OSID-100", "SF5A136", "ODI-100", "WebCam SCB-0350M" };

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
                                camIdx = _ocrCamera.CamIdx;
                                _logger.LogInformation("try open camera : " + _ocrCamera.CameraBackEnd + " " + camIdx );
                                capture.Open(camIdx, _ocrCamera.CameraBackEnd);
                                capture.FrameWidth = 1920;
                                capture.FrameHeight = 1440;
                                capture.Fps = 30;
                            }
                            capture.Read(frame);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("camera frame read fail : " + e.Message);
                    }
                    if (frame.Size().Width != 0 && frame.Size().Height != 0)
                    {
                        try
                        {
                            //Console.WriteLine(frame.Size());
                            Mat src = new Mat();
                            if (_ocrCamera.camera_rotate != 0)
                            {
                                Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(frame.Width / 2, frame.Height / 2), _ocrCamera.camera_rotate, 1.0);
                                Cv2.WarpAffine(frame, src, matrix, new OpenCvSharp.Size(frame.Width, frame.Height));
                            }
                            if (_ocrCamera.camera_crop)
                            {
                                //float cropLeft = src.Width * 0.2f;
                                //float cropRight = src.Width * 0.2f;
                                //float cropTop = src.Height * 0.1f;
                                //float cropBotom = src.Height * 0.1f;
                                if(src.Width > 1900 && src.Height > 1050)
                                {
                                    float cropLeft = 300f;
                                    float cropTop = 80f;
                                    float cropRight = 1600f;
                                    float cropBotom = 1020f;
                                    Rect cropRectangle = new Rect((int) cropLeft, (int) cropTop, (int)(cropRight-cropLeft), (int) (cropBotom-cropTop));
                                    src = src.SubMat(cropRectangle);
                                }
                                /*if (src.Width > 1900 && src.Height > 1050)
                                {
                                    float cropLeft = 150f;
                                    float cropTop = 80f;
                                    float cropRight = 1750f;
                                    float cropBotom = 1020f;
                                    Rect cropRectangle = new Rect((int)cropLeft, (int)cropTop, (int)(cropRight - cropLeft), (int)(cropBotom - cropTop));
                                    src = src.SubMat(cropRectangle);
                                }*/
                            }
                            Bitmap bitmapImage = BitmapConverter.ToBitmap(src);

                            MemoryStream memoryStream = new MemoryStream();
                            bitmapImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] imgBuf = memoryStream.ToArray();
                            
                            
                            _ocrCamera.imgBase64Str = Convert.ToBase64String(imgBuf);
                            _ocrCamera.cameraBitmap = bitmapImage;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("decode camera frame fail : " + e.Message);
                        }
                    }

                }
                catch (Exception e)
                {
                    _logger.LogError("camera error!! : " + e.Message);
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
                if(capture.IsEnabledDispose)
                {
                    _logger.LogInformation("try change camera...\ntry closing camera...");
                    capture.Dispose();
                    capture = new VideoCapture();
                    _logger.LogInformation("camera closed");
                }
                else
                {
                    _logger.LogError("camera close fail");
                }
            }
        }

        public string GetCameraBackendName()
        {
            return capture.GetBackendName();
        }


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
