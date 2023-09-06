using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Transformer;
using Transformer.Model;

namespace TransformerTests
{
    public class StatementGenerationTests
    {
        [Test]
        public async Task TestAddingIfWithConsoleLogToBlockComposer()
        {
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestFiles/knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                KnowledgeBase? knowledgeBase = JToken.ReadFrom(reader).ToObject<KnowledgeBase>();
            
                if(knowledgeBase == null)
                {
                    Assert.Fail();
                }

                CSIdiomTransformer csIdiomTransformer = new CSIdiomTransformer(knowledgeBase);
                OutputSnippet outputSnippet = new OutputSnippet();
                outputSnippet.Fragment = "if(4>3){ Console.WriteLine($text); }";
                outputSnippet.MethodName = "AddIf";
                outputSnippet.RootNodeType = "IfStatementSyntax";
                outputSnippet.Composer = "BlockComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { new MethodParameter() { Metavariable = "$text", Parameter = "text" } };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}