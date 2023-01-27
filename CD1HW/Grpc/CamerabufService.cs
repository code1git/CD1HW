using Grpc;
using Grpc.Core;

namespace CD1HW.Grpc
{
    public class CamerabufService:Camerabuf.CamerabufBase
    {
        private readonly ILogger<CamerabufService> _logger;
        public CamerabufService(ILogger<CamerabufService> logger)
        {
            _logger = logger;
        }
        public override Task<CamerabufSendFrame> SendCameraframe(CamerabufRequest request, ServerCallContext context)
        {
            CamerabufSendFrame frame = new CamerabufSendFrame();
            AppSettings appSettings = AppSettings.Instance;
            lock(appSettings)
            {
                frame.Base64Img = appSettings.imgBase64Str;
                frame.ManualFlag = 0;
            }
            return Task.FromResult(frame);
        }
    }
}
