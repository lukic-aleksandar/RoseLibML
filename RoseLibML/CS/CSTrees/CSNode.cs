﻿using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    [Serializable]
    public class CSNode : LabeledNode
    {
        public bool IsExistingRoslynNode
        {
            get { return ushort.TryParse(this.STInfo, out ushort result) || this.IsTreeLeaf; }
        }
        public bool UseRoslynMatchToWrite { get; set; }
        public int RoslynSpanStart { get; set; }
        public int RoslynSpanEnd { get; set; }


        // Možda bih ovo mogao da izmenim? To bi, možda, bilo dovoljno.
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
        public override bool IsTreeRoot()
        {
            if (STInfo == "8840") // 8840 == CompilationUnit
            {
                return true;
            }

            return false;
        }

        // Only fields that could be interesting for calculation
        public override LabeledNode CreateSimpleDuplicate()
        {
            var simpleDuplicate = new CSNode();
            simpleDuplicate.STInfo = STInfo;
            simpleDuplicate.CanHaveType = CanHaveType;
            simpleDuplicate.IsFragmentRoot = IsFragmentRoot;
            simpleDuplicate.IsTreeLeaf = IsTreeLeaf;
            simpleDuplicate.IsFixed = IsFixed;

            return simpleDuplicate;
        }

    }
}
