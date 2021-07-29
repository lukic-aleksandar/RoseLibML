using System.Collections.Generic;

namespace RoseLibLS.Transformer.Templates
{
    public partial class BaseTemplate
    {
        protected string composer;
        protected string node;
        protected string rootCSType;
        protected string fragment;
        protected string methodName;
        protected List<string> methodParameters;

        public BaseTemplate()
        {

        }

        public void Initialize(string composer, string node, string rootCSType, string fragment, string methodName, List<string> methodParameters)
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
