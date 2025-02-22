using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Transformer;
using Transformer.Model;

namespace TransformerTests
{
    public class ControllerGenerationTests
    {
        [Test]
        public async Task TestAddingControllerClassToComposer()
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
                outputSnippet.Fragment = "namespace RentApp.Controllers { public class $controllerName : ApiController { private UnitOfWork db; public $controllerName(IUnitOfWork context) { db = context; } } }";
                outputSnippet.MethodName = "AddControllerBasis";
                outputSnippet.RootNodeType = "NamespaceDeclarationSyntax";
                outputSnippet.Composer = "CompilationUnitComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> {
                    new MethodParameter() { Metavariable = "$controllerName", Parameter = "controllerName" },
                };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }


        [Test]
        public async Task TestAddingPutMethodClassComposer()
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
                outputSnippet.Fragment = "[ResponseType(typeof(void))] public IHttpActionResult $methodName(int id, $methodParameter) { if(!ModelState.IsValid) { return BadRequest(ModelState); } }";
                outputSnippet.MethodName = "AddPutMethod";
                outputSnippet.RootNodeType = "MethodDeclarationSyntax";
                outputSnippet.Composer = "ClassComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> {
                    new MethodParameter() { Metavariable = "$methodName", Parameter = "methodName" },
                    new MethodParameter() { Metavariable = "$methodParameter", Parameter = "methodParameter" }
                };
                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}