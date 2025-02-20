using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Transformer;
using Transformer.Model;

namespace TransformerTests
{
    public class EFDbSetGenerationTests
    {

        [Test]
        public async Task TestAddingDbSetToClassComposer()
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
                outputSnippet.Fragment = "public DbSet<$type> $setName { get; set; }";
                outputSnippet.MethodName = "AddDBSet";
                outputSnippet.RootNodeType = "PropertyDeclarationSyntax";
                outputSnippet.Composer = "ClassComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { new MethodParameter() { Metavariable = "$type", Parameter = "type" }, new MethodParameter() { Metavariable = "$setName", Parameter = "setName" } };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}