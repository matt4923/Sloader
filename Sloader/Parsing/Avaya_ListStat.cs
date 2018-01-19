using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Sloader.Parsing
{
    class Avaya_ListStat
    {
        private StreamReader m_File;

        public Avaya_ListStat()
        {
            m_File = new StreamReader(System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Report\\Avaya\\List_Station.txt");
        }

        public List<int> GetNumberList()
        {
            List<int> s = new List<int>();

            //string pat = @"\e\[\d{1,2};\dH(?<DN>\d{3}-?(\d{3,4})(-?\d{4})?)\e\[\d{1,2};15H";
            string pat = @"\e\[\d{1,2};\dH(?<DN>[\d-\s]{8,15})\e\[\d{1,2};15H";
            while (!m_File.EndOfStream)
            {
                string line = m_File.ReadLine();
                Regex regex = new Regex(pat);
                Match m = regex.Match(line);
                if (m.Success)
                {
                    string dn = m.Groups["DN"].Value.Trim();
                    dn = dn.Replace("-", "");
                    
                    s.Add(int.Parse(dn));
                }
            }

            return s.Distinct().ToList(); //sometimes sets are duplicated in the list config stat so just send distinct values
        }
        
    }
}
