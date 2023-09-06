using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RoseLibML.CS;
using RoseLibML.Core;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibMLTests.AlgorithmTests.SupportingClasses;

namespace RoseLibMLTests.AlgorithmTests
{
    class ConflictCheckingTests
    {
        // nt - non-terminal, t - terminal, fr (fragment root)
        //                      
        //                                                            -- t1_1
        //                                             -- nt1_3 (fr) - 
        //                              -- nt1_2 (fr) -
        //                -- nt1_1 (fr)                -- nt2_1 (fr) ( -- empty)
        //               -              -- nt2_2 (fr) -- t1_2
        //nt - nt1 (fr) -
        //               -- nt2_3 (fr) -- t1_3                  
        public CSTree CreateATreeWithConflictingSites()
        {
            CSTree tree = new CSTree();

            CSNode rootNt = new CSNode();
            rootNt.STInfo = "root-nt";
            rootNt.IsFragmentRoot = true;
            tree.Root = rootNt;

            CSNode nt1 = new CSNode();
            nt1.STInfo = "nt1";
            nt1.IsFragmentRoot = true;
            rootNt.AddChild(nt1);

            CSNode nt1_1 = new CSNode();
            nt1_1.STInfo = "nt1";
            nt1_1.IsFragmentRoot = true;
            nt1.AddChild(nt1_1);

            CSNode nt1_2 = new CSNode();
            nt1_2.STInfo = "nt1";
            nt1_2.IsFragmentRoot = true;
            nt1_1.AddChild(nt1_2);

            CSNode nt1_3 = new CSNode();
            nt1_3.STInfo = "nt1";
            nt1_3.IsFragmentRoot = true;
            nt1_2.AddChild(nt1_3);

            CSNode t1_1 = new CSNode();
            t1_1.STInfo = "t1";
            t1_1.IsFragmentRoot = false;
            t1_1.CanHaveType = false;
            nt1_3.AddChild(t1_1);

            CSNode nt2_1 = new CSNode();
            nt2_1.STInfo = "nt2";
            nt2_1.IsFragmentRoot = true;
            nt1_2.AddChild(nt2_1);

            CSNode nt2_2 = new CSNode();
            nt2_2.STInfo = "nt2";
            nt2_2.IsFragmentRoot = true;
            nt1_1.AddChild(nt2_2);

            CSNode t1_2 = new CSNode();
            t1_2.STInfo = "t1";
            t1_2.IsFragmentRoot = false;
            t1_2.CanHaveType = false;
            nt2_2.AddChild(t1_2);

            CSNode nt2_3 = new CSNode();
            nt2_3.STInfo = "nt2";
            nt2_3.IsFragmentRoot = true;
            nt1.AddChild(nt2_3);

            CSNode t1_3 = new CSNode();
            t1_3.STInfo = "t1";
            t1_3.IsFragmentRoot = false;
            t1_3.CanHaveType = false;
            nt2_3.AddChild(t1_3);

            return tree;
        }

        [Test]
        public void BookkeeperInitializationTest()
        {
            var testTree = CreateATreeWithConflictingSites();
            TBSampler sampler = new TBSampler(new ToCSWriter("onlyatest1", 1), new RoseLibML.Util.Config { ModelParams = new RoseLibML.Util.ModelParams() });
            sampler.BookKeeper = new ExtendedBookKeeper();
            sampler.Initialize(null, new LabeledTree[] { testTree }, true);

            var nt1Count = sampler.BookKeeper.GetRootCount("nt1");
            Assert.AreEqual(4, nt1Count);
        }

        [Test]
        public void ConflictIsDetectedTest()
        {
            // Create a tree with a suitable repetition
            // Create a scenario where cuts influenses types in a way suitable for conflict detection
            // Retrieve the type where these two conflicting are recorded
            // Invoke conflict detection and labeling
            var testTree = CreateATreeWithConflictingSites();
            TBSampler sampler = new TBSampler(new ToCSWriter("onlyatest2", 1), new RoseLibML.Util.Config { ModelParams = new RoseLibML.Util.ModelParams() });
            sampler.BookKeeper = new ExtendedBookKeeper();
            sampler.Initialize(null, new LabeledTree[] { testTree }, true);

            var typeNodesKVP = sampler
                                .BookKeeper
                                .TypeNodes
                                .Where(kvp => kvp.Value.Count == 2 && kvp.Value[0].STInfo == "nt1")
                                .First();


            var typeBlock = sampler.CreateTypeBlockAndAdjustCounts(typeNodesKVP.Value, 1);

            Assert.AreEqual(1, typeBlock.Count);
            Assert.AreEqual(2, sampler.BookKeeper.RootCounts["nt1"]);
        }

        /// <summary>
        /// Checks all the fragment nodes, to make sure that all do have LastModified set.
        /// Checks all the nodes surrounding the fragment to be sure these do not have LastModified set.
        /// </summary>
        [Test]
        public void LastModifiedWrittenForTheChosenFragment()
        {
            // Create a tree with a suitable repetition
            // Create a scenario where cuts influenses types in a way suitable for conflict detection
            // Retrieve the type where these two conflicting are recorded
            // Invoke conflict detection and labeling
            var testTree = CreateATreeWithConflictingSites();
            TBSampler sampler = new TBSampler(new ToCSWriter("onlyatest3", 1), new RoseLibML.Util.Config { ModelParams = new RoseLibML.Util.ModelParams() });
            sampler.BookKeeper = new ExtendedBookKeeper();
            sampler.Initialize(null, new LabeledTree[] { testTree }, true);

            var typeNodesKVP = sampler
                                .BookKeeper
                                .TypeNodes
                                .Where(kvp => kvp.Value.Count == 2 && kvp.Value[0].STInfo == "nt1")
                                .First();


            var typeBlock = sampler.CreateTypeBlockAndAdjustCounts(typeNodesKVP.Value, 1);

            var pivot = typeBlock[0];
            var fragmentNodes = pivot.GetAllFullFragmentNodes();

            var fullFragmentRoot = fragmentNodes[0];

            Assert.AreEqual(1, fullFragmentRoot.LastModified.iteration);
            if (fullFragmentRoot.Parent != null)
            {
                Assert.AreEqual(-1, fullFragmentRoot.Parent.LastModified.iteration);
            }

            foreach (var fragmentNode in fragmentNodes)
            {
                Assert.AreEqual(1, fragmentNode.LastModified.iteration);
            }

            var fragmentLeaves = pivot.GetAllFullFragmentLeaves();
            foreach (var leaf in fragmentLeaves)
            {
                Assert.AreEqual(1, leaf.LastModified.iteration);
                foreach (var leafsChild in leaf.Children)
                {
                    Assert.AreEqual(-1, leafsChild.LastModified.iteration);
                }
            }
        }
    }
}
