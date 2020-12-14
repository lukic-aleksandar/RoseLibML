using NUnit.Framework;
using RoseLibML;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.BookKeeperTests
{
    class AddNodeTypeTests
    {
        [Test]
        public void GetMD5HashCodeTest()
        {
            LabeledNodeType lnt1 = new LabeledNodeType();
            lnt1.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt1.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt1.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";


            LabeledNodeType lnt2 = new LabeledNodeType();
            lnt2.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt2.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt2.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";


            Assert.AreEqual(lnt1.GetMD5HashCode(), lnt2.GetMD5HashCode());
        }

        [Test]
        public void AddNodeRecyclesTypesTest()
        {
            LabeledNodeType lnt1 = new LabeledNodeType();
            lnt1.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt1.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt1.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";


            LabeledNodeType lnt2 = new LabeledNodeType();
            lnt2.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt2.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt2.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";

            LabeledNode node1 = new CSNode();
            LabeledNode node2 = new CSNode();


            BookKeeper bookKeeper = new BookKeeper();
            bookKeeper.AddNodeType(lnt1, node1);
            bookKeeper.AddNodeType(lnt2, node2);

            Assert.True(node1.Type == node2.Type);
            Assert.True(bookKeeper.TypeNodes.Count == 1);
        }

        [Test]
        public void UsedTypesDictionaryTest()
        {
            LabeledNodeType lnt1 = new LabeledNodeType();
            lnt1.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt1.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt1.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";


            LabeledNodeType lnt2 = new LabeledNodeType();
            lnt2.FullFragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";
            lnt2.Part1Fragment = "(8689 (8616 (IdentifierToken (Assert)  )  ) (BinTempNode)";
            lnt2.Part2Fragment = "(BinTempNode (8218) (8616 (IdentifierToken (AreEqual)  )  )  )  ) ";

            var hash1 = lnt1.GetMD5HashCode();
            var hash2 = lnt2.GetMD5HashCode();

            var base64Hash1 = System.Convert.ToBase64String(hash1);
            var base64Hash2 = System.Convert.ToBase64String(hash2);

            Assert.True(base64Hash1.GetHashCode() == base64Hash2.GetHashCode());
        }
    }
}
