using IzzixWarp;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CD1HW.Hardware
{
    /// <summary>
    /// 디젠트 지문 인식기 코드
    /// mfc 드라이버를 래핑항 Izzix.dll을 호출하는 코드
    /// Izzix.dll에 관해선 해당 공유 라이브러리를 빌드한 별개의 프로젝트를 참조
    /// 원본 mfc 드라이버가 pointer만 지원하는 부분이 있어 unsafe를 선언하여 native 코드의 사용
    /// </summary>
    public unsafe class IzzixFingerprint
    {
        // image buffer 지정
        const int IMAGE_SIZE_MAX = 640 * 480;
        const int FEATURE_SIZE_MAX = 480;
        const int FEATURE_SIZE_ISO_MAX = 630;

        private readonly ILogger<IzzixFingerprint> _logger;

        public IzzixFingerprint(ILogger<IzzixFingerprint> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 지문센서의 정보를 저장하는 class
        /// 지문센서의 정보가 있어야 스캔된 지문 이미지의 decode가 가능함
        /// </summary>
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

        /// <summary>
        /// 지문 센서 정보 받기
        /// </summary>
        /// <param name="sensorIdx">정보를 받을 지문 센서의 index</param>
        /// <returns>지문센서의 정보 class</returns>
        public IzzixSensorInfo GetSensorInfo(int sensorIdx)
        {
            int nProduct, nSensor, width = 0, height = 0;
            int* pnProduct = &nProduct;
            int* pnSensor = &nSensor;
            int* pWidth = &width;
            int* pHeight = &height;
            string sensorName = "";
            try
            {
                // index의 센서 정보 받기 (nProduct, nSensor에 저장)
                IZZIX.GetDevInfos(sensorIdx, pnProduct, pnSensor);
                // 센서 정보를 바탕으로 센서로 부터 받는 이미지의 사이즈 받아오기 (width, height에 저장)
                IZZIX.GetImageSize(nSensor, pWidth, pHeight);
                // 센서 이름 받기
                sensorName = IZZIX.GetSensorString(nSensor);
                _logger.LogInformation("izzix fingerprint sensor imgae size : {0} * {1}, sensor : {2}", width, height, sensorName);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
            }
            // class return
            return new IzzixSensorInfo(sensorName, width, height);
        }

        private IzzixSensorInfo izzixSensor = null;

        /// <summary>
        /// 지문 스캔 
        /// </summary>
        /// <returns>스캔된 이미지의 bitmap byte array</returns>
        public byte[] ScanFinger()
        {
            _logger.LogInformation("fingerprint scan start");

            byte[] imgBuffer = null;

            // 지문인식기 연결 상태 확인
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
                // 디바이스 정보 받기 (센서 정보가 없다면.... 이미지 변환에 필요)
                if (izzixSensor == null)
                {
                    izzixSensor = GetSensorInfo(0);
                }

                while (result == 0)
                {
                    // 1초 간격으로 polling
                    Thread.Sleep(1000);
                    float fakeScore;
                    float* pFakeScore = &fakeScore;
                    int width = izzixSensor.width;
                    int height = izzixSensor.height;
                    int* pWidth = &width;
                    int* pHeight = &height;

                    // 오성 최적화 이전 버전 사용 함수
                    //result = IZZIX.GetFinger(0, (byte*)pRawImageData, (byte*)pFeature);
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
                // 서명패드와의 연동을 위한 인터럽트 선언
                // 서명패드가 입력되면 지문인식중인 thread를 interrupt
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);

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
