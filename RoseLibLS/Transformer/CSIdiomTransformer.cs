using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using RoseLibLS.Transformer.Templates;
using Serilog;

namespace RoseLibLS.Transformer
{
    class CSIdiomTransformer : IdiomTransformer
    {
        public KnowledgeBase KnowledgeBase { get; set; }

        private const string folderName = "ComposersGenerated";

        public CSIdiomTransformer(KnowledgeBase knowledgeBase)
        {
            KnowledgeBase = knowledgeBase;
        }

        public async Task<bool> Generate(List<OutputSnippet> outputSnippets)
        {
            Dictionary<string, List<OutputSnippet>> composerSnippets = GroupSnippetsByComposers(outputSnippets);

            var msWorkspace = MSBuildWorkspace.Create();
            var solution = await msWorkspace.OpenSolutionAsync(KnowledgeBase.RoseLibPath);

            foreach (KeyValuePair<string, List<OutputSnippet>> cs in composerSnippets)
            {
                var document = await GenerateComposer(solution, cs.Key, cs.Value);

                solution = document.Project.Solution;
            }

            return msWorkspace.TryApplyChanges(solution);
        }

        public string TransformFragmentString(string fragment, List<MethodParameter> parameters, bool preview = false)
        {
            string transformedFragment = fragment;

            if (!preview)
            {
                // replace curly brackets
                string openBracketsPattern = @"{(?=([^""]*""[^""]*"")*[^""]*$)";
                transformedFragment = Regex.Replace(transformedFragment, openBracketsPattern, "{{");

                string closeBracketsPattern = @"}(?=([^""]*""[^""]*"")*[^""]*$)";
                transformedFragment = Regex.Replace(transformedFragment, closeBracketsPattern, "}}");
            }

            // replace metavariables in fragment with parameters
            foreach (var parameter in parameters)
            {
                int position = transformedFragment.IndexOf(parameter.Metavariable);
                if (position < 0)
                {
                    continue;
                }

                string paramString = preview ? parameter.Parameter : "{" + parameter.Parameter + "}";

                transformedFragment = transformedFragment.Substring(0, position) + paramString + transformedFragment.Substring(position + parameter.Metavariable.Length);
            }

            return transformedFragment;
        }

        private Dictionary<string, List<OutputSnippet>> GroupSnippetsByComposers(List<OutputSnippet> outputSnippets)
        {
            Dictionary<string, List<OutputSnippet>> composerSnippets = new Dictionary<string, List<OutputSnippet>>();

            foreach (var snippet in outputSnippets)
            {
                if (!KnowledgeBase.ComposerInformationMapping.ContainsKey(snippet.Composer))
                {
                    continue;
                }

                if (!composerSnippets.ContainsKey(snippet.Composer))
                {
                    composerSnippets[snippet.Composer] = new List<OutputSnippet>();
                }

                composerSnippets[snippet.Composer].Add(snippet);
            }

            return composerSnippets;
        }

        private async Task<Document> GenerateComposer(Solution solution, string composer, List<OutputSnippet> outputSnippets)
        {
            var composerInfo = KnowledgeBase.ComposerInformationMapping[composer];

            List<MethodDeclarationSyntax> methods = GetMethodsFromOutputSnippets(outputSnippets);

            // check if the document for the composer already exists
            var document = solution.Projects.Select(pr => pr.Documents.Where(d => d.Name == composerInfo.FileName && d.Folders.Contains(folderName))).Single();
            if (!document.Any())
            {
                Log.Logger.Debug($"Generate Command Handler | Create a new file for the {composer}");

                // read the template for the composer file
                BaseFileTemplate fileTemplate = new BaseFileTemplate(composer);
                string fileContent = fileTemplate.TransformText();

                var compilationUnit = await CSharpSyntaxTree.ParseText(fileContent).GetRootAsync();

                var newCompilationUnit = AddMethodsToComposer(composer, compilationUnit as CompilationUnitSyntax, methods);

                return solution.Projects.Single().AddDocument(composerInfo.FileName, newCompilationUnit, new List<string> { "RoseLib", folderName });
            }
            else
            {
                Log.Logger.Debug($"Generate Command Handler | Editing an existing file for the {composer}");

                var compilationUnit = await document.Single().GetSyntaxRootAsync();

                var newCompilationUnit = AddMethodsToComposer(composer, compilationUnit as CompilationUnitSyntax, methods);

                return document.Single().WithSyntaxRoot(newCompilationUnit);
            }
        }

