using CD1HW;
using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.WinFormUi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
/*
 * Code1HW
 * 신분증 인식기 hardware contrller program
 * 
 * 주요기능
 * 신분증 인식기에 부속된 하드웨어의 컨트롤 (카메라, 서명패드, 지문인식기, 스피커)
 * 카메라의 frame을 ocr엔진에 전달하고, ocr 결과를 전송받음
 * windows상의 UI
 * web ui를 위한 데이터를 websocket를 통해 전송
 * 
 * 전체구조
 * ASP.NET core framework을 기본 베이스로 사용 (모든 object는 의존성 주입(DI) 패턴을 적용하여 사용하는것을 원칙으로 함)
 * 프로그램 기동중 상시 작동되여야 하는 카메라 / 서명패드는 Service Provider에 등록후 Thread로 작동
 * OcrCamera object를 전체 데이터의 컨트롤에 사용
 */
namespace CD1HW
{
    public class Program
    {
        // DI object 사용을 위해 각 모듈의 object는 Service Provider를 통해 받는다.
        public static IServiceProvider ServiceProvider { get; set; }
        
        // 프로그램 메인
        // web server기동 및 기동시 동작해야하는 다른 thread의 동작 선언
        // AudioDevice에 사용된 NAudio라이브러리의 디바이스 선택이 STA thread상에서 작동 하지 않음으로 사용 x
        //[STAThread]
        public static void Main(string[] args)
        {
            // 웹서버 기동, 세부설정은 Startup.cs
            IWebHost webHost = CreateWebHostBuilder(args).Build();
            webHost.RunAsync();

            ServiceProvider = webHost.Services;

            // 카메라 Thread
            Cv2Camera cv2Camera= ServiceProvider.GetRequiredService<Cv2Camera>();
            cv2Camera.CameraStart();
            
            // 선관위 데모용 excel/csv 읽기 (메모리에 리스트 저장)
            NecDemoExcel necDemoExcel = ServiceProvider.GetRequiredService<NecDemoExcel>();
            necDemoExcel.ReadDoc();

            // Wacom 서명패드(STU Libary)
            // device input에 대한 callback을 받기위해 thread를 유지 시킨다.
            WacomSTU wacomSTU = ServiceProvider.GetRequiredService<WacomSTU>();
            Thread signPadThread = new Thread(() => wacomSTU.StartPad());
            signPadThread.Start();

            //기동시의 기동음 출력
            AudioDevice audioDevice = ServiceProvider.GetRequiredService<AudioDevice>();
            try
            {
                audioDevice.PlaySound(@"./Media/DiviceInit.wav");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            // UI (트레이 아이콘)
            Application.EnableVisualStyles();
            Thread UiThread = new Thread(() => { Application.Run(new NotifyIconForm(cv2Camera, ServiceProvider.GetRequiredService<OcrCamera>())); });
            UiThread.IsBackground = true;
            UiThread.Start();
        }

        // web host builder(kestrel)
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
             WebHost.CreateDefaultBuilder(args)
            .UseKestrel()
            .UseStartup<Startup>()
            .ConfigureKestrel(serverOptions =>
            {
                // http interface용
                serverOptions.Listen(System.Net.IPAddress.Any, 5120, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http1;
                });

                // grpc용
                serverOptions.Listen(System.Net.IPAddress.Any, 5122, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http2;
                });
            });
    }
}
