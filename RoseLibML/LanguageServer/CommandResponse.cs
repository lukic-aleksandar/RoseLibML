using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.LanguageServer
{
    public class CommandResponse
    {
        public string Message { get; set; }
        public bool Value { get; set; }

        public CommandResponse(string message, bool value)
        {
            Message = message;
            Value = value;
        }

    }   
    
}