        private CompilationUnitSyntax AddMethodsToComposer(string composer, CompilationUnitSyntax oldCompilationUnit, List<MethodDeclarationSyntax> methods)
        {
            var classDeclaration = oldCompilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Where(cl => cl.Identifier.ToString() == composer).FirstOrDefault();

            var newClassDeclaration = classDeclaration.AddMembers(methods.ToArray());

            var newCompilationUnit = oldCompilationUnit.ReplaceNode(classDeclaration, newClassDeclaration);

            return newCompilationUnit.NormalizeWhitespace();
        }

        private List<MethodDeclarationSyntax> GetMethodsFromOutputSnippets(List<OutputSnippet> outputSnippets)
        {
            List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();

            foreach (var snippet in outputSnippets)
            {
                if (!KnowledgeBase.ComposerInformationMapping.ContainsKey(snippet.Composer))
                {
                    continue;
                }

                // set up method declaration syntax
                TypeSyntax returnType = SyntaxFactory.ParseTypeName(snippet.Composer);
                var method = SyntaxFactory.MethodDeclaration(returnType, $"Add{MakeFirstUppercase(snippet.MethodName)}")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                var methodParameters = SyntaxFactory.ParameterList();
                foreach (var param in snippet.MethodParameters)
                {
                    var type = SyntaxFactory.IdentifierName("string");
                    var name = SyntaxFactory.Identifier(param.Parameter);
                    var paramSyntax = SyntaxFactory
                        .Parameter(new SyntaxList<AttributeListSyntax>(), SyntaxFactory.TokenList(), type, name, null);
                    methodParameters = methodParameters.AddParameters(paramSyntax);
                }
                methodParameters = methodParameters.NormalizeWhitespace();
                method = method.WithParameterList(methodParameters);

                // replace metavariables in fragment with parameter names 
                string transformedFragment = TransformFragmentString(snippet.Fragment, snippet.MethodParameters);

                // create the body of the method
                string composerNode = KnowledgeBase.ComposerInformationMapping[snippet.Composer].Node;
                string methodBody = GetComposerMethodBody(snippet.Composer, transformedFragment, composerNode, snippet.RootNodeType);

                var bodyStatements = SyntaxFactory.ParseStatement(methodBody);
                method = method.WithBody(bodyStatements as BlockSyntax);

                method = method.NormalizeWhitespace();
                methods.Add(method);
            }

            return methods;
        }

        private string GetComposerMethodBody(string composer, string fragment, string composerNode, string rootNodeType)
        {
            string methodBody;
            string templateClass = "";

            // retrieve template name from the knowledge base
            if (KnowledgeBase.ComposerInformationMapping.ContainsKey(composer))
            {
                templateClass = KnowledgeBase.ComposerInformationMapping[composer].Template;
            }

            switch (templateClass)
            {
                case "MethodComposerTemplate":
                    var methodTemplate = new MethodComposerTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = methodTemplate.TransformText();
                    break;
                case "ComposerTemplate":
                    var otherTemplate = new ComposerTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = otherTemplate.TransformText();
                    break;
                default:
                    methodBody = "{}";
                    break;
            }

            return methodBody;
        }

        private string MakeFirstUppercase(string text)
        {
            return text.Substring(0, 1).ToUpper() + text.Substring(1);
        }
    }
}
