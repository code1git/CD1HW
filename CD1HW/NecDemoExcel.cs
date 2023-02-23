using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using SharpDX.Text;
using MiniExcelLibs;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace CD1HW
{
    /// <summary>
    /// 선관위 데모용 주소 db 엑셀 read
    /// 선관위로 부터 이름, 개인식별번호, 주소 순으로 기록된 데모용 엑셀 파일 제공
    /// 선관의 데모시 주소의 ocr은 실행하지 않고 이름, 개인식별번호를 key로 엘셀내에서 주소를 서치
    /// 실제 제춤구현시에는 이름, 개인식별번호의 ocr정보를 key로 선거인 관리 명부 시스템으로 부터 주소를 받도록 구성
    /// </summary>
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

        /// <summary>
        /// application에 기재된 path에 있는 엑셀파일 read -> 리스트 저장
        /// MiniExcel라이브러리가 csv도 지원하나, 개발 일정상 데모일자에 따른 리스크도 있어 csv리더는 기존 구현한 방식을 사용.
        /// csv는 utf-8인코딩만 지원 (특정 사이트에대한 데모사양임으로 필요이상의 리스크 요소는 배제)
        /// </summary>
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
        
        /// <summary>
        /// 저장된 주소 리스트에서 검색
        /// </summary>
        /// <param name="name">이름</param>
        /// <param name="regnum">주민번호</param>
        /// <param name="birth">생년월일</param>
        /// <returns>조건에 적합한 인물 list</returns>
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
