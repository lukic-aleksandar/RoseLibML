using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Util
{
    class ConfigReader
    {
        public static Config ReadValidateConfig(string path, List<ValidationResult> validationResults)
        {
            string json = System.IO.File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<Config>(json);
            
            DataAnnotationsValidator.TryValidateObjectRecursive<Config>(config, validationResults);
            return config;
        }
    }
}
