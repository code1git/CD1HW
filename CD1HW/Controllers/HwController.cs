using CD1HW.Hardware;
using CD1HW.Model;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

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
            lock (appSettings)
            {
                appSettings.name = null;
                appSettings.addr = null;
                appSettings.birth = null;

                appSettings.birth_img = null;
                appSettings.face_img = null;
                appSettings.name_img = null;
                appSettings.regnum_img = null;
            }
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
    }
}
