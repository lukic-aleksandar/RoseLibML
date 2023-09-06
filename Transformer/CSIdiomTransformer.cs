using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Transformer.Templates;
using Serilog;
using System.IO;
using System.Text;
using Transformer.Model;

namespace Transformer
{
    public class CSIdiomTransformer : IdiomTransformer
    {
        public KnowledgeBase KnowledgeBase { get; set; }

        private Dictionary<string, string> composerFiles;


        private const string folderName = "Generated";

        public CSIdiomTransformer(KnowledgeBase knowledgeBase)
        {
            composerFiles = new Dictionary<string, string>();
            KnowledgeBase = knowledgeBase;
            Init();
        }

        private void Init()
        {
            
            if (KnowledgeBase == null) { throw new Exception("Knowledge base must be provided."); }

            foreach (string filePath in Directory.EnumerateFiles(KnowledgeBase.RoseLibPath, "*.cs", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                composerFiles.Add(fileName, filePath);
            }
        }

        public async Task Generate(List<OutputSnippet> outputSnippets)
        {
            var generatedComposersPath =  Path.Combine(
                KnowledgeBase.RoseLibPath,
                KnowledgeBase.ComposersPath,
                folderName);

            var directoryInfo = Directory.CreateDirectory(generatedComposersPath);
            
            Dictionary<string, List<OutputSnippet>> composerSnippets = GroupSnippetsByComposers(outputSnippets);

            foreach (KeyValuePair<string, List<OutputSnippet>> cs in composerSnippets)
            {
                await GenerateComposer(directoryInfo, cs.Key, cs.Value);
            }

            return;
        }

        public string TransformFragmentString(string fragment, List<MethodParameter> parameters, bool preview = false)
        {
            string transformedFragment = fragment;

            if (!preview)
            {
                // replace curly brackets
                string openBracketsPattern = @"{";//@"{(?=([^""]*""[^""]*"")*[^""]*$)";
                transformedFragment = Regex.Replace(transformedFragment, openBracketsPattern, "{{");

                string closeBracketsPattern = @"}"; //@"}(?=([^""]*""[^""]*"")*[^""]*$)";
                transformedFragment = Regex.Replace(transformedFragment, closeBracketsPattern, "}}");
            }

            // replace metavariables in fragment with parameters
            foreach (var parameter in parameters)
            {
                int position = transformedFragment.IndexOf(parameter.Metavariable);

                while (position > -1)
                {
                    string paramString = preview ? parameter.Parameter : "{" + parameter.Parameter + "}";

                    transformedFragment = transformedFragment.Substring(0, position) + paramString + transformedFragment.Substring(position + parameter.Metavariable.Length);

                    position = transformedFragment.IndexOf(parameter.Metavariable);
                }
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

        private async Task GenerateComposer(DirectoryInfo generationDirecory, string composer, List<OutputSnippet> outputSnippets)
        {
            var composerInfo = KnowledgeBase.ComposerInformationMapping[composer];
            var composerFilePath = Path.Combine(generationDirecory.FullName, composerInfo.FileName);


            List<MethodDeclarationSyntax> methods = GetMethodsForOutputSnippets(outputSnippets);
            CompilationUnitSyntax? resultingCU = null;

            if (!composerFiles.ContainsKey(composerInfo.FileName))
            {
                // read the template for the composer file
                BaseFileTemplate fileTemplate = new BaseFileTemplate(composer);
                string fileContent = fileTemplate.TransformText();

                var compilationUnit = await CSharpSyntaxTree.ParseText(fileContent).GetRootAsync();
                resultingCU = AddMethodsToComposer(composer, (compilationUnit as CompilationUnitSyntax)!, methods);
            }
            else
            {
                using (var sr = new StreamReader(composerFilePath))
                {
                    var existingCode = sr.ReadToEnd();
                    var st = CSharpSyntaxTree.ParseText(existingCode);
                    var compilationUnit = st.GetRoot();
                    resultingCU = AddMethodsToComposer(composer, (compilationUnit as CompilationUnitSyntax)!, methods);
                }
            }

            using (var fileStream = File.Create(composerFilePath))
            {
                var utf8 = new UTF8Encoding();

                fileStream.Write(utf8.GetBytes(resultingCU.ToFullString()));
                fileStream.Flush();
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

        private List<MethodDeclarationSyntax> GetMethodsForOutputSnippets(List<OutputSnippet> outputSnippets)
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
                var method = SyntaxFactory.MethodDeclaration(returnType, $"{MakeFirstUppercase(snippet.MethodName)}")
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
                case "BlockComposerTemplate":
                    var bodyMethodTemplate = new BlockComposerTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = bodyMethodTemplate.TransformText();
                    break;
                case "ClassComposerTemplate":
                case "StructComposerTemplate":
                case "InterfaceComposerTemplate":
                    var memberMethodTemplate = new MemberComposerMethodTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = memberMethodTemplate.TransformText();
                    break;
                case "NamespaceComposerTemplate":
                    var namespaceMethodTemplate = new NamespaceComposerMethodTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = namespaceMethodTemplate.TransformText();
                    break;
                case "CompilationUnitTemplate":
                    var compilationUnitMethodTemplate = new CompilationUnitComposerMethodTemplate(composer, fragment, composerNode, rootNodeType);
                    methodBody = compilationUnitMethodTemplate.TransformText();
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
