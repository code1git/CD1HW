using System.Threading.Tasks;
using Grpc;
using Grpc.Net.Client;

namespace CD1HW.Grpc
{
    public class CameraRpcClient
    {
        
        public void sendRpc()
        {
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5052");
            while (true)
            {
                Camerabuf.CamerabufClient client = new Camerabuf.CamerabufClient(channel);
                CamerabufSendFrame frame = new CamerabufSendFrame();
                AppSettings appSettings = AppSettings.Instance;
                lock(appSettings)
                {
                    frame.Base64Img = appSettings.imgBase64Str;
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
