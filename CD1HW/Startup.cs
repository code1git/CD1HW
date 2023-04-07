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
            services.Configure<Appsettings>(Configuration.GetSection(Appsettings.Settings));
            services.AddOptions();
            services.AddControllers();
            services.AddMvc().AddJsonOptions(opction =>
            {
                opction.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddSingleton<OcrCamera>();
            services.AddSingleton<AudioDevice>();
            services.AddSingleton<Cv2Camera>();
            services.AddSingleton<AudioDevice>();
            services.AddTransient<NotifyIconForm>();
            services.AddTransient<DemoUI>();
            //services.AddSingleton<NecDemoExcel>();
            services.AddSingleton<IzzixFingerprint>();
            //services.AddSingleton<WacomSTU>();
            services.AddCors(opctions =>
            {
                opctions.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader();
                });
            });
            services.AddSingleton<SerialService>();
            services.AddSingleton<IdScanRpcClient>();
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // file logger선언
            var _path = Directory.GetCurrentDirectory();
            loggerFactory.AddFile($"{_path}\\logs\\log.txt");
        }
    }
}
