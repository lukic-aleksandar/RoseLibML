using DataPreprocessor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPreprocessor
{
    public class ProjectAnalyser
    {
        private const string FILE_EXTENSION = "*.cs";
        public const string IMPLICIT_BASE_CLASS = "object";

        // So, the key is a group - named by the base type name (there is a possibility that two types have 
        // a same name)
        // The value is a tuple of two: file path and class as a string
        public Dictionary<string, List<Tuple<FileInfo, string>>> ComponentGroups { get; set; }

        public ProjectAnalyser()
        {
            ComponentGroups = new Dictionary<string, List<Tuple<FileInfo, string>>>();
        }

        public Dictionary<string, List<Tuple<FileInfo, string>>> AnalyseProject(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            FileInfo[] files = directoryInfo.GetFiles(FILE_EXTENSION, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                AnalyseFileAndGroup(file);
            }

            var sortedComponentGroups = ComponentGroups.OrderBy(x => x.Value.Count()).ToDictionary(x => x.Key, x => x.Value);
            return ComponentGroups;
        }

        public Dictionary<string, List<Tuple<FileInfo, string>>> GetComponentGroupsWithoutBaseAndNonRepetative(int threshold)
        {
            var sortedComponentGroups = ComponentGroups
                .Where(x => x.Value.Count() > threshold)
                .Where(x => !x.Key.Equals(IMPLICIT_BASE_CLASS))
                .OrderBy(x => x.Value.Count())
                .ToDictionary(x => x.Key, x => x.Value);

            return sortedComponentGroups;
        }



        // Files with multiple classes are currently skipped
        // If it turns out that there are many such files, then consider doing something about it
        // The occurance of such a file is logged
        public void AnalyseFileAndGroup(FileInfo file)
        {
            try
            {
                var filePath = file.FullName;
                string fileText = File.ReadAllText(file.FullName);
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileText);

                ExtractClassesAndGroup(file, filePath, syntaxTree);
                ExtractInterfacesAndGroup(file, filePath, syntaxTree);
                
            }
            catch(Exception e)
            {
                Console.WriteLine($"WARNING: An error occured while processing a file. File path {file.FullName}. Error {e.StackTrace}");
            }
        }

        private void ExtractClassesAndGroup(FileInfo file, string filePath, SyntaxTree syntaxTree)
        {
            List<ClassDeclarationSyntax> classes = null;

            try
            {
                classes = ExtractClasses(syntaxTree);
            }
            catch (Exception)
            {
                Console.WriteLine("Did not manage to extract classes as there was an error, for file: " + filePath);
            }

            if (classes == null || classes.Count == 0)
            {
                return;
            }
            if (classes.Count > 1)
            {
                Console.WriteLine($"WARNING: Skipping a file with multiple classes. File path {filePath}");
                return;
            }

            var @class = classes.First();
            List<string> baseTypes = ExtractBaseTypes(@class.BaseList, @class.Identifier.Text);
            if (baseTypes.Count == 0)
            {
                AddToComponentGroup(file, @class, new List<string>() { IMPLICIT_BASE_CLASS });
            }
            else
            {
                AddToComponentGroup(file, @class, baseTypes);
            }
        }

        private void ExtractInterfacesAndGroup(FileInfo file, string filePath, SyntaxTree syntaxTree)
        {
            List<InterfaceDeclarationSyntax> interfaces = null;

            try
            { 
                interfaces = ExtractInterfaces(syntaxTree);
            }
            catch (Exception)
            {
                Console.WriteLine("Did not manage to extract interfaces as there was an error, for file: " + filePath);
            }

            if (interfaces == null || interfaces.Count == 0)
            {
                return;
            }
            if (interfaces.Count > 1)
            {
                Console.WriteLine($"WARNING: Skipping a file with multiple classes. File path {filePath}");
                return;
            }

            var @interface = interfaces.First();
            List<string> baseTypes = ExtractBaseTypes(@interface.BaseList, @interface.Identifier.Text);
            if (baseTypes.Count == 0)
            {
                AddToComponentGroup(file, @interface, new List<string>() { IMPLICIT_BASE_CLASS });
            }
            else
            {
                AddToComponentGroup(file, @interface, baseTypes);
            }
        }

        private object dictionaryLock = new object();
        private void AddToComponentGroup(FileInfo fileInfo,SyntaxNode component, List<string> baseTypes)
        {
            foreach (var baseType in baseTypes)
            {
                lock (dictionaryLock)
                {
                    ComponentGroups.TryGetValue(baseType, out var group);
                    if(group == null)
                    {
                        group = new List<Tuple<FileInfo, string>>();
                    }

                    var codeWithoutComments = new CommentsUsingsAndRegionRemover().Visit(component).ToString();

                    group.Add(new Tuple<FileInfo, string>(fileInfo, codeWithoutComments));
                    ComponentGroups[baseType] = group;
                }
            }
        }
        private List<ClassDeclarationSyntax> ExtractClasses(SyntaxTree syntaxTree)
        {
            CompilationUnitSyntax cus = syntaxTree.GetCompilationUnitRoot();
            if(cus.Members.Count == 0)
            {
                Console.WriteLine("Can't extract classes! Zero members of a Compilation Unit! Throwing an error anyways.");
            }
            return cus.Members[0]
                .DescendantNodes()
                .Where((member) => member is ClassDeclarationSyntax && member.Parent is NamespaceDeclarationSyntax)
                .Cast<ClassDeclarationSyntax>()
                .ToList();
        }

        private List<InterfaceDeclarationSyntax> ExtractInterfaces(SyntaxTree syntaxTree)
        {
            CompilationUnitSyntax cus = syntaxTree.GetCompilationUnitRoot();
            if (cus.Members.Count == 0)
            {
                Console.WriteLine("Can't extract interfaces! Zero members of a Compilation Unit! Throwing an error anyways.");
            }
            return cus.Members[0]
                .DescendantNodes()
                .Where((member) => member is InterfaceDeclarationSyntax && member.Parent is NamespaceDeclarationSyntax)
                .Cast<InterfaceDeclarationSyntax>()
                .ToList();
        }

        private List<string> ExtractBaseTypes(BaseListSyntax? baseList, string componentName)
        {
            List<string> baseTypes = new List<string>();

            if (baseList != null)
            {
                var unsupportedBaseList = baseList
                    .DescendantNodes()
                    .Where(node => node is PrimaryConstructorBaseTypeSyntax)
                    .Cast<PrimaryConstructorBaseTypeSyntax>()
                    .ToList();
                if(unsupportedBaseList != null && unsupportedBaseList.Count != 0)
                {
                    throw new Exception($"Unsupported base for class {componentName}");
                }

                var baseTypeList = baseList
                    .DescendantNodes()
                    .Where(node => node is SimpleBaseTypeSyntax)
                    .Cast<SimpleBaseTypeSyntax>()
                    .ToList();

                foreach (var baseType in baseTypeList)
                {
                    var genericName = baseType
                        .DescendantNodes()
                        .Where(node => node is GenericNameSyntax)
                        .Cast<GenericNameSyntax>().FirstOrDefault();


                    if (genericName != null)
                    {
                        baseTypes.Add(genericName.ToString());
                    }
                    else
                    {
                        var identifierName = baseType
                                                .DescendantNodes()
                                                .Where(node => node is IdentifierNameSyntax)
                                                .Cast<IdentifierNameSyntax>().FirstOrDefault();

                        if (identifierName != null)
                        {
 
                            baseTypes.Add(identifierName.ToString());
                            
                        }
                        else
                        {
                            throw new Exception($"Base type info could not be inferred. File {componentName}");
                        }
                    }

                }
            }

            return baseTypes;
        }
    }

    interface ID 
    {

    }

    interface ID2 : ID
    {

    }
}
