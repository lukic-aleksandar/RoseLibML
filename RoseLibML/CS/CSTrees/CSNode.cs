using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    public class CSNode : LabeledNode
    {
        public bool UseRoslynMatchToWrite { get; set; }
        public int RoslynSpanStart { get; set; }
        public int RoslynSpanEnd { get; set; }

        public bool IsExistingRoslynNode
        {
            get { return ushort.TryParse(this.STInfo, out ushort result); }
        }

        public bool CouldBeWritten
        {
            get
            {
                // A small hack!. 
                // To avoid adding new fields, I used this state to
                // denote that it shouldn't even be written.
                if (!IsExistingRoslynNode && UseRoslynMatchToWrite)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
