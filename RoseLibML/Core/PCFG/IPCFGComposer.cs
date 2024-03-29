﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core.PCFG
{
    public interface IPCFGComposer<T>
    {
        void CalculateProbabilities(List<T> treeRoots);
        double CalculateFragmentProbability(T root);
    }
}
