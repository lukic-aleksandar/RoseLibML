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
    public class CompilationUnitNamespaceGenerationTests
    {
        // $"namespace {namespaceName} {{ internal class {className} {{ }} }} "

        [Test]
        public async Task TestAddingNamespaceWithClassToComposer()
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
                outputSnippet.Fragment = "namespace $namespaceName { internal class $className { } }";
                outputSnippet.MethodName = "AddVSClass";
                outputSnippet.RootNodeType = "NamespaceDeclarationSyntax";
                outputSnippet.Composer = "CompilationUnitComposer";
                outputSnippet.MethodParameters = new List<MethodParameter> { new MethodParameter() { Metavariable = "$namespaceName", Parameter = "namespaceName" }, new MethodParameter() { Metavariable = "$className", Parameter = "className" } };

                await csIdiomTransformer.Generate(new List<OutputSnippet> { outputSnippet });

                Assert.Pass();
            }
        }
    }
}
