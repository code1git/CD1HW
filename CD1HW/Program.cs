using CD1HW;
using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.WinFormUi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
/*
 * Code1HW
 * �ź��� �νı� hardware contrller program
 * 
 * �ֿ���
 * �ź��� �νı⿡ �μӵ� �ϵ������ ��Ʈ�� (ī�޶�, �����е�, �����νı�, ����Ŀ)
 * ī�޶��� frame�� ocr������ �����ϰ�, ocr ����� ���۹���
 * windows���� UI
 * web ui�� ���� �����͸� websocket�� ���� ����
 * 
 * ��ü����
 * ASP.NET core framework�� �⺻ ���̽��� ��� (��� object�� ������ ����(DI) ������ �����Ͽ� ����ϴ°��� ��Ģ���� ��)
 * ���α׷� �⵿�� ��� �۵��ǿ��� �ϴ� ī�޶� / �����е�� Service Provider�� ����� Thread�� �۵�
 * OcrCamera object�� ��ü �������� ��Ʈ�ѿ� ���
 */
namespace CD1HW
{
    public class Program
    {
        // DI object ����� ���� �� ����� object�� Service Provider�� ���� �޴´�.
        public static IServiceProvider ServiceProvider { get; set; }
        
        // ���α׷� ����
        // web server�⵿ �� �⵿�� �����ؾ��ϴ� �ٸ� thread�� ���� ����
        // AudioDevice�� ���� NAudio���̺귯���� ����̽� ������ STA thread�󿡼� �۵� ���� �������� ��� x
        //[STAThread]
        public static void Main(string[] args)
        {
            // ������ �⵿, ���μ����� Startup.cs
            IWebHost webHost = CreateWebHostBuilder(args).Build();
            webHost.RunAsync();

            ServiceProvider = webHost.Services;

            // ī�޶� Thread
            Cv2Camera cv2Camera= ServiceProvider.GetRequiredService<Cv2Camera>();
            cv2Camera.CameraStart();
            
            // ������ ����� excel/csv �б� (�޸𸮿� ����Ʈ ����)
            NecDemoExcel necDemoExcel = ServiceProvider.GetRequiredService<NecDemoExcel>();
            necDemoExcel.ReadDoc();

            // Wacom �����е�(STU Libary)
            // device input�� ���� callback�� �ޱ����� thread�� ���� ��Ų��.
            WacomSTU wacomSTU = ServiceProvider.GetRequiredService<WacomSTU>();
            Thread signPadThread = new Thread(() => wacomSTU.StartPad());
            signPadThread.Start();

            //�⵿���� �⵿�� ���
            AudioDevice audioDevice = ServiceProvider.GetRequiredService<AudioDevice>();
            try
            {
                audioDevice.PlaySound(@"./Media/DiviceInit.wav");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            // UI (Ʈ���� ������)
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
                // http interface��
                serverOptions.Listen(System.Net.IPAddress.Any, 5120, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http1;
                });

                // grpc��
                serverOptions.Listen(System.Net.IPAddress.Any, 5122, ListenOptions =>
                {
                    ListenOptions.Protocols = HttpProtocols.Http2;
                });
            });
    }
}
