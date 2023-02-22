using System.Threading.Tasks;
using CD1HW.Controllers;
using Grpc;
using Grpc.Net.Client;

namespace CD1HW.Grpc
{
    /* grpc client 예제
     * grpc를 server side로 작동함으로 사용하지 않으나, 테스트를 위해 작성한 코드
     */
    public class CameraRpcClient
    {
        private readonly ILogger<CameraRpcClient> _logger;
        private readonly OcrCamera _ocrCamera;
        public CameraRpcClient(ILogger<CameraRpcClient> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
        }
        public void sendRpc()
        {
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5052");
            while (true)
            {
                Camerabuf.CamerabufClient client = new Camerabuf.CamerabufClient(channel);
                CamerabufSendFrame frame = new CamerabufSendFrame();
                lock(_ocrCamera)
                {
                    frame.Base64Img = _ocrCamera.imgBase64Str;
                }
                CamerabufRequest response = client.SendCameraframePysv(frame);
            }
        }

        public void stardRoop()
        {
            Thread roopThread = new Thread(new ThreadStart(sendRpc));
            roopThread.Start();
        }
    }
}
