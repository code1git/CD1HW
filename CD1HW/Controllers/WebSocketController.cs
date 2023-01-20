﻿using System.Drawing;
using System.Drawing.Imaging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CD1HW;
using CD1HW.Controllers;
using CD1HW.Hardware;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace WebSocketsSample.Controllers;

// <snippet>
public class WebSocketController : ControllerBase
{
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(ILogger<WebSocketController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogInformation("ws connected!");
            try
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await StreamCamera(webSocket);

            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }
        }
        else
        {
            _logger.LogInformation("ws fail (not an ws request)");
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    // </snippet>

    private static async Task StreamCamera(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            //byte[] msgBuf = Encoding.UTF8.GetBytes(AppSettings.imgBase64Str);
            WsMsg wsMsg = new WsMsg();
            AppSettings appSettings = AppSettings.Instance;
            lock (appSettings)
            {
                wsMsg.imgBase64Str = appSettings.imgBase64Str;
                wsMsg.name = appSettings.name;
                wsMsg.addr = appSettings.addr;
                wsMsg.birth = appSettings.birth;
                wsMsg.name_img = appSettings.name_img;
                wsMsg.regnum_img = appSettings.regnum_img;
                wsMsg.face_img = appSettings.face_img;   
            }
            byte[] msgBuf = JsonSerializer.SerializeToUtf8Bytes<WsMsg>(wsMsg);



            await webSocket.SendAsync(
                new ArraySegment<byte>(msgBuf, 0, msgBuf.Length),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);
                receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        //_logger.LogInformation("try close ws");
        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }

}

public class WsMsg
{
    public string imgBase64Str { get; set; }
    public string name { get; set; }
    public string addr { get; set; }
    public string birth { get; set; }
    public string regnum { get; set; }
    public string name_img { get; set; }
    public string regnum_img { get; set; }
    public string face_img { get; set; }
    public string birth_img { get; set; }
}