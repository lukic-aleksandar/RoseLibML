using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer
{
    public class PCFGLoadingCommandArguments
    {
        [Required, Range(0, 2000000000)]
        public int NeededFrame { get; set; }

        public string RuleStartsWith { get; set; }
    }
}
