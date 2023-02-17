using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CD1HW
{
    public class NecDemoCsv
    {
        private readonly AppSettings? _options;
        private readonly ILogger<NecDemoCsv> _logger;
        private List<Person> _necDemoAddr;
        public NecDemoCsv(ILogger<NecDemoCsv> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _options = options.Value;
            _necDemoAddr = new List<Person>();
        }

        public void ReadCsv()
        {
            string NecDemoFilePath = _options.NecDemoFilePath;
            _logger.LogInformation("read csv 4 nec start.... : "+ _options.NecDemoFilePath);

            try
            {
                using(StreamReader sr = new StreamReader(NecDemoFilePath))
                using(CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture))
                {
                    _necDemoAddr = csv.GetRecords<Person>().ToList();
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

        public string GetAddr(string name)
        {
            foreach (Person person in _necDemoAddr)
            {
                if (person.Name.Equals(name))
                {
                    return person.Address;
                }
            }
            return "";
        }

        public List<Person> GetAddr(string name, string regnum, string birth)
        {
            List<Person> people = new List<Person>();
            foreach (Person person in _necDemoAddr)
            {
                if (person.Name.Equals(name.Replace("-", "").Replace(".", "").Replace(" ", "")) && (person.Regnum.Replace("-", "").Replace(".", "").Replace(" ", "").Equals(regnum)|| person.Regnum.Replace("-", "").Replace(".", "").Replace(" ", "").Equals(birth)))
                {
                    people.Add(person);
                }
            }
            return people;
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
