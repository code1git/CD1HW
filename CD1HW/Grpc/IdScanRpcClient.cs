using CD1HW.Hardware;
using Grpc;
using Grpc.Net.Client;
using System.Text.Json;

namespace CD1HW.Grpc
{
    /// <summary>
    /// GRpc client
    /// OCR Engine과의 통신을 위한 GRPC client
    /// </summary>
    public class IdScanRpcClient
    {
        private readonly ILogger<IdScanRpcClient> _logger;
        public IdScanRpcClient(ILogger<IdScanRpcClient> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
        }

        public Model.OcrResult OcrProcess(OCRInfo oCRInfo)
        {
            Model.OcrResult ocr = new Model.OcrResult();
            long startTimeMill = DateTime.Now.Ticks;
            try
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5122");
                Code1gRPC.Code1gRPCClient client = new Code1gRPC.Code1gRPCClient(channel);
                OCRResult response = client.OcrStart(oCRInfo);
                _logger.LogInformation(response.OcrResult);
                string ocrResult = response.OcrResult;
                ocr = JsonSerializer.Deserialize<Model.OcrResult>(ocrResult);
            }
            catch (Exception e)
            {
                _logger.LogError("grpc connection fail.....");
            }
            finally
            {

            }
            long endTimeMill = DateTime.Now.Ticks;
            ocr.ocr_timemill = endTimeMill - startTimeMill;

            return ocr;
        }
    }
}
