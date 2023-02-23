using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using SharpDX.MediaFoundation;
using Microsoft.Extensions.Logging;
using CD1HW.Grpc;
using WebSocketsSample.Controllers;


namespace CD1HW.Hardware
{

    /// <summary>
    /// open cv를 활용한 카메라 컨틀롤
    /// 의존성 주입시 singletone 선언 하여 사용할것
    /// </summary>
    public sealed class Cv2Camera
    {
        private readonly ILogger<Cv2Camera> _logger;
        private readonly OcrCamera _ocrCamera;
        public Cv2Camera(ILogger<Cv2Camera> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
        }

        /// <summary>
        /// di를 적용하지않고 singletone 구현을 하기위한 코드 -> 현재 사용하지않음
        /// </summary>
        //private static readonly Lazy<Cv2Camera> _insteance = new Lazy<Cv2Camera>(() => new Cv2Camera());
        //public static Cv2Camera Instance { get { return _insteance.Value; } }
        
        private static Thread cameraThread;
        private VideoCapture capture;

        /// <summary>
        /// 카메라 캡쳐된 이미지를 계속해서 뿌려주기 위한 Callback함수
        /// </summary>
        private void CaptureCameraCallback()
        {
            // 카메라를 이름으로 지정하는 로직. .net framwork에서 .net core로 변경시 라이브러리 비호환으로 비활성화
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

            // camera index
            int camIdx = 0;
            // 현재의 카메라 프레임
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
                            // 카메라 open (열려있지 않을 시)
                            if (capture==null || !capture.IsOpened())
                            {
                                camIdx = _ocrCamera.CamIdx;
                                _logger.LogInformation("try open camera : " + _ocrCamera.CameraBackEnd + " " + camIdx );
                                capture.Open(camIdx, _ocrCamera.CameraBackEnd);
                                capture.FrameWidth = 1920;
                                capture.FrameHeight = 1440;
                                capture.Fps = 30;
                            }
                            //frame read
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
                            // read한 frame을 가공/저장
                            Mat src = new Mat();
                            //선관위 camera rotate (데모기 사양상 카메라 역방향)
                            if (_ocrCamera.camera_rotate != 0)
                            {
                                Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(frame.Width / 2, frame.Height / 2), _ocrCamera.camera_rotate, 1.0);
                                Cv2.WarpAffine(frame, src, matrix, new OpenCvSharp.Size(frame.Width, frame.Height));
                            }
                            // 화면 크롭
                            if (_ocrCamera.camera_crop)
                            {
                                //선관위용
                                if(src.Width > 1900 && src.Height > 1050)
                                {
                                    float cropLeft = 300f;
                                    float cropTop = 80f;
                                    float cropRight = 1600f;
                                    float cropBotom = 1020f;
                                    Rect cropRectangle = new Rect((int) cropLeft, (int) cropTop, (int)(cropRight-cropLeft), (int) (cropBotom-cropTop));
                                    src = src.SubMat(cropRectangle);
                                }
                                // 오성 데모기기용
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
                            
                            // OcrCamera에 frame저장
                            
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

        /// <summary>
        /// 카메라 Thread 시작
        /// </summary>
        public void CameraStart()
        {
            cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            cameraThread.Start();
        }

        /// <summary>
        /// 카메라 리셋 (OcrCamera.camIdx)를 변경한후 리세하면 카메라의 변경 (카메라 Thread에서 카메라를 다시시작)
        /// </summary>
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
