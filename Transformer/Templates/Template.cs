using System.CodeDom.Compiler;
using System.CodeDom;

namespace Transformer.Templates
{
    public partial class BaseFileTemplate
    {
        private readonly string composer;

        public BaseFileTemplate(string composer)
        {
            this.composer = composer;
        }
    }

    public partial class BlockComposerTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string composerNode;
        private readonly string rootNodeType;
        
        public BlockComposerTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }

    public partial class MemberComposerMethodTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string rootNodeType;
        private readonly string composerNode;

        public MemberComposerMethodTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }

    public partial class NamespaceComposerMethodTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string rootNodeType;
        private readonly string composerNode;

        public NamespaceComposerMethodTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }

    public partial class CompilationUnitComposerMethodTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string rootNodeType;
        private readonly string composerNode;

        public CompilationUnitComposerMethodTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }

}
