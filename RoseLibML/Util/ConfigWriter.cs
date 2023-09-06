using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Util
{
    class ConfigWriter
    {
        public static void WriteConfig(string inpath, string outpath)
        {

            string json = System.IO.File.ReadAllText(inpath);

            using (StreamWriter sw = File.AppendText(outpath))
            {
                sw.Write(json);
                sw.Write('\n');
            }
        }
    }
}
