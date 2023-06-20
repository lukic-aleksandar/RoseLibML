using System.Collections.Generic;

namespace Transformer.Model
{
    public class KnowledgeBase
    {
        public string RoseLibPath { get; set; }
        public string ComposersPath { get; set; }
        public Dictionary<string, List<string>> RootTypeToComposerMapping { get; set; }
        public Dictionary<string, ComposerInformation> ComposerInformationMapping { get; set; }
    }

}
