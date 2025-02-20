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
    public class MigrationClassGenerationTests
    {

        [Test]
        public async Task TestAddingMigrationClassToComposer()
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
                outputSnippet.Fragment = "namespace RentApp.Migrations { public partial class $migrationName : DbMigration { public override void Up() { } public override void Down() { } } }";
                outputSnippet.MethodName = "AddMigrationBasis";
                outputSnippet.RootNodeType = "NamespaceDeclarationSyntax";
                outputSnippet.Composer = "CompilationUnitComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { 
                    new MethodParameter() { Metavariable = "$migrationName", Parameter = "migrationName" },
                    //new MethodParameter() { Metavariable = "$className", Parameter = "className" } 
                };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}
