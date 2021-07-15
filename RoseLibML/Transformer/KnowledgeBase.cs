using System.Collections.Generic;

namespace RoseLibML.LanguageServer.Transformer
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
