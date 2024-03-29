﻿using RoseLibML.Core;
using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoseLibMLTests.AlgorithmTests.SupportingClasses
{
    public class ExtendedBookKeeper : BookKeeper
    {
        public override void AddNodeType(LabeledNodeType givenType, LabeledNode node)
        {
            var trueType = LabeledNode.GetType(node);
            if (trueType.FullFragment != givenType.FullFragment ||
                    trueType.Part1Fragment != givenType.Part1Fragment ||
                    trueType.Part2Fragment != givenType.Part2Fragment)
            {
                throw new ArgumentException("Given type does not equal true type.");
            }

            if (!UsedTypes.ContainsKey(givenType.GetTypeHash()))
            {
                TypeNodes.Add(givenType, new List<LabeledNode>(10));
                UsedTypes.Add(givenType.GetTypeHash(), givenType);
                node.Type = givenType;
            }
            else
            {
                node.Type = UsedTypes[givenType.GetTypeHash()];
            }

            TypeNodes[node.Type].Add(node);
        }
    }
}
