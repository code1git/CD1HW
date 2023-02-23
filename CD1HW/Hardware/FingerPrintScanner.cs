using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using IzzixWarp;
using CD1HW.Controllers;
using Serilog;
using SharpDX;

namespace CD1HW.Hardware
{

    /// <summary>
    /// 디젠트 지문 인식기 코드
    /// **** 구버전 ****
    /// 동작은 동일한나 di을 이용하는것이 아닌 인스턴스 선언을 통하여 객체 유지
    /// 개선판 코드는 IzzixFingerprint.cs 참조
    /// 참조를 위해 남겨놈
    /// </summary>
    public sealed class FingerPrintScanner
    {

        private FingerPrintScanner() { isRunning = false; }
        private static readonly Lazy<FingerPrintScanner> _insteance = new Lazy<FingerPrintScanner>(() => new FingerPrintScanner());
        public static FingerPrintScanner Instance { get { return _insteance.Value; } }

        private static bool isRunning;

        // 메모리 버퍼 크기 정의
        const int IMAGE_SIZE_MAX = 640 * 480;
        const int FEATURE_SIZE_MAX = 480;
        const int FEATURE_SIZE_ISO_MAX = 630;

        // C++ 모듈 사용으로 unsafe 선언
        public unsafe byte[] ScanFinger()
        {
            Log.Debug("fp st log");
            Console.WriteLine("fp st console");

            byte[] imgBuffer = null;
            //lock (_insteance)
            //{
            if (IZZIX.IsAvailableDevice(0) != 1)
            {
                //_logger.LogCritical("Fingerprint scanner is not available");
                return null;
            }

            if (isRunning)
            {
                isRunning = false;
                Thread.Sleep(100);
            }
            isRunning = true;

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
                int nProduct, nSensor, width, height;
                int* pnProduct = &nProduct;
                int* pnSensor = &nSensor;
                int* pWidth = &width;
                int* pHeight = &height;
                IZZIX.GetDevInfos(0, pnProduct, pnSensor);
                IZZIX.GetImageSize(nSensor, pWidth, pHeight);

                string sensorName = "";
                sensorName = IZZIX.GetSensorString(nSensor);
                Log.Debug("debug! imgae size : {0} * {1}, sensor : {2}", width, height, sensorName);
                Console.WriteLine("debug! imgae size : {0} * {1}, sensor : {2}", width, height, sensorName);

                while (result == 0 && isRunning)
                {
                    Thread.Sleep(1000);
                    //result = IZZIX.GetFinger(0, (byte*)pRawImageData, (byte*)pFeature);
                    float fakeScore;
                    float* pFakeScore = &fakeScore;
                    result = IZZIX.GetFPImage(0, (byte*)pRawImageData, pWidth, pHeight, pFakeScore);
                }
                if (isRunning)
                {
                    // 이미지 포인터 -> Byte array
                    for (int i = 0; i < rawImageData.Length; i++)
                    {
                        rawImageData[i] = Marshal.ReadByte(pRawImageData, i);
                    }

                    // convert raw data to bitmap
                    // bitmap file header size : sizeof(BITMAPINFO)+(sizeof(RGBQUAD)*color) = 16+4*256
                    int bitmapInfoSize = 1040;
                    imgBuffer = new byte[width * height + bitmapInfoSize];
                    pImgBuffer = Marshal.AllocHGlobal(IMAGE_SIZE_MAX + bitmapInfoSize);

                    IZZIX.ConvertImage((byte*)pRawImageData, (byte*)pImgBuffer, width, height);
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
            }
            catch (ThreadInterruptedException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Log.Fatal(e.StackTrace);
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
                isRunning = false;
            }
            //}

            return imgBuffer;
        }

        public unsafe byte[] ScanFingerOnece()
        {
            byte[] imgBuffer = null;
            //lock (_insteance)
            //{
            if (IZZIX.IsAvailableDevice(0) != 1)
            {
                Log.Fatal("Fingerprint scanner is not available");
                return null;
            }

            if (isRunning)
            {
                isRunning = false;
                Thread.Sleep(300);
            }
            isRunning = true;

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

                // 디바이스 정보 받기 (이미지 변환에 필요)
                int nProduct, nSensor, width, height;
                int* pnProduct = &nProduct;
                int* pnSensor = &nSensor;
                int* pWidth = &width;
                int* pHeight = &height;
                IZZIX.GetDevInfos(0, pnProduct, pnSensor);
                IZZIX.GetImageSize(nSensor, pWidth, pHeight);

                string sensorName = "";
                sensorName = IZZIX.GetSensorString(nSensor);
                Log.Debug("debug! imgae size : {0} * {1}, sensor : {2}", width, height, sensorName);

                //  스캔

                //IZZIX.GetFinger(0, (byte*)pRawImageData, (byte*)pFeature);
                float fakeScore;
                float* pFakeScore = &fakeScore;
                int result = IZZIX.GetFPImage(0, (byte*)pRawImageData, pWidth, pHeight, pFakeScore);

                if (isRunning)
                {
                    // 이미지 포인터 -> Byte array
                    for (int i = 0; i < rawImageData.Length; i++)
                    {
                        rawImageData[i] = Marshal.ReadByte(pRawImageData, i);
                    }


                    

                    // convert raw data to bitmap
                    // bitmap file header size : sizeof(BITMAPINFO)+(sizeof(RGBQUAD)*color) = 16+4*256
                    int bitmapInfoSize = 1040;
                    imgBuffer = new byte[width * height + bitmapInfoSize];
                    pImgBuffer = Marshal.AllocHGlobal(IMAGE_SIZE_MAX + bitmapInfoSize);

                    IZZIX.ConvertImage((byte*)pRawImageData, (byte*)pImgBuffer, width, height);

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
            }
            catch (Exception e)
            {
                Log.Fatal(e.StackTrace);
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
                isRunning = false;
            }
            //}

            return imgBuffer;
        }
    }
}
