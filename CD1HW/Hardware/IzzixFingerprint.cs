using IzzixWarp;
using System.Runtime.InteropServices;

namespace CD1HW.Hardware
{
    public unsafe class IzzixFingerprint
    {
        const int IMAGE_SIZE_MAX = 640 * 480;
        const int FEATURE_SIZE_MAX = 480;
        const int FEATURE_SIZE_ISO_MAX = 630;

        private readonly ILogger<IzzixFingerprint> _logger;

        public IzzixFingerprint(ILogger<IzzixFingerprint> logger)
        {
            _logger = logger;
        }

        public class IzzixSensorInfo
        {
            public string sensorName;
            public int width;
            public int height;

            public IzzixSensorInfo(string sensorName, int width, int height)
            {
                this.sensorName = sensorName;
                this.width = width;
                this.height = height;
            }
        }

        public IzzixSensorInfo GetSensorInfo()
        {
            int nProduct, nSensor, width = 0, height = 0;
            int* pnProduct = &nProduct;
            int* pnSensor = &nSensor;
            int* pWidth = &width;
            int* pHeight = &height;
            string sensorName = "";
            try
            {
                IZZIX.GetDevInfos(0, pnProduct, pnSensor);
                IZZIX.GetImageSize(nSensor, pWidth, pHeight);

                sensorName = IZZIX.GetSensorString(nSensor);
                Console.WriteLine("debug! imgae size : {0} * {1}, sensor : {2}", width, height, sensorName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new IzzixSensorInfo(sensorName, width, height);
        }

        private IzzixSensorInfo izzixSensor = null;

        // C++ 모듈 사용으로 unsafe 선언
        public byte[] ScanFinger()
        {
            Console.WriteLine("fp st console");

            byte[] imgBuffer = null;
            if (IZZIX.IsAvailableDevice(0) != 1)
            {
                return null;
            }
            Thread.Sleep(100);

            // libray 사용을 위한 포인터/버퍼 선언
            IntPtr pRawImageData = IntPtr.Zero;
            IntPtr pFeature = IntPtr.Zero;
            IntPtr pImgBuffer = IntPtr.Zero;
            try
            {
                // scan fingerprint
                pRawImageData = Marshal.AllocHGlobal(IMAGE_SIZE_MAX);
                pFeature = Marshal.AllocHGlobal(FEATURE_SIZE_ISO_MAX);

                byte[] rawImageData = new byte[IMAGE_SIZE_MAX];
                byte[] feature = new byte[FEATURE_SIZE_ISO_MAX];

                // 터치상태에서 스캔이 될때 (result == 1)이 될 때까지 반복적으로 스캔
                int result = 0;
                // 디바이스 정보 받기 (이미지 변환에 필요)
                if (izzixSensor == null)
                {
                    izzixSensor = GetSensorInfo();
                }

                while (result == 0)
                {
                    Thread.Sleep(1000);
                    //result = IZZIX.GetFinger(0, (byte*)pRawImageData, (byte*)pFeature);
                    float fakeScore;
                    float* pFakeScore = &fakeScore;
                    int width = izzixSensor.width;
                    int height = izzixSensor.height;
                    int* pWidth = &width;
                    int* pHeight = &height;

                    result = IZZIX.GetFPImage(0, (byte*)pRawImageData, pWidth, pHeight, pFakeScore);
                }

                // 이미지 포인터 -> Byte array
                for (int i = 0; i < rawImageData.Length; i++)
                {
                    rawImageData[i] = Marshal.ReadByte(pRawImageData, i);
                }

                // convert raw data to bitmap
                // bitmap file header size : sizeof(BITMAPINFO)+(sizeof(RGBQUAD)*color) = 16+4*256
                int bitmapInfoSize = 1040;
                imgBuffer = new byte[izzixSensor.width * izzixSensor.height + bitmapInfoSize];
                pImgBuffer = Marshal.AllocHGlobal(IMAGE_SIZE_MAX + bitmapInfoSize);

                IZZIX.ConvertImage((byte*)pRawImageData, (byte*)pImgBuffer, izzixSensor.width, izzixSensor.height);
                for (int i = 0; i < imgBuffer.Length; i++)
                {
                    imgBuffer[i] = Marshal.ReadByte(pImgBuffer, i);
                }

                // test code : save bitmap
                /*string path = @".\save\tmp1.bmp";
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                fs.Write(imgBuffer, 0, width*height);
                    fs.Close();*/
            }
            catch (ThreadInterruptedException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            finally
            {
                try
                {
                    // *****free unmannaged memory*****
                    // unsafe 상태로 사용한 자원 반환 (제대로 처리되지않으면 memory leak의 원인이 될 수 있음)
                    Marshal.FreeHGlobal(pRawImageData);
                    Marshal.FreeHGlobal(pFeature);
                    Marshal.FreeHGlobal(pImgBuffer);
                }
                catch (Exception)
                {
                }
            }

            return imgBuffer;
        }
    }
}
