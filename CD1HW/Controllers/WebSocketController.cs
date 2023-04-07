using System.Drawing;
using System.Drawing.Imaging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CD1HW;
using CD1HW.Controllers;
using CD1HW.Hardware;
using CD1HW.Model;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace WebSocketsSample.Controllers;

/// <summary>
/// websoket controller
/// websoket를 통해 현재의 status를 매 요청동안 전송
/// client side에서 요청을 받으면 현재의 camera frame과 보관하고있는 ocr정보등을 전송
/// client에서는 받은 데이터로부터 원하는 정보는 parsing하여 사용
/// </summary>
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
    /// <summary>
    /// 카메라 스트림 + ocr데이터 ws를 통해 전송
    /// </summary>
    /// <param name="webSocket"></param>
    /// <returns></returns>
    private async Task StreamCamera(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            //byte[] msgBuf = Encoding.UTF8.GetBytes(AppSettings.imgBase64Str);
            OcrResult ocrResult = _ocrCamera.ocrResult;
            ocrResult.imageBase64 = _ocrCamera.imageBase64;
            
            
            byte[] msgBuf = JsonSerializer.SerializeToUtf8Bytes(_ocrCamera.ocrResult);



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