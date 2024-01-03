using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core
{
    public interface Writer
    {
        void InitializeForSampler(BookKeeper bookKeeper, LabeledTree[] trees);
        void SetIteration(int iteration);
        void WriteSingleFragment(string fragmentInTreebankNotation);
        void Close();
    }
}
