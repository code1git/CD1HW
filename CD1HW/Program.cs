using CD1HW;
using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.WinFormUi;
using Microsoft.AspNetCore.Server.Kestrel.Core;

Thread UiThread = new Thread(() => { Application.Run(new NotifyIconForm()); });
UiThread.IsBackground = true;
UiThread.Start();

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddSingleton<FingerPrintScanner>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(serverOptions =>
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

//////////
builder.Services.AddGrpc();
Cv2Camera.Instance.CameraStart();
//CameraRpcClient cameraRpcClient = new CameraRpcClient();
//cameraRpcClient.stardRoop();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())

{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<CamerabufService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

[STAThread]
static void Main()
{


    // To customize application configuration such as set high DPI settings or default font,
    // see https://aka.ms/applicationconfiguration.
}