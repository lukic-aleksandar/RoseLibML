using Transformer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer.DTOs.Arguments
{
    public class PreviewCommandArguments
    {
        [Required]
        public string Fragment { get; set; }
        [Required]
        public List<MethodParameter> MethodParameters { get; set; }
    }
}
