using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoseLibLS.Transformer
{
    public class OutputSnippet
    {
        [Required]
        public string Composer { get; set; }
        [Required]
        public string Fragment { get; set; }
        [Required]
        public string RootNodeType { get; set; }
        [Required]
        public string MethodName { get; set; }
        public List<MethodParameter> MethodParameters { get; set; }
    }

    public class MethodParameter {
        [Required]
        public string Parameter { get; set; }
        [Required]
        public string Metavariable { get; set; }
    }
}
