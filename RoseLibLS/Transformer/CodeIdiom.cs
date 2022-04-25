﻿using System.Collections.Generic;

namespace RoseLibLS.Transformer
{
    public class CodeIdiom
    {
        public string RootNodeType { get; set; }
        public string Fragment { get; set; }
        public List<string> Metavariables { get; set; } = new List<string>();
        public List<string> Composers { get; set; } = new List<string>();
    }
}
