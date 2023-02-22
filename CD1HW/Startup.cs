using CD1HW.Grpc;
using CD1HW.Hardware;
using CD1HW.WinFormUi;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CD1HW
{
    // web server 기동을 위한 설정
    public class Startup
    {
        public IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 프로그램에 사용될 object를 선언
            services.Configure<AppSettings>(Configuration.GetSection(AppSettings.Settings));
            services.AddOptions();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddGrpc();
            services.AddSingleton<OcrCamera>();
            services.AddSingleton<AudioDevice>();
            services.AddSingleton<Cv2Camera>();
            services.AddSingleton<AudioDevice>();
            services.AddScoped<NotifyIconForm>();
            services.AddSingleton<NecDemoExcel>();
            services.AddSingleton<IzzixFingerprint>();
            services.AddSingleton<WacomSTU>();
            services.AddCors(opctions =>
            {
                opctions.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            //app.UseHttpsRedirection();
            //app.UseAuthorization();

            // web socket server 선언 (js ui들을 사용할수 있도록)
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };
            
            
            app.UseWebSockets(webSocketOptions);
            app.UseCors();

            app.UseRouting();

            // endpoint에 grpc선언 -> grpc를 통해 ocr engine과 통신
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<CamerabufService>();
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client."); });
                endpoints.MapControllers();
            });

            // file logger선언
            var _path = Directory.GetCurrentDirectory();
            loggerFactory.AddFile($"{_path}\\logs\\log.txt");
        }
    }
}
