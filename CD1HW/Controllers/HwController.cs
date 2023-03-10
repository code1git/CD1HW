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
        private readonly WacomSTU _wacomSTU;

        public HwController(ILogger<HwController> logger, Cv2Camera cv2Camera, OcrCamera ocrCamera, NecDemoExcel necExcel, IzzixFingerprint izzixFingerprint, WacomSTU wacomSTU, AudioDevice audioDevice)
        {
            _logger = logger;
            _cv2Camera = cv2Camera;
            _ocrCamera = ocrCamera;
            _necExcel = necExcel;
            _audioDevice = audioDevice;
            _izzixFingerprint = izzixFingerprint;
            _wacomSTU = wacomSTU;
        }

        /// <summary>
        /// 카메라 리셋
        /// </summary>
        /// <returns></returns>
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
        /// 저장된 ocr 결과 리셋
        /// </summary>
        /// <returns></returns>
        [HttpGet("/ocr_reset")]
        public string OcrReset()
        {
            _logger.LogInformation("call ocr reset by web");

            _ocrCamera.id_card_type = "";
            _ocrCamera.name = "";
            _ocrCamera.regnum = "";
            _ocrCamera.addr = "";
            _ocrCamera.birth = "";
            _ocrCamera.addr = "";
            _ocrCamera.sex = "";

            _ocrCamera.birth_img = "";
            _ocrCamera.face_img = "";
            _ocrCamera.name_img = "";
            _ocrCamera.regnum_img = "";
            _ocrCamera.masking_img = "";
            _ocrCamera.finger_img = "";
            _ocrCamera.sign_img = "";
            return "ocr reset";
        }

        /// <summary>
        /// 수동스캔
        /// </summary>
        /// <returns>소요시간 (100nanosec)</returns>
        [HttpGet("/manual_scan")]
        public string ManualScan()
        {
            _logger.LogInformation("call manual scan by web");
            _ocrCamera.manual_flag = 1;
            try
            {
                while (_ocrCamera.manual_flag == 1 || _ocrCamera.manual_time_chk_flag == 1)
                {
                
                }
                long ocrTime = _ocrCamera.manual_ed_time_mill - _ocrCamera.manual_st_time_mill;
                return ocrTime.ToString();

            }
            catch (Exception)
            {
                return "0";
            }
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

        [HttpPost("/ocr_result")]
        public string OcrResult(OcrMsg ocrMsg)
        {
            CD1HW.Model.OcrResult ocrResult = ocrMsg.ocr_result;
            ResultImg resultImg = ocrMsg.result_image;

            if (ocrResult != null)
            {
                _ocrCamera.name = ocrResult.name;
                _ocrCamera.regnum = ocrResult.regnum;
                // 선관위 (addr x)
                //_ocrCamera.addr = ocrResult.addr;
                _ocrCamera.birth = ocrResult.birth;
                _ocrCamera.sex = ocrResult.sex;
                _ocrCamera.id_card_type = ocrResult.id_card_type;
                _ocrCamera.issue_date = ocrResult.issue_date;
                if(ocrResult.regnum != null && (ocrResult.sex == null || ocrResult.sex.Equals("")))
                {
                    try
                    {
                        string[] regnumSplit = ocrResult.regnum.Split("-");
                        int regnumSexNum = int.Parse(regnumSplit[1].Substring(0,1));
                        if (regnumSexNum == 1 || regnumSexNum == 3)
                        {
                            _ocrCamera.sex = "1";
                        }
                        else if (regnumSexNum == 2 || regnumSexNum == 4)
                        {
                            _ocrCamera.sex = "2";
                        }

                    }
                    catch (Exception)
                    {
                    }
                }

            }

            if (resultImg != null)
            {
                _ocrCamera.birth_img = resultImg.birth_img;
                _ocrCamera.face_img = resultImg.face_img;
                _ocrCamera.name_img = resultImg.name_img;
                _ocrCamera.regnum_img = resultImg.regnum_img;
                _ocrCamera.masking_img = resultImg.masking_img;

            }
            _ocrCamera.SaveResult();
            if(_ocrCamera.manual_time_chk_flag == 1)
            {
                _ocrCamera.manual_ed_time_mill = DateTime.Now.Ticks;
                _ocrCamera.manual_time_chk_flag = 0;
            }
            
            return "ocr reset";
        }

        /// <summary>
        /// 지문인식기 호출 (테스트용)
        /// </summary>
        /// <param name="ocrMsg"></param>
        /// <returns></returns>
        [HttpPost("/call_finger")]
        public string callFinger(OcrResult ocrMsg)
        {
            try
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
            }
            return _ocrCamera.finger_img;
        }
        /*[HttpPost("/call_padandfinger_old")]
        public string callPad(OcrResult ocrMsg)
        {   
            _logger.LogInformation("call finger print scanner and sing pad by web");
            _ocrCamera.finger_img = null;
            _ocrCamera.sign_img = null;
            
            try
            {
                
                string TEMP_DIR = ".\\temp\\";
                if (!Directory.Exists(TEMP_DIR))
                    Directory.CreateDirectory(TEMP_DIR);
                string filename = Path.GetRandomFileName();
                string signPadImgPath = Path.Combine(TEMP_DIR, filename);
                
                byte[] fingerScanImg = null;
                //IzzixFingerprint iZZIXFingerprint = new IzzixFingerprint();
                //FingerPrintScanner fingerPrintScanner = FingerPrintScanner.Instance;
                //Thread fingerprintThread = new Thread(delegate () { fingerScanImg = fingerPrintScanner.ScanFinger(); });
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
                    Signpad.CallSignPadEvent(ocrMsg.name, ocrMsg.birth, ocrMsg.addr, signPadImgPath, fingerprintThread);
                }
                catch (Exception e)
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
                        _logger.LogInformation("sing detected : " + signPadImgPath);
                        Thread.Sleep(500);

                        using (FileStream fs = new FileStream(signPadImgPath, FileMode.Open, FileAccess.Read))
                        {
                            byte[] signPadImg = new byte[fs.Length];
                            fs.Read(signPadImg, 0, signPadImg.Length);
                            fs.Close();
                            _ocrCamera.sign_img = Convert.ToBase64String(signPadImg);
                        }
                        System.IO.File.Delete(signPadImgPath);
                    }
                    // 지문이미지 처리
                    else if (fingerScanImg != null)
                    {
                        _logger.LogInformation("finger scaned");
                        _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                    }
                    
                    try
                    {
                        _audioDevice.StopSound();
                        _audioDevice.PlaySound(@"./Media/SignEnd.wav");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                        //_audioDevice.outputDevice = null;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                    /////
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return "singpad / fingerprint scanner";
        }*/

        /// <summary>
        /// 지문인식기 + 서명패드 호출
        /// 둘중하나가 입력완료되면 반대쪽은 종료
        /// </summary>
        /// <param name="ocrMsg">서명패드에 표시될 정보. 이름, 생년월일, 주소</param>
        /// <returns></returns>
        [HttpPost("/call_padandfinger")]
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
                        try
                        {
                            _audioDevice.StopSound();
                            _audioDevice.PlaySound(@"./Media/SignEnd.wav");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.Message);
                            //_audioDevice.outputDevice = null;
                        }
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
                        try
                        {
                            _audioDevice.StopSound();
                            _audioDevice.PlaySound(@"./Media/SignCancel.wav");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.Message);
                            //_audioDevice.outputDevice = null;
                        }
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
                        try
                        {
                            _audioDevice.StopSound();
                            _audioDevice.PlaySound(@"./Media/SignEnd.wav");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.Message);
                            //_audioDevice.outputDevice = null;
                        }
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
        }
    }

}
