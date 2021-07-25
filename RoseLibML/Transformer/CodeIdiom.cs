using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RoseLibML.LanguageServer.Transformer
{
    public class CodeIdiom
    {
        public List<string> ContextNodes { get; set; }
        public string RootCSType { get; set; }
        public string Fragment { get; set; }
        public List<string> Metavariables { get; set; } = new List<string>();
    }
}
