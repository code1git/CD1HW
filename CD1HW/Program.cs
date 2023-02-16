using CD1HW;
using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.WinFormUi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace CD1HW
{
    public class Program
    {
        public static IServiceProvider ServiceProvider { get; set; }

        //[STAThread]
        public static void Main(string[] args)
        {
            IWebHost webHost = CreateWebHostBuilder(args).Build();
            webHost.RunAsync();

            ServiceProvider = webHost.Services;
            Cv2Camera cv2Camera= ServiceProvider.GetRequiredService<Cv2Camera>();
            cv2Camera.CameraStart();
            NecDemoCsv necDemoCsv = ServiceProvider.GetRequiredService<NecDemoCsv>();
            necDemoCsv.ReadCsv();
            WacomSTU wacomSTU = ServiceProvider.GetRequiredService<WacomSTU>();
            //wacomSTU.StartPad();

            Thread signPadThread = new Thread(() => wacomSTU.StartPad());
            signPadThread.Start();

            AudioDevice audioDevice = AudioDevice.Instance;
            audioDevice.PlaySound(@"./Media/DiviceInit.wav");

            Application.EnableVisualStyles();
            Thread UiThread = new Thread(() => { Application.Run(new NotifyIconForm(cv2Camera, ServiceProvider.GetRequiredService<OcrCamera>())); });
            UiThread.IsBackground = true;
            UiThread.Start();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
             WebHost.CreateDefaultBuilder(args)
            .UseKestrel()
            .UseStartup<Startup>()
            .ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(System.Net.IPAddress.Any, 5120, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http1;
                });
                serverOptions.Listen(System.Net.IPAddress.Any, 5122, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http2;
                });
            });
    }
}
