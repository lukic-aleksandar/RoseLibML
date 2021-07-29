using System.Collections.Generic;

namespace RoseLibLS.Transformer
{
    class KnowledgeBase
    {
        public string RoseLibPath { get; set; }
        public Dictionary<string, List<string>> RootTypeToComposerMapping { get; set; }
        public Dictionary<string, ComposerInformation> ComposerInformationMapping { get; set; }
    }

    class ComposerInformation
    {
        public string Template { get; set; }
        public string Node { get; set; }
    }
}
