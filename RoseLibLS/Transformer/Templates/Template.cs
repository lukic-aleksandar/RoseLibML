namespace RoseLibLS.Transformer.Templates
{
    public partial class BaseFileTemplate
    {
        private readonly string composer;

        public BaseFileTemplate(string composer)
        {
            this.composer = composer;
        }
    }

    public partial class MethodComposerTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string composerNode;
        private readonly string rootNodeType;
        
        public MethodComposerTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }
    }

    public partial class ComposerTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string composerNode;
        private readonly string rootNodeType;

        public ComposerTemplate(string composer, string fragment, string composerNode, string rootNodeType)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.composerNode = composerNode;
            this.rootNodeType = rootNodeType;
        }
    }
}
