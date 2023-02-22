using Grpc;
using Grpc.Core;

namespace CD1HW.Grpc
{
    /* Grpc service code
     * client (ocr engine)로 부터 유휴상태 flag를 받으면 (프레임 요청을 받으면)
     * 현재의 camera frame와 수동촬영에 대한 flag를 response
     */
    public class CamerabufService:Camerabuf.CamerabufBase
    {
        private readonly ILogger<CamerabufService> _logger;
        private readonly OcrCamera _ocrCamera;
        public CamerabufService(ILogger<CamerabufService> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
        }
        public override Task<CamerabufSendFrame> SendCameraframe(CamerabufRequest request, ServerCallContext context)
        {
            CamerabufSendFrame frame = new CamerabufSendFrame();
            frame.Base64Img = _ocrCamera.imgBase64Str;
            frame.ManualFlag = _ocrCamera.manual_flag;
            // 수동촬영시의 소요시간 체크용 코드
            if(_ocrCamera.manual_flag == 1)
            {
                _ocrCamera.manual_time_chk_flag = 1;
                _ocrCamera.manual_st_time_mill = DateTime.Now.Ticks;
                _ocrCamera.manual_flag = 0;
            }
            return Task.FromResult(frame);
        }
    }
}
