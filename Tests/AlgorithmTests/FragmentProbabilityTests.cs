using NUnit.Framework;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Core.PCFG;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibMLTests.AlgorithmTests
{
    class FragmentProbabilityTests
    {
        // nt - non-terminal, t - terminal, fr (fragment root)
        //                      
        //                                           -- t1 (leaf)
        //                                   -- nt2 - 
        //                      -- nt1 (fr) -
        //                                   -- nt2 ( -- empty)
        //  rootnt - nt1 (fr) -
        // 
        //                      -- nt2 -
        //                              -- t1 (leaf)
        public CSTree CreateATree()
        {
            CSTree tree = new CSTree();

            CSNode rootNt = new CSNode();
            rootNt.STInfo = "rootnt";
            rootNt.IsFragmentRoot = true;
            tree.Root = rootNt;

            CSNode nt1_1 = new CSNode();
            nt1_1.STInfo = "nt1";
            nt1_1.IsFragmentRoot = true;
            rootNt.AddChild(nt1_1);

            CSNode nt1_2 = new CSNode();
            nt1_2.STInfo = "nt1";
            nt1_2.IsFragmentRoot = true;
            nt1_1.AddChild(nt1_2);

            CSNode nt2_1 = new CSNode();
            nt2_1.STInfo = "nt2";
            nt2_1.IsFragmentRoot = false;
            nt1_2.AddChild(nt2_1);

            CSNode t1_1 = new CSNode();
            t1_1.STInfo = "t1";
            t1_1.IsFragmentRoot = false;
            t1_1.CanHaveType = false;
            t1_1.IsTreeLeaf = true;
            nt2_1.AddChild(t1_1);

            CSNode nt2_2 = new CSNode();
            nt2_2.STInfo = "nt2";
            nt2_2.IsFragmentRoot = false;
            nt1_2.AddChild(nt2_2);

            CSNode nt2_3 = new CSNode();
            nt2_3.STInfo = "nt2";
            nt2_3.IsFragmentRoot = false;
            nt2_3.CanHaveType = false;
            nt1_1.AddChild(nt2_3);

            CSNode t1_2 = new CSNode();
            t1_2.STInfo = "t1";
            t1_2.IsFragmentRoot = false;
            t1_2.CanHaveType = false;
            t1_1.IsTreeLeaf = true;
            nt2_3.AddChild(t1_2);

            return tree;
        }


        [Test]
        public void TestPCFGCalculation()
        {
            CSTree tree = CreateATree();

            var config = new Config();
            var modelParams = new ModelParams() { P = 0.05, ExcludeLeafsFromGeometric = false };
            config.ModelParams = modelParams;
            LabeledTreePCFGComposer pcfgComposer = new LabeledTreePCFGComposer(new List<LabeledTree>() { tree }, config);
            pcfgComposer.CalculateProbabilitiesLn();

            Assert.IsNotNull(pcfgComposer.Rules["rootnt"]);
            Assert.AreEqual(1, pcfgComposer.Rules["rootnt"].Count); // How many right hand sides (RHS) ?
            Assert.AreEqual(1, pcfgComposer.Rules["rootnt"]["nt1"].Count); // How many times did this RHS occur?
            Assert.AreEqual(Math.Log(1), pcfgComposer.Rules["rootnt"]["nt1"].ProbabilityLn); // What is the final probability of RHS?

            Assert.IsNotNull(pcfgComposer.Rules["nt1"]);
            Assert.AreEqual(2, pcfgComposer.Rules["nt1"].Count);
            Assert.AreEqual(1, pcfgComposer.Rules["nt1"]["nt1 nt2"].Count);
            Assert.AreEqual(1, pcfgComposer.Rules["nt1"]["nt2 nt2"].Count);
            Assert.AreEqual(Math.Log(0.5), pcfgComposer.Rules["nt1"]["nt1 nt2"].ProbabilityLn);
            Assert.AreEqual(Math.Log(0.5), pcfgComposer.Rules["nt1"]["nt2 nt2"].ProbabilityLn);


            Assert.IsNotNull(pcfgComposer.Rules["nt2"]);
            Assert.AreEqual(1, pcfgComposer.Rules["nt2"].Count);
            Assert.AreEqual(2, pcfgComposer.Rules["nt2"]["t1"].Count);
            Assert.AreEqual(Math.Log(1), pcfgComposer.Rules["nt2"]["t1"].ProbabilityLn);
        }


        [Test]
        public void TestFragmentProbabilitiesCalculation()
        {
            CSTree tree = CreateATree();

            var config = new Config();
            var modelParams = new ModelParams() { P = 0.05, ExcludeLeafsFromGeometric = false };
            config.ModelParams = modelParams;
            LabeledTreePCFGComposer pcfgComposer = new LabeledTreePCFGComposer(new List<LabeledTree>() { tree }, config);
            pcfgComposer.CalculateProbabilitiesLn();

            var nt1_1 = tree.Root.Children[0];

            var probabilityLn = pcfgComposer.FragmentProbabilityLnFromPCFGRules(nt1_1, out int fragmentSize);
            Assert.AreEqual(Math.Log(0.5), probabilityLn);
            Assert.AreEqual(3, fragmentSize); // The initial fragment root should not be counted, only its descendants (but descendants that are fragment roots should) 
        }

        [Test]
        public void TestFragmentProbabilitiesCalculationLeafsExcluded()
        {
            CSTree tree = CreateATree();

            var config = new Config();
            var modelParams = new ModelParams() { P = 0.05, ExcludeLeafsFromGeometric = true };
            config.ModelParams = modelParams;
            LabeledTreePCFGComposer pcfgComposer = new LabeledTreePCFGComposer(new List<LabeledTree>() { tree }, config);
            pcfgComposer.CalculateProbabilitiesLn();

            var nt1_2 = tree.Root.Children[0].Children[0];

            var probabilityLn = pcfgComposer.FragmentProbabilityLnFromPCFGRules(nt1_2, out int fragmentSize);
            Assert.AreEqual(Math.Log(0.5), probabilityLn);
            // The initial fragment root should not be counted, only its descendants (but descendants that are fragment roots should).
            // Tree leafs not counted in this test, because of ExcludeLeafsFromGeometric
            Assert.AreEqual(2, fragmentSize);
        }
    }
}
