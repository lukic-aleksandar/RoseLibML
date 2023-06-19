using System.Collections.Generic;
using System.Threading.Tasks;
using Transformer.Model;

namespace Transformer
{
    interface IdiomTransformer
    {
        Task Generate(List<OutputSnippet> outputSnippets);
        string TransformFragmentString(string fragment, List<MethodParameter> parameters, bool preview);
    }
}
