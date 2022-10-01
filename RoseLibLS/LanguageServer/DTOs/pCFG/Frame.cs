using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer
    class Frame<T>
    {
        public int FrameNumber { get; set; }
        public List<T> Items { get; set; }
        public int TotalRules { get; set; }
        public int TotalRuleFrames { get; set; }
    }
}
