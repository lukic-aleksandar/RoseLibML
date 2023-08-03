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
    public class NamespaceMemberGenerationTests
    {
        [Test]
        public async Task TestAddingControllerToNamespaceComposer()
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
                outputSnippet.Fragment = "[RoutePrefix(\"api/$resourcePath$\")] public class $resourceName$Controller: ApiController { private $resourceName$Context _context; public $resourceName$Controller($resourceName$Context context){_context = context;}[Route(\"\")] public IEnumerable<$resourceName$> GetAll(){return _context.getAll();}}";
                outputSnippet.MethodName = "AddController";
                outputSnippet.RootNodeType = "ClassDeclarationSyntax";
                outputSnippet.Composer = "NamespaceComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { new MethodParameter() { Metavariable = "$resourceName$", Parameter = "resourceName" }, new MethodParameter() { Metavariable = "$resourcePath$", Parameter = "resourcePath" } };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}
