using NUnit.Framework;
using RoseLibML;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;

namespace Tests.FragmentExtensions
{
    public class GetFragmentStringTests
    {

        [Test]
        public void GetFragmentStringTestShort()
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

            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2)  ) ");
        }

        [Test]
        public void GetFragmentStringTestLong()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsFragmentRoot = false;
            root.AddChild(child1);

            CSNode grandChild1 = new CSNode();
            grandChild1.STInfo = "grandChild1";
            grandChild1.IsFragmentRoot = true;
            child1.AddChild(grandChild1);

            CSNode grandChild2 = new CSNode();
            grandChild2.STInfo = "grandChild2";
            grandChild2.IsFragmentRoot = true;
            child1.AddChild(grandChild2);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            Assert.AreEqual(root.GetFragmentString(), "(root (child1 (grandChild1) (grandChild2)  ) (child2)  ) ");
        }

        [Test]
        public void GetFragmentStringTestType()
        {
            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsFragmentRoot = false;
            child1.CanHaveType = false;
            root.AddChild(child1);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsFragmentRoot = true;
            root.AddChild(child2);

            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2)  ) ");
        }

        [Test]
        public void GetFragmentStringTestLong2()
        {
            CSNode beforeRoot = new CSNode();
            beforeRoot.STInfo = "root";
            beforeRoot.IsFragmentRoot = true;

            CSNode root = new CSNode();
            root.STInfo = "root";
            root.IsFragmentRoot = true;
            beforeRoot.AddChild(root);

            CSNode child1 = new CSNode();
            child1.STInfo = "child1";
            child1.IsFragmentRoot = true;
            root.AddChild(child1);

            CSNode grandChild1 = new CSNode();
            grandChild1.STInfo = "grandChild1";
            grandChild1.IsFragmentRoot = true;
            child1.AddChild(grandChild1);

            CSNode grandChild2 = new CSNode();
            grandChild2.STInfo = "grandChild2";
            grandChild2.IsFragmentRoot = true;
            child1.AddChild(grandChild2);

            CSNode child2 = new CSNode();
            child2.STInfo = "child2";
            child2.IsFragmentRoot = false;
            child2.CanHaveType = false;
            root.AddChild(child2);

            Assert.AreEqual(root.GetFragmentString(), "(root (child1) (child2)  ) ");
        }
    }
}