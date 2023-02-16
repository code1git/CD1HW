using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace CD1HW
{
    public class NecDemoExcel
    {
        private readonly AppSettings? _options;
        private readonly ILogger<NecDemoExcel> _logger;
        private Dictionary<string, string> _necDemoAddr;
        public NecDemoExcel(ILogger<NecDemoExcel> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _options = options.Value;
            _necDemoAddr = new Dictionary<string, string>();
        }
        private void ReleaseExcelObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }

        public void ReadExcel()
        {
            string NecDemoExcelPath = _options.NecDemoFilePath;

            Microsoft.Office.Interop.Excel.Application application = null;
            Workbook workbook = null;
            _Worksheet worksheet = null;
            try
            {

                application = new Microsoft.Office.Interop.Excel.Application();
                application.Visible = true;
                workbook = application.Workbooks.Open(NecDemoExcelPath);
                worksheet = (_Worksheet)workbook.Sheets[1];
                Console.WriteLine(worksheet.Name);
            }
            catch (Exception e)
            {
                _logger.LogError("read excel error! with " + e.Message);
            }
            finally
            {
                workbook.Close();
                application.Quit();
                ReleaseExcelObject(worksheet);
                ReleaseExcelObject(workbook);
                ReleaseExcelObject(application);
            }
        }
    }
}
