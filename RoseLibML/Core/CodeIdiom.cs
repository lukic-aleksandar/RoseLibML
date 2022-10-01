using System.Collections.Generic;

namespace RoseLibML.Core
{
    public class CodeIdiom
    {
        public string RootNodeType { get; set; }
        public string Fragment { get; set; }
        public List<string> Metavariables { get; set; } = new List<string>();
        public List<string> ContextNodes { get; set; } = new List<string>();
    }
}
