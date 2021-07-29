﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using RoseLibLS.LanguageServer;
using RoseLibLS.Transformer.Templates;

namespace RoseLibLS.Transformer
{
    class IdiomTransformer
    {
        public KnowledgeBase KnowledgeBase { get; set; }

        private int metavariableCounter = 0;

        public IdiomTransformer(KnowledgeBase knowledgeBase)
        {
            KnowledgeBase = knowledgeBase;
        }

        public List<string> Generate(GenerateCommandArguments arguments)
        {
            List<string> outputSnippets = new List<string>();

            if (KnowledgeBase.RootTypeToComposerMapping.ContainsKey(arguments.RootCSType))
            {
                // get composers from the knowledge base
                List<string> composers = KnowledgeBase.RootTypeToComposerMapping[arguments.RootCSType];

                foreach (var composer in composers)
                {
                    if (!KnowledgeBase.ComposerInformationMapping.ContainsKey(composer))
                    {
                        continue;
                    }

                    string node = KnowledgeBase.ComposerInformationMapping[composer].Node;
                    string template = KnowledgeBase.ComposerInformationMapping[composer].Template;

                    // transform fragment string for template
                    string transformedFragment = TransformFragmentString(arguments.Fragment);

                    // generate method and save to a file
                    BaseTemplate templateObj = GetTemplate(template);
                    templateObj.Initialize(composer, node, arguments.RootCSType, transformedFragment, arguments.MethodName, arguments.MethodParameters);

                    string outputText = templateObj.TransformText();

                    string fileName = $"{composer}Extension_{arguments.MethodName}.cs";
                    File.WriteAllText(Path.Combine(KnowledgeBase.RoseLibPath, fileName), outputText);

                    // add saved file to the RoseLib project 
                    var project = LoadProject(Path.Combine(KnowledgeBase.RoseLibPath, @"RoseLibApp.csproj"));
                   /* project.ReevaluateIfNecessary();
                    project.AddItem("Compile", fileName);
                    project.Save();*/

                    outputSnippets.Add(outputText);
                }
            }

            return outputSnippets;
        }

        private BaseTemplate GetTemplate(string templateClassName)
        {
            BaseTemplate template;
            switch (templateClassName)
            {
                case "ComposerTemplate":
                    template = new ComposerTemplate();
                    break;
                case "MethodComposerTemplate":
                    template = new MethodComposerTemplate();
                    break;
                default:
                    template = new BaseTemplate();
                    break;
            }

            return template;
        }

        private string TransformFragmentString(string fragment)
        {
            string openBracketsPattern = @"{(?=([^""]*""[^""]*"")*[^""]*$)";
            fragment = Regex.Replace(fragment, openBracketsPattern, "{{");

            string closeBracketsPattern = @"}(?=([^""]*""[^""]*"")*[^""]*$)";
            fragment = Regex.Replace(fragment, closeBracketsPattern, "}}");

            // find metavariables and replace with placeholders
            // string starting with $ not in quotes
            string metavariablesPattern = @"\$[A-Za-z]+(?=([^""]*""[^""]*"")*[^""]*$)";
            fragment = Regex.Replace(fragment, metavariablesPattern, ReplaceMetavariable);

            metavariableCounter = 0;

            return fragment;
        }

        private string ReplaceMetavariable(Match m)
        {
            string replacementString = $"{{{metavariableCounter}}}";
            metavariableCounter++;

            return replacementString;
        }

        private Project LoadProject(string projectPath)
        {
            var project = ProjectCollection.GlobalProjectCollection.LoadedProjects.FirstOrDefault(pr => pr.FullPath == projectPath);
            if (project == null)
            {
                project = new Project(projectPath);
            }

            return project;
        }
    }
}