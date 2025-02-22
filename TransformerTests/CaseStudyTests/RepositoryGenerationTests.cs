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
    public class RepositoryGenerationTests
    {

        [Test]
        public async Task TestAddingRepositoryToComposer()
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
                outputSnippet.Fragment = "namespace RentApp.Persistance.Repository { public class $repositoryName: Repository<$modelType, int>, $implementedRepository { protected RADBContext RADBContext { get { return context as RADBContext; } } public $repositoryName(DbContext context): base(context){ } } }";
                outputSnippet.MethodName = "AddRepositoryClass";
                outputSnippet.RootNodeType = "NamespaceDeclarationSyntax";
                outputSnippet.Composer = "CompilationUnitComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { 
                    new MethodParameter() { Metavariable = "$repositoryName", Parameter = "repositoryName" },
                    new MethodParameter() { Metavariable = "$modelType", Parameter = "modelType" },
                    new MethodParameter() { Metavariable = "$implementedRepository", Parameter = "implementedRepository" } 
                };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }

        [Test]
        public async Task TestAddingGetAllRepositoryMethodToClassComposer()
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
                outputSnippet.Fragment = "public IEnumerable<$modelType> GetAll(int pageIndex, int pageSize) { return RADBContext.$dbsetName.Skip((pageIndex - 1) * pageSize).Take(pageSize); }";
                outputSnippet.MethodName = "AddGetAllRepositoryMethod";
                outputSnippet.RootNodeType = "MethodDeclarationSyntax";
                outputSnippet.Composer = "ClassComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> {
                    new MethodParameter() { Metavariable = "$modelType", Parameter = "modelType" },
                    new MethodParameter() { Metavariable = "$dbsetName", Parameter = "dbsetName" }
                };
                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}
