using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.Model;
using Microsoft.AspNetCore.Mvc;

namespace CD1HW.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly ILogger<OcrController> _logger;
        private readonly OcrCamera _ocrCamera;
        private readonly IdScanRpcClient _idScanRpcClient;
        private readonly SerialService _serialService;

        public OcrController(ILogger<OcrController> logger, OcrCamera ocrCamera, IdScanRpcClient idScanRpcClient, SerialService serialService)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
            _idScanRpcClient = idScanRpcClient;
            _serialService = serialService;
        }

        /// <summary>
        /// 수동스캔 현재 카메라 화상을 ocr
        /// </summary>
        /// <returns>Ocr 결과</returns>
        [HttpPost("/manual_scan")]
        [HttpGet("/manual_scan")]
        public OcrResult ManualScan()
        {
            OcrResult ocrResult = new OcrResult();
            OCRInfo oCRInfo = new OCRInfo();
            try
            {
                switch (_ocrCamera.IRMode)
                {
                    case "off":
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                    case "on":
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);

                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);

                        break;
                    case "both":
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        oCRInfo.IrImgBase64 = _ocrCamera.imageBase64;
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                    case "dev":
                        _serialService.IRSwitch(true);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        Thread.Sleep(_ocrCamera.IRTimesleep);
                        break;
                    default:
                        oCRInfo.ImgBase64 = _ocrCamera.imageBase64;
                        ocrResult = _idScanRpcClient.OcrProcess(oCRInfo);
                        break;
                }
            }
            catch (Exception)
            {
            }
            _ocrCamera.ocrResult = ocrResult;
            _ocrCamera.OcrResultUpdated();

            return ocrResult;
        }

        /// <summary>
        /// 저장된 ocr 결과 리셋
        /// </summary>
        /// <returns></returns>
        
        [HttpPost("/ocr_reset")]
        [HttpGet("/ocr_reset")]
        public string OcrReset()
        {
            _logger.LogInformation("call ocr reset by web");

            _ocrCamera.ocrResult = new OcrResult();
            _ocrCamera.OcrResultUpdated();
            return "ocr reset";
        }
    }
}
