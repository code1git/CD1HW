using CD1HW.Controllers;
using CD1HW.Hardware;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using System.Configuration;
using Windows.ApplicationModel.Store;

namespace CD1HW
{
    public class OcrCamera
    {

        // get opction from appsettings.json
        private readonly AppSettings? _options;
        private readonly ILogger<OcrCamera> _logger;
        public OcrCamera(ILogger<OcrCamera> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            try
            {
                _options = options.Value;
                ProductType = _options.ProductType;
                DemoUIOnStart = _options.DemoUIOnStart;
                CamIdx = _options.CamIdx;
                switch (_options.CameraBackend)
                {
                    case "DSHOW":
                        CameraBackEnd = VideoCaptureAPIs.DSHOW; break;
                    case "MSMF":
                        CameraBackEnd = VideoCaptureAPIs.MSMF;  break;
                    case "WINRT":
                        CameraBackEnd = VideoCaptureAPIs.WINRT; break;
                    default:
                        CameraBackEnd = VideoCaptureAPIs.DSHOW; break;
                }
            }
            catch (Exception)
            {
                _logger.LogError("exception from get opction from appsettings.json");
            }
            
        }
        public string ProductType { get; set; }
        public bool DemoUIOnStart { get; set; }
        public int CamIdx { get; set; }
        public VideoCaptureAPIs CameraBackEnd { get; set; } = VideoCaptureAPIs.DSHOW;
        public string imgBase64Str { get; set; }
        public Bitmap cameraBitmap { get; set; }
        public bool camera_crop = true;
        public int camera_rotate = 180;
        public int manual_flag { get; set; } = 0;

        //ocr 결과
        public string id_card_type { get; set; }
        public string name { get; set; }
        public string regnum { get; set; }
        public string driver_num { get; set; }
        public string birth { get; set; }
        public string issue_date { get; set; }
        public string addr { get; set; }
        public string sex { get; set; }

        //ocr 결과 image crop
        public string name_img { get; set; }
        public string regnum_img { get; set; }
        public string face_img { get; set; }
        public string birth_img { get; set; }

        public string masking_img { get; set; }

        //singpad fingerprint scanner
        public string sign_img { get; set; }
        public string finger_img { get; set; }


    }
}
