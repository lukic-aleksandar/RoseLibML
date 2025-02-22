using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transformer.Model;
using Transformer;

namespace TransformerTests
{
    public class IRepositoryGenerationTests
    {

        [Test]
        public async Task TestCreatingEmptyIRepository()
        {
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestFiles/knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                KnowledgeBase? knowledgeBase = JToken.ReadFrom(reader).ToObject<KnowledgeBase>();

                if (knowledgeBase == null)
                {
                    Assert.Fail();
                }

                CSIdiomTransformer csIdiomTransformer = new CSIdiomTransformer(knowledgeBase);
                OutputSnippet outputSnippet = new OutputSnippet();
                outputSnippet.Fragment = "namespace RentApp.Persistance.Repository { public interface $repositoryName : IRepository<$type, int> { } }";
                outputSnippet.MethodName = "AddIRepositoryBasis";
                outputSnippet.RootNodeType = "NamespaceDeclarationSyntax";
                outputSnippet.Composer = "CompilationUnitComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { 
                    new MethodParameter() { Metavariable = "$repositoryName", Parameter = "repositoryName" },
                    new MethodParameter() { Metavariable = "$type", Parameter = "type" } 
                };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }

        [Test]
        public async Task TestAddingGetAllMethod()
        {
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestFiles/knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                KnowledgeBase? knowledgeBase = JToken.ReadFrom(reader).ToObject<KnowledgeBase>();

                if (knowledgeBase == null)
                {
                    Assert.Fail();
                }

                CSIdiomTransformer csIdiomTransformer = new CSIdiomTransformer(knowledgeBase);
                OutputSnippet outputSnippet = new OutputSnippet();
                outputSnippet.Fragment = "IEnumerable<VehicleType> GetAll(int pageIndex, int pageSize);";
                outputSnippet.MethodName = "AddGetAllMethod";
                outputSnippet.RootNodeType = "MethodDeclarationSyntax";
                outputSnippet.Composer = "InterfaceComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> {
                    //new MethodParameter() { Metavariable = "$methodName", Parameter = "methodName" },
                    //new MethodParameter() { Metavariable = "$methodParameter", Parameter = "methodParameter" }
                };
                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }

    }
}
