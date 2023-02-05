using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using RoseLibML;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Text;
using RoseLibML.Util;
using System.IO;

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

        [Test]
        public void TestParentChildRelationshipCorrection()
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

            Assert.True(CheckIfRelationshipsAreOK(root));

        }

        /// <summary>
        /// This is the output of the binarization, explained
        /// (8875 - Method
        ///     (8343) - 'public' (a token, attached to the method)
        ///     (8347) - 'static' (a token, attached to the method)
        ///     (8621 (8304)  ) - (PredefinedType (bool)), a first child of the method
        ///     (B_8875 - a binarization node
        ///         (IdentifierToken (DidSucceed)  ) - a name of the method, first child of the binarization node
        ///         (B_8875 - a binarization node 
        ///             (8906 - ParametersList -  a first child of the binarization node
        ///                 (8200) - '(', a token attached to the method
        ///                 (8908 (8616 (IdentifierToken (int)  )  ) (IdentifierToken (repeatTimes)  )  ) - a first successive child of ParametersList
        ///                 (8216) - ',', a token attached to the method
        ///                 (8908 (8616 (IdentifierToken (BigInteger)  )  ) (IdentifierToken (previousResult)  )  ) - a second, but first successive child of ParametersList
        ///                 (8201)  ) - ')', a token attached to the method
        ///             (8792 - Block, the method's body
        ///                 (8205) - '{', a token attached to the body
        ///                 (8206)  )  )  )  )  - '}',a token attached to the body
        /// </summary>
        [Test]
        public void TestCSMethodBinarization()
        {
            CSNodeCreator csNodeCreator = new CSNodeCreator();
            var method = CreateMethod();
            CSNode csNode = csNodeCreator.CreateNode(method);

            LabeledTreeTransformations.Binarize(csNode, new CSNodeCreator());
            
            Assert.AreEqual(csNode.GetFragmentString(), "(8875 (8343) (8347) (8621 (8304)  ) (B_8875 (IdentifierToken (DidSucceed)  ) (B_8875 (8906 (8200) (8908 (8616 (int)  ) (IdentifierToken (repeatTimes)  )  ) (8216) (8908 (8616 (BigInteger)  ) (IdentifierToken (previousResult)  )  ) (8201)  ) (8792 (8205) (8206)  )  )  )  ) ");
        }

        [Test]
        public void TestClassBinarization() 
        {

            var path = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles\\TestFile.cs");
            var fileInfo = new FileInfo(path);
            var tree = CSTreeCreator.CreateTree(fileInfo, null);

            Assert.True(true);
        }

        public bool CheckIfRelationshipsAreOK(LabeledNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.Parent != node)
                {
                    return false;
                }
                if (!CheckIfRelationshipsAreOK(child))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Produces a method looking like this:
        /// public static bool DidSucceed(int repeatTimes, BigInteger previousResult)
        ///{
        ///}
        ///
        /// Treebank representation:
        /// (8875 
        ///     (8343) 
        ///     (8347) 
        ///     (8621 (8304)  ) 
        ///     (IdentifierToken (DidSucceed)  ) 
        ///     (8906 (8200) 
        ///           (8908 (8616 (IdentifierToken (int)  )  ) (IdentifierToken (repeatTimes)  )  ) 
        ///           (8216) 
        ///           (8908 (8616 (IdentifierToken (BigInteger)  )  ) (IdentifierToken (previousResult)  )  ) 
        ///           (8201)  ) 
        ///     (8792 (8205) (8206)  )  )
        /// </summary>
        /// <returns>Method syntax node</returns>
        public SyntaxNode CreateMethod()
        {
            SyntaxTokenList modifiers = new SyntaxTokenList();

            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            TypeSyntax returnType = SyntaxFactory.ParseTypeName("bool");
            var method = SyntaxFactory.MethodDeclaration(returnType, "DidSucceed");
            method = method.WithModifiers(modifiers);


            var @params = SyntaxFactory.ParameterList();
            
            var paramType1 = SyntaxFactory.IdentifierName("int");
            var paramName1 = SyntaxFactory.Identifier("repeatTimes");
            var paramSyntax1 = SyntaxFactory
                .Parameter(new SyntaxList<AttributeListSyntax>(), SyntaxFactory.TokenList(), paramType1, paramName1, null);
            @params = @params.AddParameters(paramSyntax1);

            var paramType2 = SyntaxFactory.IdentifierName("BigInteger");
            var paramName2 = SyntaxFactory.Identifier("previousResult");
            var paramSyntax2 = SyntaxFactory
                .Parameter(new SyntaxList<AttributeListSyntax>(), SyntaxFactory.TokenList(), paramType2, paramName2, null);
            @params = @params.AddParameters(paramSyntax2);

            @params = @params.NormalizeWhitespace();
            method = method.WithParameterList(@params);

            method = method.WithBody(SyntaxFactory.Block());
            method = method.NormalizeWhitespace();

            //var methodAsAString = method.ToFullString();
            return method;
        }
    }
}
