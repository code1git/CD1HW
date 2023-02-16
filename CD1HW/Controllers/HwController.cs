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
using WacomSTUTest.Hardware;
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
        private readonly NecDemoCsv _necCsv;
        private readonly AudioDevice _audioDevice;
        private readonly IzzixFingerprint _izzixFingerprint;
        private readonly WacomSTU _wacomSTU;

        public HwController(ILogger<HwController> logger, Cv2Camera cv2Camera, OcrCamera ocrCamera, AudioDevice audioDevice,NecDemoCsv necCsv, IzzixFingerprint izzixFingerprint, WacomSTU wacomSTU)
        {
            _logger = logger;
            _cv2Camera = cv2Camera;
            //_cv2Camera = Cv2Camera.Instance;
            _ocrCamera = ocrCamera;
            _necCsv = necCsv;
            _audioDevice = audioDevice;
            _izzixFingerprint = izzixFingerprint;
            _wacomSTU = wacomSTU;

            
        }
        
        [HttpGet("/nametest")]
        public string NameTest(string name)
        {
            _ocrCamera.name = name;
            string addr = _necCsv.GetAddr(name);
            if (addr.Equals(""))
                addr = "addr not found";
            _ocrCamera.addr = addr;
            return addr;
        }

        [HttpGet("/camera_info")]
        public string GetCameraBackendName()
        {
            return _cv2Camera.GetCameraBackendName();
        }

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

        [HttpGet("/manual_scan")]
        public string ManualScan()
        {
            _logger.LogInformation("call manual scan by web");

            _ocrCamera.manual_flag = 1;
            return "manual_scan";
        }

        /*[HttpPost("/query_addr_4_nec")]
        public string NecAddr(CD1HW.Model.OcrResult ocrResult)
        {
            string name = ocrResult.name;
            string regnum = ocrResult.regnum;
            _ocrCamera.name = name;
            string addr = _necCsv.GetAddr(name);
            if (addr.Equals(""))
                addr = "명부 데이터가 존재하지 않습니다.";
            _ocrCamera.addr = addr;
            return addr;
        }*/

        [HttpPost("/query_addr_4_nec")]
        public List<NecDemoCsv.Person> NecAddr(CD1HW.Model.OcrResult ocrResult)
        {
            string name = ocrResult.name.Replace("-", "").Replace(".", "").Replace(" ", "");
            string regnum = ocrResult.regnum.Replace("-", "").Replace(".", "").Replace(" ","");
            string birth = ocrResult.birth.Replace("-", "").Replace(".", "").Replace(" ", "");
            if(birth.Length == 8)
            {
                birth = birth.Substring(2, 6);
            }
            Console.WriteLine(birth);
            List<NecDemoCsv.Person> people = _necCsv.GetAddr(name, regnum, birth);
            
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
                _ocrCamera.addr = ocrResult.addr;
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
                // 선관위 전용
                if (_ocrCamera.ProductType.Equals("NEC"))
                {
                    _ocrCamera.addr = _necCsv.GetAddr(ocrResult.name);
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
            return "ocr reset";
        }

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
                    _audioDevice.outputDevice = null;
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
                        Signpad.closeWizard();
                        _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                try
                {
                    _audioDevice.StopSound();
                    _audioDevice.PlaySound(@"./Media/SignEnd.wav");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _audioDevice.outputDevice = null;
                }
                    /////
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return "singpad / fingerprint scanner";
        }

        [HttpPost("/call_padandfinger")]
        public string CallSignnFinger(OcrResult ocrMsg)
        {
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
                    _wacomSTU.completeFlag = 0;
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
                    _audioDevice.outputDevice = null;
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
                        fingerprintThread.Join();
                        _ocrCamera.finger_img = null;
                    }
                    else if (_wacomSTU.completeFlag == 2)
                    {
                        _logger.LogInformation("sign canceled");
                        fingerprintThread.Interrupt();
                        fingerprintThread.Join();
                        _ocrCamera.finger_img = null;
                        _ocrCamera.sign_img = null;
                    }
                    // 지문이미지 처리
                    else if (fingerScanImg != null)
                    {
                        _logger.LogInformation("finger scaned");
                        _ocrCamera.finger_img = Convert.ToBase64String(fingerScanImg);
                        _ocrCamera.sign_img = null;
                    }
                    _wacomSTU.SetPadImage(Properties.Resources.sign_start);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                try
                {
                    _audioDevice.StopSound();
                    _audioDevice.PlaySound(@"./Media/SignEnd.wav");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _audioDevice.outputDevice = null;
                }
            }

            return "singpad / fingerprint scanner";
        }


        [HttpGet("/padandfinger_cancel")]
        public string SignnFingerCancel()
        {
            _wacomSTU.completeFlag = 2;
            return "cancel singpad / fingerprint scanner";
        }

        [HttpGet("/pad")]
        public string Pad3()
        {
            _wacomSTU.SetSignPad("전병현", "860729", "김수환무거북이와두루미");
            return "test";
        }
    }

}
