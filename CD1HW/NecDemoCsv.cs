using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Office.Interop.Excel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CD1HW
{
    public class NecDemoCsv
    {
        private readonly AppSettings? _options;
        private readonly ILogger<NecDemoCsv> _logger;
        private Dictionary<string, string> _necDemoAddr;
        public NecDemoCsv(ILogger<NecDemoCsv> logger, IOptions<AppSettings> options)
        {
            Console.WriteLine("ccc");
            _logger = logger;
            _options = options.Value;
            _necDemoAddr = new Dictionary<string, string>();
        }

        public void ReadCsv()
        {
            string NecDemoFilePath = _options.NecDemoFilePath;
            _logger.LogInformation(_options.NecDemoFilePath);

            try
            {
                using(StreamReader sr = new StreamReader(NecDemoFilePath))
                using(CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture))
                {
                    IEnumerable<Person> recodes = csv.GetRecords<Person>();
                    foreach(Person person in recodes)
                    {
                        //Console.WriteLine(person.Name + " " + person.Address);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("read nec demo file error! with " + e.Message);
            }
            finally
            {
               
            }
        }

        public class Person
        {
            [Index(0)]
            public string Name { get; set; }
            [Index(1)]
            public string Regnum { get; set; }
            [Index(2)]
            public string Address { get; set; }
        }
    }
}
