using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoseLibLS.Transformer
{
    interface IdiomTransformer
    {
        Task<bool> Generate(List<OutputSnippet> outputSnippets);
        string TransformFragmentString(string fragment, List<MethodParameter> parameters, bool preview);
    }
}
