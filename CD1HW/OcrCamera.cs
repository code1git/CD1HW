using CD1HW.Hardware;
using CD1HW.Model;
using Microsoft.Extensions.Options;

namespace CD1HW
{
    /* 각종 global 변수의 저장에 사용
     */
    public class OcrCamera
    {
        private readonly Appsettings? _options;
        private readonly ILogger<OcrCamera> _logger;
        
        public OcrCamera(ILogger<OcrCamera> logger, IOptions<Appsettings> options)
        {
            _logger = logger;
            try
            {
                // get opctions from appsettings.json
                _options = options.Value;
                
                ProductType = _options.ProductType;
                DemoUIOnStart = _options.DemoUIOnStart;
                SignPadFont = _options.SignPadFont;
                ResultPath = _options.ResultPath;
                IRMode = _options.IRMode;
                IRTimesleep = _options.IRTimesleep;
            }
            catch (Exception)
            {
                _logger.LogError("exception from get opction from appsettings.json");
            }
            
        }
        // application.json으로 부터 읽은 초기세팅
        /* 제품의 타입
         * OSID 오성
         * NEC 선관위 데모용
         */
        public string ProductType { get; set; }
        // 실행시 UI실행
        public bool DemoUIOnStart { get; set; }
        public string imageBase64 { get; set; }
        // 현재의 camera image의 bitmap
        public Bitmap cameraBitmap { get; set; }
        // IR mode IR ramp on, off, both
        public string IRMode { get; set; }
        public int IRTimesleep { get; set; }
        // 서명패드 display의 글씨체 지정
        public string SignPadFont { get; set; } = "굴림체";
        // 결과 저장되는 임시 folder의 path
        public string ResultPath { get; set; }
        //ocr 결과
        public Model.OcrResult ocrResult { get; set; } = new Model.OcrResult();
       
        //singpad fingerprint scanner
        public string sign_img { get; set; }
        public Bitmap fingerBitmap { get; set; }
        public string finger_img { get; set; }

        public event EventHandler<OcrResult> OcrResultUpdate;
        
        public void OcrResultUpdated()
        {
            if(this.OcrResultUpdate != null)
            {
                OcrResultUpdate(this, this.ocrResult);
            }
        }

        // 결과 log text file에 append
        public void SaveResult()
        {
            try
            {
                string resultTxt =  ResultPath + @"\.ocr_result.csv";
                string regnumIdx;
                if (ocrResult.regnum != null && !ocrResult.regnum.Equals(""))
                    regnumIdx = ocrResult.regnum;
                else if (ocrResult.birth != null && !ocrResult.birth.Equals(""))
                    regnumIdx = ocrResult.birth;
                else
                    regnumIdx = "none";
                string maskingImgFilePath = ResultPath + "/" + ocrResult.name + "_" + regnumIdx + "_masking.jpg";
                using (StreamWriter sw = new StreamWriter(resultTxt, true))
                {
                    string tmpSex = "";
                    if (ocrResult.sex.Equals("1"))
                        tmpSex = "남";
                    else if (ocrResult.sex.Equals("2"))
                        tmpSex = "여";
                    sw.WriteLine(System.DateTime.Now+ "," + ocrResult.name + "," + regnumIdx + "," + tmpSex + "," + maskingImgFilePath);
                }
                if (ocrResult.masking_img != null)
                {

                    using (FileStream fs = new FileStream(maskingImgFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] bData = Convert.FromBase64String(ocrResult.masking_img);
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
