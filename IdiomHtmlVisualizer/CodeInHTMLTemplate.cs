﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace IdiomHtmlVisualizer
{
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Collections.Generic;
    using IdiomHtmlVisualizer;
    using IdiomHtmlVisualizer.Model;
    using Microsoft.CodeAnalysis;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class CodeInHTMLTemplate : CodeInHTMLTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n\n<html>\n<head>\n\t<style>span {white-space: pre ; font-family:\'Courier New\';}</sty" +
                    "le>\n\t<style>\n\t\t#tooltip {\n\t\t  background-color: #333;\n\t\t  color: white;\n\t\t  padd" +
                    "ing: 5px 10px;\n\t\t  border-radius: 4px;\n\t\t  font-size: 13px;\n\t\t  display: none;\n\t" +
                    "\t}\n\n\t\t#arrow,\n\t\t#arrow::before {\n\t\t\tposition: absolute;\n\t\t\twidth: 8px;\n\t\t\theight" +
                    ": 8px;\n\t\t\tbackground: inherit;\n\t\t}\n\n\t\t#arrow {\n\t\t\tvisibility: hidden;\n\t\t}\n\n\t\t#ar" +
                    "row::before {\n\t\t\tvisibility: visible;\n\t\t\tcontent: \'\';\n\t\t\ttransform: rotate(45deg" +
                    ");\n\t\t}\n\n\t\t#tooltip[data-popper-placement^=\'top\'] > #arrow {\n\t\t\tbottom: -4px;\n\t\t}" +
                    "\n\n\t\t#tooltip[data-popper-placement^=\'bottom\'] > #arrow {\n\t\t\ttop: -4px;\n\t\t}\n\n\t\t#t" +
                    "ooltip[data-popper-placement^=\'left\'] > #arrow {\n\t\t\tright: -4px;\n\t\t}\n\n\t\t#tooltip" +
                    "[data-popper-placement^=\'right\'] > #arrow {\n\t\t\tleft: -4px;\n\t\t}\n\n\t\t#tooltip[data-" +
                    "show] {\n\t\t\tdisplay: block;\n\t\t}\n\t</style>\n\t<style>\n\n\t\t#details-popup {\n\t\t  positi" +
                    "on: fixed;\n\t\t  top: 0;\n\t\t  bottom: 0;\n\t\t  left: 0;\n\t\t  right: 0;\n\t\t  background:" +
                    " rgba(0, 0, 0, 0.7);\n\t\t  transition: opacity 500ms;\n\t\t  visibility: hidden;\n\t\t  " +
                    "opacity: 0;\n\t\t  overflow-y:scroll;\n\t\t}\n\n\t\t#details-popup-content {\n\t\t  margin: 7" +
                    "0px auto;\n\t\t  padding: 20px;\n\t\t  background: #fff;\n\t\t  border-radius: 5px;\n\t\t  w" +
                    "idth: 30%;\n\t\t  position: relative;\n\t\t  transition: all 5s ease-in-out;\n\t\t}\n\n\t\t#d" +
                    "etails-popup-content h2 {\n\t\t  margin: 0;\n\t\t  color: #333;\n\t\t  font-family: Tahom" +
                    "a, Arial, sans-serif;\n\t\t}\n\t\t#details-popup-close {\n\t\t  position: absolute;\n\t\t  t" +
                    "op: 20px;\n\t\t  right: 30px;\n\t\t  transition: all 200ms;\n\t\t  font-size: 30px;\n\t\t  f" +
                    "ont-weight: bold;\n\t\t  text-decoration: none;\n\t\t  color: #555;\n\t\t}\n\t\t#details-pop" +
                    "up-close:hover {\n\t\t  color: #111;\n\t\t  cursor: pointer;\n\t\t}\n\t\t#details-popup-cont" +
                    "ent-body {\n\t\t  max-height: 30%;\n\t\t  overflow: auto;\n\t\t  font-family: Tahoma, Ari" +
                    "al, sans-serif;\n\t\t  white-space: pre;\n\t\t}\n\n\t\t#details-popup-content-text {\n\t\t  m" +
                    "argin: 0;\n\t\t}\n\n\t\t@media screen and (max-width: 700px){\n\t\t  #details-popup-conten" +
                    "t{\n\t\t\twidth: 70%;\n\t\t  }\n\t\t}\n\t</style>\n\t<script>\n\t\tfunction highlight(x, sameIdio" +
                    "msNo, sameSubtreesNo) {\n\t\t\tlet className = x.className;\n\t\t\tlet previousColor = x" +
                    ".style.backgroundColor;\n\n\t\t\tlocalStorage.setItem(\'previousColor\', previousColor)" +
                    ";\n\n\t\t\tlet idiomElements = document.getElementsByClassName(className);\n\n\t\t\tfor(va" +
                    "r i=0;i<idiomElements.length;i++){\n\t\t\t\tidiomElements[i].style.backgroundColor = " +
                    "\'#257AFD\';\n\t\t\t}\n\n\t\t\tconst idiomMarkText = document.querySelector(\'#idiom-mark\');" +
                    "\n\t\t\tconst idiomsSameText = document.querySelector(\'#idioms-same\');\n\t\t\tconst subt" +
                    "reesSameText = document.querySelector(\'#subtrees-same\');            \n\t\t\t\n\t\t\tidio" +
                    "mMarkText.textContent = \'mark: \' + className.substring(0,8);\n\t\t\tidiomsSameText.t" +
                    "extContent = \'\\nsame idioms: \' + sameIdiomsNo;\n\t\t\t\n\t\t\tlet sameSubtrees = sameSub" +
                    "treesNo > 0 ? sameSubtreesNo : \'-\';\n\t\t\tsubtreesSameText.textContent = \'\\nsame su" +
                    "btrees: \' + sameSubtrees;\n\n\n\t\t\tconst tooltip = document.querySelector(\'#tooltip\'" +
                    ");\n\t\t\tPopper.createPopper(x, tooltip , {\n\t\t\t\tmodifiers: [\n\t\t\t\t\t{\n\t\t\t\t\tname: \'off" +
                    "set\',\n\t\t\t\t\toptions: {\n\t\t\t\t\t\toffset: [0, 8],\n\t\t\t\t\t},\n\t\t\t\t\t},\n\t\t\t\t],\n\t\t\t});\n\t\t\ttoo" +
                    "ltip.setAttribute(\'data-show\', \'\');\n\t\t}\n\n\t\tfunction unhighlight(x) {\n\t\t\tlet clas" +
                    "sName = x.className;\n\n\t\t\tvar previousColor = localStorage.getItem(\'previousColor" +
                    "\', previousColor);\n\n\t\t\tlet idiomElements = document.getElementsByClassName(class" +
                    "Name);\n\n\t\t\tfor(var i=0;i<idiomElements.length;i++){\n\t\t\t\tidiomElements[i].style.b" +
                    "ackgroundColor = previousColor;\n\t\t\t}\n\n\t\t\tconst tooltip = document.querySelector(" +
                    "\'#tooltip\');\n\t\t\ttooltip.removeAttribute(\'data-show\' );\n\t\t}\n\t\t\n\t\tfunction selectI" +
                    "diom(x, sameIdiomsNo, sameSubtreesNo, idiomCodeEncoded, filesThatHaveIt) {\n\t\t\tle" +
                    "t className = x.className;\n\n\t\t\tlet overlay = document.getElementById(\"details-po" +
                    "pup\");\n\t\t\toverlay.style.visibility = \'visible\'; \n\t\t\toverlay.style.opacity = 1;\n\n" +
                    "\t\t\tlet content = document.getElementById(\"details-popup-content-text\");\n\n\t\t\tcont" +
                    "ent.textContent = \'\'; // Clear everything\n\t\t\tcontent.textContent = \'idiom mark: " +
                    "\' + className;\n\t\t\tcontent.textContent += \'\\nsame idioms: \' + sameIdiomsNo;\n\t\t\t\n\t" +
                    "\t\tlet sameSubtrees = sameSubtreesNo > 0 ? sameSubtreesNo : \'-\';\n\t\t\tcontent.textC" +
                    "ontent += \'\\nsame subtrees: \' + sameSubtrees;\n\n\t\t\tcontent.textContent += \'\\n\\nid" +
                    "iom json: \' + atob(idiomCodeEncoded);\n\n\t\t\tlet listWithLinks = document.getElemen" +
                    "tById(\"details-popup-content-links\");\n\t\t\tlistWithLinks.innerHTML = \'\';\n\t\t\t\n\t\t\tfo" +
                    "r (const file of filesThatHaveIt) {\n\t\t\t\tlet createdLI = createListItemAnchor(\'./" +
                    "\' + file + \'#\' + className, `Idiom in ${file}`);\n\t\t\t\tlistWithLinks.append(create" +
                    "dLI);\n\t\t\t}\n\t\t}\n\n\t\tfunction createListItemAnchor(link, text) {\n\t\t\tlet li = docume" +
                    "nt.createElement(\'li\');\n\t\t\tlet a = document.createElement(\'a\'); \n\t\t\t\t\n\t\t\tlet lin" +
                    "kText = document.createTextNode(text);\n\t\t\t\n\t\t\t\n\t\t\ta.appendChild(linkText); \n\t\t\ta" +
                    ".href = link; \n\t\t\t\t\n\t\t\tli.appendChild(a);\n\t\t\t\n\t\t\treturn li;\n\t\t}\n\n\t\tfunction dese" +
                    "lectIdiom(x) {\n\t\t\tlet overlay = document.getElementById(\"details-popup\")\n\t\t\tover" +
                    "lay.style.visibility = \'hidden\'; \n\t\t\toverlay.style.opacity = 0;\n\t\t}\n\t</script>\n<" +
                    "/head>\n<body>\n");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
 
	var alreadySeenMarks = new List<string>();
	var previousTokenMark = string.Empty;
	foreach (var token in Tokens)
	{
		var nodeHashValue = NodeHasher.CalculateNodeHash(token.ValueText, token.Span.Start, token.Span.End);

		var tokenMark = VisualizationData.Source2TargetMapping.GetValueOrDefault(nodeHashValue);
		if (tokenMark == null)
		{
			throw new DataMisalignedException("For some reason, tokenGuid not found :(");
		}

		if (tokenMark == previousTokenMark)
		{

            
            #line default
            #line hidden
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(HttpUtility.HtmlEncode(token.ToFullString())));
            
            #line default
            #line hidden
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
		
		}
		else 
		{
			if (previousTokenMark != string.Empty)
			{

            
            #line default
            #line hidden
            this.Write("</span>");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"

			}
			var idiomHandler = VisualizationData.IdiomHandler;
			var idiom = idiomHandler.MarksIdiomsMap[tokenMark];
			var sameIdiomsNo = idiomHandler.IdiomRoots[idiom].Count;

			var sameSubtreesNo = 0;
			if (idiomHandler.IdenticalSubtreeRoots.ContainsKey(idiom))
			{
				sameSubtreesNo = idiomHandler.IdenticalSubtreeRoots[idiom].Count;
			}

			var codeFragment = idiomHandler.MarksIdiomCodeMap[tokenMark];
			var codeFragmentBinary = Encoding.UTF8.GetBytes(codeFragment);
			var codeFragmentEncoded = Convert.ToBase64String(codeFragmentBinary);

			previousTokenMark = tokenMark;

			var shouldAddId = false;
			if(!alreadySeenMarks.Contains(tokenMark))
			{
				shouldAddId = true;
				alreadySeenMarks.Add(tokenMark);
			}

			var treeBankIdiom = idiomHandler.MarksIdiomsMap[tokenMark];
			var containingFiles = idiomHandler.IdiomsInFiles[treeBankIdiom].ToList().Select(s => s + ".htm").ToList();



            
            #line default
            #line hidden
            this.Write("<span style=\"background-color:");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(ColorHelper.GetIdiomColor(tokenMark)));
            
            #line default
            #line hidden
            this.Write(";\" class=\"");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(tokenMark));
            
            #line default
            #line hidden
            this.Write("\" ");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
 if(shouldAddId) { Write($"id=\"{tokenMark}\"");} 
            
            #line default
            #line hidden
            this.Write(" onmouseover=\"highlight(this,");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(sameIdiomsNo));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(sameSubtreesNo));
            
            #line default
            #line hidden
            this.Write(")\" onmouseout=\"unhighlight(this)\" onclick=\"selectIdiom(this,");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(sameIdiomsNo));
            
            #line default
            #line hidden
            this.Write(",");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(sameSubtreesNo));
            
            #line default
            #line hidden
            this.Write(", \'");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(codeFragmentEncoded));
            
            #line default
            #line hidden
            this.Write("\', [ ");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(string.Join(",", containingFiles.Select(cf => "'" + cf + "'").ToList())));
            
            #line default
            #line hidden
            this.Write(" ])\"> ");
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(HttpUtility.HtmlEncode(token.ToFullString())));
            
            #line default
            #line hidden
            
            #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
		
		}
	}	

            
            #line default
            #line hidden
            this.Write(@"</span>
<div id=""tooltip"" role=""tooltip"">
	<span id=""idiom-mark""></span>
	<span id=""idioms-same""></span>
	<span id=""subtrees-same""></span>
	<div id=""arrow"" data-popper-arrow></div>
</div>

<div id=""details-popup"">
	<div id=""details-popup-content"">
		<h2>Details</h2>
		<a id=""details-popup-close"" onclick=""deselectIdiom()"">&times;</a>
		<div id=""details-popup-content-body"">
			<p id=""details-popup-content-text"">
			</p>
			<ul style=""padding-left: 0px;"" id=""details-popup-content-links"">
			</ul>
		</div>
	</div>
</div>
<script src=""https://unpkg.com/@popperjs/core@2/dist/umd/popper.js""></script>
</body>
</html>

");
            return this.GenerationEnvironment.ToString();
        }
        
        #line 1 "C:\Users\ntodo\Desktop\SAGED\RoseLibML-master\IdiomHtmlVisualizer\CodeInHTMLTemplate.tt"
   
	
	public ColorHelper ColorHelper { get; set; }
	public VisualizationData VisualizationData { get; set; }
	public IEnumerable<SyntaxToken> Tokens { get; set; }
	

        
        #line default
        #line hidden
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class CodeInHTMLTemplateBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        public System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
