using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using SharpDX.Text;
using MiniExcelLibs;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace CD1HW
{
    public class NecDemoExcel
    {
        private readonly AppSettings? _options;
        private readonly ILogger<NecDemoExcel> _logger;
        private List<Person> _necDemoAddr;
        public NecDemoExcel(ILogger<NecDemoExcel> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _options = options.Value;
            _necDemoAddr = new List<Person>();
        }

        public void ReadDoc()
        {
            string NecDemoFilePath = _options.NecDemoFilePath;
            string NecDemoFileType = _options.NecDemoFileType;
            if (NecDemoFileType.Equals("csv"))
            {
                _logger.LogInformation("read csv 4 nec start.... : " + _options.NecDemoFilePath);

                try
                {
                    using (StreamReader sr = new StreamReader(NecDemoFilePath))
                    using (CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture))
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
            else
            {
                _logger.LogInformation("read Excel 4 nec start.... : " + _options.NecDemoFilePath);

                try
                {
                    using (FileStream stream = File.Open(NecDemoFilePath, FileMode.Open, FileAccess.Read))
                    {

                        var rows = stream.Query();
                        foreach (var row in rows)
                        {
                            Person person = new Person();
                            person.Name = row.A;
                            person.Address = row.C;
                            person.Regnum = row.B;
                            _necDemoAddr.Add(person);
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
            name = name.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            regnum = regnum.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            birth = birth.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            string birthTmp8 = birth;
            if (birth.Length == 8)
            {
                birth = birth.Substring(2, 6);
            }

            List<Person> people = new List<Person>();
            foreach (Person person in _necDemoAddr)
            {
                string personName = person.Name.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("(", "").Replace(")", "");
                string personRegnum = person.Regnum.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("(", "").Replace(")", "");

                if (personName.Equals(name) )
                {
                    if(regnum!=null && !regnum.Equals(""))
                    {
                        if (regnum.Equals(personRegnum))
                        {
                            people.Add(person);
                        }
                    }
                    else
                    {
                        if (birthTmp8.Equals(personRegnum))
                        {
                            people.Add(person);
                        }
                        if (personRegnum.Length> 6)
                        {
                            personRegnum = personRegnum.Substring(0, 6);
                        }
                        if(birth.Equals(personRegnum) )
                        {
                            people.Add(person);
                        }
                    }
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
