using System.Drawing;
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
    private readonly OcrCamera _ocrCamera;

    public WebSocketController(ILogger<WebSocketController> logger, OcrCamera ocrCamera)
    {
        _logger = logger;
        _ocrCamera = ocrCamera;
    }

    [HttpGet("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogInformation("web socket connected!");
            try
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await StreamCamera(webSocket);

            }
            catch (Exception e)
            {
                _logger.LogError("error in web socket! : " + e.Message);
            }
        }
        else
        {
            _logger.LogInformation("web socket fail (not an web socket request)");
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    // </snippet>

    private async Task StreamCamera(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            //byte[] msgBuf = Encoding.UTF8.GetBytes(AppSettings.imgBase64Str);
            WsMsg wsMsg = new WsMsg();
        
            wsMsg.imgBase64Str = _ocrCamera.imgBase64Str;
            wsMsg.name = _ocrCamera.name;
            wsMsg.regnum = _ocrCamera.regnum;
            wsMsg.addr = _ocrCamera.addr;
            wsMsg.birth = _ocrCamera.birth;
            wsMsg.sex = _ocrCamera.sex;
            wsMsg.name_img = _ocrCamera.name_img;
            wsMsg.regnum_img = _ocrCamera.regnum_img;
            wsMsg.face_img = _ocrCamera.face_img;
            wsMsg.masking_img = _ocrCamera.masking_img;
            wsMsg.sign_img= _ocrCamera.sign_img;
            wsMsg.finger_img = _ocrCamera.finger_img;
            
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
    public string sex { get; set; }
    public string name_img { get; set; }
    public string regnum_img { get; set; }
    public string face_img { get; set; }
    public string birth_img { get; set; }
    public string masking_img { get; set; }
    public string sign_img { get; set; }
    public string finger_img { get; set; }

}