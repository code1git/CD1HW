using System.IO;
using CD1HW.Hardware;
using CD1HW.Model;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using OpenCvSharp;

namespace CD1HW.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HwController : ControllerBase
    {
        private readonly ILogger<HwController> _logger;
        public HwController(ILogger<HwController> logger)
        {
            _logger = logger;
        }
        
        [HttpGet("/nametest")]
        public string Test(string name)
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.name = name;
            return  "ttt";
        }

        [HttpGet("/camera_info")]
        public string GetCameraBackendName()
        {
            Cv2Camera cv2Camera = Cv2Camera.Instance;
            return cv2Camera.GetCameraBackendName();
        }

        [HttpGet("/camera_reset")]
        public string ResetCamera()
        {
            Cv2Camera cv2Camera = Cv2Camera.Instance;
            lock(cv2Camera)
            {
                cv2Camera.ResetCamera();
            }
            return "camera reset";
        }

        [HttpGet("/ocr_reset")]
        public string OcrReset()
        {
            AppSettings appSettings = AppSettings.Instance;
            appSettings.id_card_type = null;
            appSettings.name = null;
            appSettings.addr = null;
            appSettings.birth = null;

            appSettings.birth_img = null;
            appSettings.face_img = null;
            appSettings.name_img = null;
            appSettings.regnum_img = null;
            return "ocr reset";
        }

        [HttpPost("/ocr_result")]
        public string OcrResult(OcrMsg ocrMsg)
        {
            OcrResult ocrResult = ocrMsg.ocr_result;
            ResultImg resultImg = ocrMsg.result_image;
            AppSettings appSettings = AppSettings.Instance;
            lock (appSettings)
            {
                if (ocrResult != null)
                {
                    appSettings.name = ocrResult.name;
                    appSettings.addr = ocrResult.addr;
                    appSettings.birth = ocrResult.birth;
                    appSettings.id_card_type = ocrResult.id_card_type;
                    appSettings.issue_date = ocrResult.issue_date;

                }

                if (resultImg != null)
                {
                    appSettings.birth_img = resultImg.birth_img;
                    appSettings.face_img = resultImg.face_img;
                    appSettings.name_img = resultImg.name_img;
                    appSettings.regnum_img = resultImg.regnum_img;

                }

            }
            return "ocr reset";
        }

        [HttpPost("/call_finger")]
        public string callFinger(OcrResult ocrMsg)
        {
            AppSettings appSettings = AppSettings.Instance;
            byte[] fingerScanImg = null;
            FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
            Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
            fingerprintThread.Start();
            while (fingerScanImg == null)
            {
                // wait any one end
            }
            appSettings.finger_img = Convert.ToBase64String(fingerScanImg);
            return appSettings.finger_img;
        }

        [HttpPost("/call_pad")]
        public string callPad(OcrResult ocrMsg)
        {
            AppSettings appSettings = AppSettings.Instance;
            string TEMP_DIR = ".\\temp\\";
            if (!Directory.Exists(TEMP_DIR))
                Directory.CreateDirectory(TEMP_DIR);
            string filename = Path.GetRandomFileName();
            string signPadImgPath = Path.Combine(TEMP_DIR, filename);
            byte[] fingerScanImg = null;
            FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
            Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
            fingerprintThread.Start();

            //Signpad.CallSignPadEvent(ocrMsg.name, ocrMsg.birth, ocrMsg.addr, signPadImgPath, fingerprintThread);

            while (fingerScanImg == null && !System.IO.File.Exists(Path.Combine(TEMP_DIR, filename)))
            {
                // wait any one end
            }
            try
            {
                //
                // sign pad를 우선처리 (지문 터치센서의 오인식 가능성이 있음으로)
                if (System.IO.File.Exists(signPadImgPath))
                {
                    Thread.Sleep(500);

                    using (FileStream fs = new FileStream(signPadImgPath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] signPadImg = new byte[fs.Length];
                        fs.Read(signPadImg, 0, signPadImg.Length);
                        fs.Close();
                        appSettings.sign_img = Convert.ToBase64String(signPadImg);
                    }
                }
                // 지문이미지 처리
                else if (fingerScanImg != null)
                {
                    appSettings.finger_img = Convert.ToBase64String(fingerScanImg);
                }
            }
            catch (Exception)
            {
            }
            return "singpad / fingerprint scanner";
        }
    }
}
