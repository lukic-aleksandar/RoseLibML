using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataPreprocessor
{
    class CommentsUsingsAndRegionRemover : CSharpSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.EndOfDocumentationCommentToken:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.RegionDirectiveTrivia:
                case SyntaxKind.EndRegionDirectiveTrivia:
                    return default; // new SyntaxTrivia()  // if C# <= 7.0
                default:
                    return trivia;
            }
        }

        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            return null;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {

            var attributeLists = SortAttributeLists(node.AttributeLists);

            node = node.WithAttributeLists(attributeLists);
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var attributeLists = SortAttributeLists(node.AttributeLists);

            node = node.WithAttributeLists(attributeLists);
            return base.VisitMethodDeclaration(node);
        }


        public SyntaxList<AttributeListSyntax> SortAttributeLists(SyntaxList<AttributeListSyntax> attributeLists)
        {
            var asList = attributeLists.ToList();
            asList.Sort((a, b) => a.ToString().CompareTo(b.ToString()));


            return new SyntaxList<AttributeListSyntax>().AddRange(asList);
        }

    }
}
