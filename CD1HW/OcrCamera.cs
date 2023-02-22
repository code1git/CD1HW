using CD1HW.Controllers;
using CD1HW.Hardware;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using System.Configuration;
using Windows.ApplicationModel.Store;

namespace CD1HW
{
    /* 각종 global 변수의 저장에 사용
     * 유지/보수의 편의상 camera/image에 관한 변수 만이 아닌 프로그램내 전반에 사용되는 변수들을 몰아서 저장중
     * 추후 유지/보수의 편의상의 의미가 적어지거나 성능상의 issue를 보일정도로 커진다면 분리하는것이 좋을 것으로 보임
     */
    public class OcrCamera
    {
        private readonly AppSettings? _options;
        private readonly ILogger<OcrCamera> _logger;
        public OcrCamera(ILogger<OcrCamera> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            try
            {
                // get opctions from appsettings.json
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
                SignPadFont = _options.SignPadFont;
                ResultPath = _options.ResultPath;
            }
            catch (Exception)
            {
                _logger.LogError("exception from get opction from appsettings.json");
            }
            
        }
        // application.json으로 부터 읽은 초기세팅
        // 제품의 타입 (추후 버전의 분화없이 flag로 분기하기 위해)
        public string ProductType { get; set; }
        // 실행시 UI실행
        public bool DemoUIOnStart { get; set; }
        // camera index
        public int CamIdx { get; set; }
        // camera capture의 backend (windows :  dshow권장)
        public VideoCaptureAPIs CameraBackEnd { get; set; } = VideoCaptureAPIs.DSHOW;
        // 현재의 camera image를 base 64 str로 변환한 데이터 (데이터 전송에 사용)
        public string imgBase64Str { get; set; }
        // 현재의 camera image의 bitmap
        public Bitmap cameraBitmap { get; set; }
        public bool camera_crop = true;
        public int camera_rotate = 180;
        
        // 수동촬영, 수동촬영시의 check data
        public int manual_flag { get; set; } = 0;
        public int manual_time_chk_flag { get; set; } = 0;
        public long manual_st_time_mill { get; set; }
        public long manual_ed_time_mill { get; set; }
        
        // 서명패드 display의 글씨체 지정
        public string SignPadFont { get; set; } = "굴림체";
        // 결과 저장되는 임시 folder의 path
        public string ResultPath { get; set; }

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

        // 결과 log text file에 append
        public void SaveResult()
        {
            try
            {
                string resultTxt =  ResultPath + @"\.ocr_result.csv";
                string regnumIdx;
                if (regnum != null && !regnum.Equals(""))
                    regnumIdx = regnum;
                else if (birth != null && !birth.Equals(""))
                    regnumIdx = birth;
                else
                    regnumIdx = "none";
                string maskingImgFilePath = ResultPath + "/" +name + "_" + regnumIdx + "_masking.jpg";
                using (StreamWriter sw = new StreamWriter(resultTxt, true))
                {
                    string tmpSex = "";
                    if (sex.Equals("1"))
                        tmpSex = "남";
                    else if (sex.Equals("2"))
                        tmpSex = "여";
                    sw.WriteLine(System.DateTime.Now+ "," + name+ "," + regnumIdx + "," + tmpSex + "," + maskingImgFilePath);
                }
                if (masking_img != null)
                {

                    using (FileStream fs = new FileStream(maskingImgFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] bData = Convert.FromBase64String(masking_img);
                        fs.Write(bData);
                    }
                }
                else
                {
                    _logger.LogInformation("there is no masking image");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public void SaveSignImg(string padName, string padBirth)
        {
            try
            {
                string signImgFilePath = ResultPath + "/" + padName + "_" + padBirth + "_sign.jpg";
                if (sign_img!=null)
                {
                    using (FileStream fs = new FileStream(signImgFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] bData = Convert.FromBase64String(sign_img);
                        fs.Write(bData);
                    }
                }
                else
                {
                    _logger.LogInformation("there is no sign image");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

        }

        public void SaveFingerImg(string padName, string padBirth)
        {
            try
            {
                string fingerImgFilePath = ResultPath + "/" + padName + "_" + padBirth + "_finger.bmp";
                if (finger_img != null)
                {
                    using (FileStream fs = new FileStream(fingerImgFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] bData = Convert.FromBase64String(finger_img);
                        fs.Write(bData);
                    }
                }
                else
                {
                    _logger.LogInformation("there is no finger image");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

        }

    }
}
