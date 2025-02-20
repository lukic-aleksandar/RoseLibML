using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Transformer;
using Transformer.Model;

namespace TransformerTests
{
    public class ConcurrencyExceptionHandlingGenerationTests
    {
        [Test]
        public async Task TestAddingConcurrencyExceptionHandlingToBlockComposer()
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
                outputSnippet.Fragment = "try { db.SaveChanges(); unitOfWork.Complete(); } catch(DbUpdateConcurrencyException) { if($findOneExpression) { return NotFound(); } else { throw; } }";
                outputSnippet.MethodName = "AddSaveWithConcurrencyHandling";
                outputSnippet.RootNodeType = "TryStatementSyntax";
                outputSnippet.Composer = "BlockComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { new MethodParameter() { Metavariable = "$findOneExpression", Parameter = "findOneExpression" } };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}