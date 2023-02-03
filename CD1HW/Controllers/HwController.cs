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
using Code1HWSvr;

namespace CD1HW.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HwController : ControllerBase
    {
        private readonly ILogger<HwController> _logger;
        private readonly Cv2Camera _cv2Camera;
        private readonly OcrCamera _ocrCamera;
        private readonly NecDemoCsv _necCsv;
        public HwController(ILogger<HwController> logger, Cv2Camera cv2Camera, OcrCamera ocrCamera, NecDemoCsv necCsv)
        {
            Console.WriteLine("aaaa");
            _logger = logger;
            _cv2Camera = cv2Camera;
            //_cv2Camera = Cv2Camera.Instance;
            _ocrCamera = ocrCamera;
            _necCsv = necCsv;
            
            
        }
        
        [HttpGet("/nametest")]
        public string Test(string name)
        {
            //AppSettings appSettings = AppSettings.Instance;
            _ocrCamera.name = name;
            return name;
        }

        [HttpGet("/camera_info")]
        public string GetCameraBackendName()
        {
            return _cv2Camera.GetCameraBackendName();
        }

        [HttpGet("/camera_reset")]
        public string ResetCamera()
        {
            //Cv2Camera cv2Camera = Cv2Camera.Instance;
            lock(_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
            return "camera reset";
        }

        [HttpGet("/ocr_reset")]
        public string OcrReset()
        {
            //AppSettings appSettings = AppSettings.Instance;
            _ocrCamera.id_card_type = "";
            _ocrCamera.name = "";
            _ocrCamera.regnum = "";
            _ocrCamera.addr = "";
            _ocrCamera.birth = "";

            _ocrCamera.birth_img = "";
            _ocrCamera.face_img = "";
            _ocrCamera.name_img = "";
            _ocrCamera.regnum_img = "";
            return "ocr reset";
        }

        [HttpPost("/ocr_result")]
        public string OcrResult(OcrMsg ocrMsg)
        {
            OcrResult ocrResult = ocrMsg.ocr_result;
            ResultImg resultImg = ocrMsg.result_image;

                if (ocrResult != null)
                {
                    _ocrCamera.name = ocrResult.name;
                    _ocrCamera.regnum = ocrResult.regnum;
                    _ocrCamera.addr = ocrResult.addr;
                    _ocrCamera.birth = ocrResult.birth;
                    _ocrCamera.id_card_type = ocrResult.id_card_type;
                    _ocrCamera.issue_date = ocrResult.issue_date;

                }

                if (resultImg != null)
                {
                    _ocrCamera.birth_img = resultImg.birth_img;
                    _ocrCamera.face_img = resultImg.face_img;
                    _ocrCamera.name_img = resultImg.name_img;
                    _ocrCamera.regnum_img = resultImg.regnum_img;

                }
            return "ocr reset";
        }

        [HttpPost("/call_finger")]
        public string callFinger(OcrResult ocrMsg)
        {
            //AppSettings appSettings = AppSettings.Instance;
            byte[] fingerScanImg = null;
            FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
            Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
            fingerprintThread.Start();
            while (fingerScanImg == null)
            {
                // wait any one end
            }
            _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
            return _ocrCamera.finger_img;
        }

        [HttpPost("/call_padandfinger")]
        public string callPad(OcrResult ocrMsg)
        {
            //AppSettings appSettings = AppSettings.Instance;
            string TEMP_DIR = ".\\temp\\";
            if (!Directory.Exists(TEMP_DIR))
                Directory.CreateDirectory(TEMP_DIR);
            string filename = Path.GetRandomFileName();
            string signPadImgPath = Path.Combine(TEMP_DIR, filename);
            byte[] fingerScanImg = null;
            FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
            Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
            fingerprintThread.Start();
            
            Signpad.CallSignPadEvent(ocrMsg.name, ocrMsg.birth, ocrMsg.addr, signPadImgPath, fingerprintThread);

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
                        _ocrCamera.sign_img = Convert.ToBase64String(signPadImg);
                    }
                }
                // 지문이미지 처리
                else if (fingerScanImg != null)
                {
                    Signpad.closeWizard();
                    _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                }
            }
            catch (Exception)
            {
            }
            return "singpad / fingerprint scanner";
        }
    }
}
