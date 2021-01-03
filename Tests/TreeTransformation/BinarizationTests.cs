using NUnit.Framework;
using RoseLibML;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.TreeTransformation
{
    class BinarizationTests
    {
        [Test]
        public void NoLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (B_root (child2) (B_root (child3) (child4)  )  )  ) ");
        }
        [Test]
        public void NoLeafsTwoBTypesBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = false;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            CSNode grandchild1 = new CSNode();
            grandchild1.STInfo = "grandchild1";
            grandchild1.IsFragmentRoot = true;
            child3.AddChild(grandchild1);

            CSNode grandchild2 = new CSNode();
            grandchild2.STInfo = "grandchild2";
            grandchild2.IsFragmentRoot = true;
            child3.AddChild(grandchild2);

            CSNode grandchild3 = new CSNode();
            grandchild3.STInfo = "grandchild3";
            grandchild3.IsFragmentRoot = true;
            child3.AddChild(grandchild3);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (B_root (child2) (B_root (child3 (grandchild1) (B_child3 (grandchild2) (grandchild3)  )  ) (child4)  )  )  ) ");
        }

        [Test]
        public void AllLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsTreeLeaf = true;
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsTreeLeaf = true;
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsTreeLeaf = true;
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsTreeLeaf = true;
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2) (child3) (child4)  ) ");
        }

        [Test]
        public void FirstLeafsThen2NonLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsTreeLeaf = true;
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsTreeLeaf = true;
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2) (child3) (child4)  ) ");
        }

        [Test]
        public void FirstLeafsThenXNonLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsTreeLeaf = true;
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsTreeLeaf = true;
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            CSNode child5 = new CSNode();
            child5.STInfo = "child5";
            child5.IsFragmentRoot = true;
            root.AddChild(child5);

            CSNode child6 = new CSNode();
            child6.STInfo = "child6";
            child6.IsFragmentRoot = true;
            root.AddChild(child6);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2) (child3) (B_root (child4) (B_root (child5) (child6)  )  )  ) ");
        }

        [Test]
        public void FirstLeafsThenXNonLeafsThenLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsTreeLeaf = true;
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsTreeLeaf = true;
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            CSNode child5 = new CSNode();
            child5.STInfo = "child5";
            child5.IsFragmentRoot = true;
            root.AddChild(child5);

            CSNode child6 = new CSNode();
            child6.STInfo = "child6";
            child6.IsFragmentRoot = true;
            child6.IsTreeLeaf = true;
            root.AddChild(child6);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2) (child3) (B_root (child4) (child5)  ) (child6)  ) ");
        }

        [Test]
        public void FirstLeafsThenXNonLeafsThenLeafsThenXNonLeafsBinarizationTest()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsTreeLeaf = true;
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsTreeLeaf = true;
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            CSNode child3 = new CSNode();
            child3.STInfo = "child3";
            child3.IsFragmentRoot = true;
            root.AddChild(child3);

            CSNode child4 = new CSNode();
            child4.STInfo = "child4";
            child4.IsFragmentRoot = true;
            root.AddChild(child4);

            CSNode child5 = new CSNode();
            child5.STInfo = "child5";
            child5.IsFragmentRoot = true;
            root.AddChild(child5);

            CSNode child6 = new CSNode();
            child6.STInfo = "child6";
            child6.IsFragmentRoot = true;
            child6.IsTreeLeaf = true;
            root.AddChild(child6);

            CSNode child7 = new CSNode();
            child7.STInfo = "child7";
            child7.IsFragmentRoot = true;
            root.AddChild(child7);

            CSNode child8 = new CSNode();
            child8.STInfo = "child8";
            child8.IsFragmentRoot = true;
            root.AddChild(child8);

            CSNode child9 = new CSNode();
            child9.STInfo = "child9";
            child9.IsFragmentRoot = true;
            root.AddChild(child9);

            LabeledTreeTransformations.Binarize(root, new CSNodeCreator());
            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2) (child3) (B_root (child4) (child5)  ) (child6) (child7) (B_root (child8) (child9)  )  ) ");
        }
    }
}
