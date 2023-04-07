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
using System.Drawing;

namespace CD1HW.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HwController : ControllerBase
    {
        private readonly ILogger<HwController> _logger;
        private readonly Cv2Camera _cv2Camera;
        private readonly OcrCamera _ocrCamera;
        private readonly NecDemoExcel _necExcel;
        private readonly AudioDevice _audioDevice;
        private readonly IzzixFingerprint _izzixFingerprint;
        private readonly SerialService _serialService;
        //private readonly WacomSTU _wacomSTU;

        public HwController(ILogger<HwController> logger, Cv2Camera cv2Camera, OcrCamera ocrCamera, NecDemoExcel necExcel,
            IzzixFingerprint izzixFingerprint, AudioDevice audioDevice, SerialService serialService)
        {
            _logger = logger;
            _cv2Camera = cv2Camera;
            _ocrCamera = ocrCamera;
            _necExcel = necExcel;
            _audioDevice = audioDevice;
            _izzixFingerprint = izzixFingerprint;
            _serialService = serialService;
        }

        /// <summary>
        /// 카메라 리셋
        /// </summary>
        /// <returns></returns>
        [HttpPost("/camera_reset")]
        [HttpGet("/camera_reset")]
        public string ResetCamera()
        {
            _logger.LogInformation("call camera reset by web");

            lock (_cv2Camera)
            {
                _cv2Camera.ResetCamera();
            }
            return "camera reset";
        }



        /// <summary>
        /// 선관위 데모용 명부조회
        /// </summary>
        /// <param name="ocrResult">성명, 주민번호, 생년월일</param>
        /// <returns>조건에 맍는 명부내 인물 리스트</returns>
        [HttpPost("/query_addr_4_nec")]
        public List<NecDemoExcel.Person> NecAddr(CD1HW.Model.OcrResult ocrResult)
        {
            string name = ocrResult.name;
            string regnum = ocrResult.regnum;
            string birth = ocrResult.birth;
            
            List<NecDemoExcel.Person> people = _necExcel.GetAddr(name, regnum, birth);
            
            return people;
        }

        /// <summary>
        /// 지문인식기 호출 (테스트용)
        /// </summary>
        /// <param name="ocrMsg"></param>
        /// <returns></returns>
        [HttpPost("/call_finger")]
        [HttpGet("/call_finger")]
        public string callFinger(OcrResult ocrMsg)
        {
            /*try
            {
                _logger.LogInformation("call finger print scanner by web");
                _ocrCamera.finger_img = null;
                byte[] fingerScanImg = null;
                FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
                Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
                fingerprintThread.Start();
                while (fingerScanImg == null)
                {
                    // wait any one end
                    Thread.Sleep(50);
                }
                _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }*/

            try
            {
                _ocrCamera.finger_img = null;
                _logger.LogInformation("call finger print scanner by web");
                Bitmap finger = _serialService.ScanFingerSFM();
                _ocrCamera.fingerBitmap = finger;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            return _ocrCamera.finger_img;
        }


        /* /// <summary>
         /// 지문인식기 + 서명패드 호출
         /// 둘중하나가 입력완료되면 반대쪽은 종료
         /// </summary>
         /// <param name="ocrMsg">서명패드에 표시될 정보. 이름, 생년월일, 주소</param>
         /// <returns></returns>
         [HttpPost("/call_padandfinger")]
         [HttpGet("/call_padandfinger")]
         public string CallSignnFinger(OcrResult ocrMsg)
         {
             if(ocrMsg.name == null || ocrMsg.name.Equals(""))
             {
                 if((ocrMsg.regnum == null || ocrMsg.regnum.Equals(""))&&(ocrMsg.birth == null || ocrMsg.birth.Equals("")))
                 {
                     return "no p data";
                 }
             }


             _logger.LogInformation("call finger print scanner and sing pad by web");
             _ocrCamera.finger_img = null;
             byte[] fingerScanImg = null;

             lock (_izzixFingerprint)
             {
                 Thread fingerprintThread = new Thread(delegate () { fingerScanImg = _izzixFingerprint.ScanFinger(); });
                 try
                 {
                     fingerprintThread.Start();
                 }
                 catch (Exception)
                 {
                     _logger.LogError("fingerprint scanner call fail");
                 }

                 try
                 {
                     _wacomSTU.SetSignPad(ocrMsg.name, ocrMsg.birth, ocrMsg.addr);
                 }
                 catch (Exception)
                 {
                 }


                 try
                 {
                     _audioDevice.StopSound();
                     _audioDevice.PlaySound(@"./Media/SignStart.wav");
                 }
                 catch (Exception e)
                 {
                     _logger.LogError(e.Message);
                     //_audioDevice.outputDevice = null;
                 }


                 while (fingerScanImg == null && _wacomSTU.completeFlag == 0)
                 {
                     // wait anyone end
                 }

                 try
                 {
                     //
                     // sign pad를 우선처리 (지문 터치센서의 오인식 가능성이 있음으로)
                     // sign pad completed
                     if (_wacomSTU.completeFlag == 1)
                     {
                         _logger.LogInformation("sign completed ");
                         fingerprintThread.Interrupt();
                         _ocrCamera.finger_img = null;
                         Bitmap compImage = Properties.Resources.sign_end;
                         _wacomSTU.SetPadImage(compImage);
                         _ocrCamera.SaveSignImg(ocrMsg.name, ocrMsg.birth);

                         _audioDevice.StopSound();
                         _audioDevice.PlaySound(@"./Media/SignEnd.wav");

                         _logger.LogError(e.Message);
                         //_audioDevice.outputDevice = null;

                         Bitmap initImage = Properties.Resources.sign_start;
                         _wacomSTU.SetPadImage(initImage);
                     }
                     else if (_wacomSTU.completeFlag == 2)
                     {
                         Bitmap initImage = Properties.Resources.sign_start;
                         _wacomSTU.SetPadImage(initImage);
                         _logger.LogInformation("sign canceled");
                         fingerprintThread.Interrupt();
                         _ocrCamera.finger_img = null;
                         _ocrCamera.sign_img = null;

                         _audioDevice.StopSound();
                         _audioDevice.PlaySound(@"./Media/SignCancel.wav");

                         _logger.LogError(e.Message);
                         //_audioDevice.outputDevice = null;
                         return "1";
                     }
                     // 지문이미지 처리
                     else if (fingerScanImg != null)
                     {
                         _wacomSTU.completeFlag = 1;
                         _logger.LogInformation("finger scaned");
                         _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                         _ocrCamera.sign_img = null;
                         Bitmap compImage = Properties.Resources.sign_end;
                         _wacomSTU.SetPadImage(compImage);
                         _ocrCamera.SaveFingerImg(ocrMsg.name, ocrMsg.birth);

                         _audioDevice.StopSound();
                         _audioDevice.PlaySound(@"./Media/SignEnd.wav");

                         _logger.LogError(e.Message);
                         //_audioDevice.outputDevice = null;

                         Bitmap initImage = Properties.Resources.sign_start;
                         _wacomSTU.SetPadImage(initImage);
                     }



                 }
                 catch (Exception e)
                 {
                     _logger.LogError(e.Message);
                 }

             }

             return "0";
         }

         /// <summary>
         /// 서명패드 취소 -> 취소 flag 업데이트
         /// </summary>
         /// <returns>현제 완료 스테이터스따른 결과값 (front단에서 처리위함)</returns>
         [HttpPost("/cancel_padandfinger")]
         [HttpGet("/cancel_padandfinger")]
         public string CancelSignnFinger()
         {
             if (_wacomSTU.completeFlag == 0)
             {
                 _wacomSTU.completeFlag = 2;
                 return "0";
             }
             else
             {
                 return "1";
             }
             return "cancel singpad / fingerprint scanner";
         }*/
    }

}
