using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core.LabeledTrees
{
    public class NodeCreator
    {
        public virtual LabeledNode CreateNode()
        {
            return new LabeledNode();
        }
    }
}
