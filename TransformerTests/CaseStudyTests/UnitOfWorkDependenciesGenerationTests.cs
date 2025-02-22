using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Transformer;
using Transformer.Model;

namespace TransformerTests
{
    public class UnitOfWorkDependenciesGenerationTests
    {

        [Test]
        public async Task TestAddingDependencyToClassComposer()
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
                outputSnippet.Fragment = "[Dependency] public $iRepositoryName $name { get; set; }";
                outputSnippet.MethodName = "AddUoWDependency";
                outputSnippet.RootNodeType = "PropertyDeclarationSyntax";
                outputSnippet.Composer = "ClassComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> 
                { 
                    new MethodParameter() { Metavariable = "$iRepositoryName", Parameter = "iRepositoryName" },
                    new MethodParameter() { Metavariable = "$name", Parameter = "name" } 
                };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}