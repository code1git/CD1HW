using Grpc;
using Grpc.Core;

namespace CD1HW.Grpc
{
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
            frame.ManualFlag = _ocrCamera.mamanual_flag;
            _ocrCamera.mamanual_flag = 0;
            return Task.FromResult(frame);
        }
    }
}
