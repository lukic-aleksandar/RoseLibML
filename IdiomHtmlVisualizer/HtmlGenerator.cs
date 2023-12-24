using IdiomHtmlVisualizer.Model;
using Microsoft.CodeAnalysis;
using RoseLibML.CS;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdiomHtmlVisualizer
{
	public static class HtmlGenerator
	{
		private static ColorHelper colorHelper = new ColorHelper();

		public static void GenerateHTML(VisualizationData visualizationData)
		{
			CodeInHTMLTemplate template = new CodeInHTMLTemplate()
			{
				ColorHelper = colorHelper,
				VisualizationData = visualizationData,
				Tokens = visualizationData.SourceSyntaxTree.GetRoot().DescendantTokens()
			};

			using (var streamWriter = new StreamWriter(visualizationData.HtmlFileName))
			{
				var html = template.TransformText();
				streamWriter.Write(html);
			}
		}
	}
}
