using System.Collections.Generic;

namespace RoseLibML.Transformer.Templates
{
    public partial class MethodComposerTemplate
    {
        private readonly string composer;
        private readonly string fragment;
        private readonly string methodName;
        private readonly List<string> methodParameters;

        public MethodComposerTemplate(string composer, string fragment, string methodName, List<string> methodParameters)
        {
            this.composer = composer;
            this.fragment = fragment;
            this.methodName = methodName;
            this.methodParameters = methodParameters;
        }
    }

    public partial class ComposerTemplate
    {
        private readonly string composer;
        private readonly string node;
        private readonly string rootCSType;
        private readonly string fragment;
        private readonly string methodName;
        private readonly List<string> methodParameters;

        public ComposerTemplate(string composer, string node, string rootCSType, string fragment, string methodName, List<string> methodParameters)
        {
            this.composer = composer;
            this.node = node;
            this.rootCSType = rootCSType;
            this.fragment = fragment;
            this.methodName = methodName;
            this.methodParameters = methodParameters;
        }
    }
}
