using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdiomHtmlVisualizer
{
    public static class HtmlHelper
    {
        public static void PrintHTML(string fileName, SyntaxTree syntaxTree, Dictionary<uint, string> dictionary)
        {
            ColorHelper colorHelper = new ColorHelper();

            var tokens = syntaxTree.GetRoot().DescendantTokens();
            using (var streamWriter = new StreamWriter(fileName))
            {
                streamWriter.Write(@"
<html>
<head>
    <style>span {white-space: pre ; font-family:'Courier New';}</style>
<style>
        #tooltip {
          background-color: #333;
          color: white;
          padding: 5px 10px;
          border-radius: 4px;
          font-size: 13px;
          display: none;
        }

        #arrow,
        #arrow::before {
            position: absolute;
            width: 8px;
            height: 8px;
            background: inherit;
        }

        #arrow {
            visibility: hidden;
        }

        #arrow::before {
            visibility: visible;
            content: '';
            transform: rotate(45deg);
        }

        #tooltip[data-popper-placement^='top'] > #arrow {
            bottom: -4px;
        }

        #tooltip[data-popper-placement^='bottom'] > #arrow {
            top: -4px;
        }

        #tooltip[data-popper-placement^='left'] > #arrow {
            right: -4px;
        }

        #tooltip[data-popper-placement^='right'] > #arrow {
            left: -4px;
        }

        #tooltip[data-show] {
            display: block;
        }
    </style>
    <script>
        function highlight(x) {
            let className = x.className;
            let previousColor = x.style.backgroundColor;

            localStorage.setItem(""previousColor"", previousColor);

            let idiomElements = document.getElementsByClassName(className);

            for(var i=0;i<idiomElements.length;i++){
                idiomElements[i].style.backgroundColor = '#257AFD';
            }

            const tooltipText = document.querySelector('#tooltip-text');
            tooltipText.textContent = className;
            const tooltip = document.querySelector('#tooltip');
            Popper.createPopper(x, tooltip , {
                modifiers: [
                    {
                    name: 'offset',
                    options: {
                        offset: [0, 8],
                    },
                    },
                ],
            });
            tooltip.setAttribute('data-show', '');
        }

        function unhighlight(x) {
            let className = x.className;

            var previousColor = localStorage.getItem(""previousColor"", previousColor);

            let idiomElements = document.getElementsByClassName(className);

            for(var i=0;i<idiomElements.length;i++){
                idiomElements[i].style.backgroundColor = previousColor;
            }

            const tooltip = document.querySelector('#tooltip');
            tooltip.removeAttribute('data-show' );
        }
        
        function selectIdiom(x) {
            let className = x.className;

            let idiomElements = document.getElementsByClassName(className);

            let idiom = ``;
            for(var i=0;i<idiomElements.length;i++){
                idiom += idiomElements[i].textContent;
            }

            alert(idiom);
        }
    </script>
</head>
<body>
");
                var previousTokenMark = string.Empty;
                foreach (var token in tokens)
                {
                    var STInfo = token.ValueText;
                    var RoslynSpanStart = token.Span.Start;
                    var RoslynSpanEnd = token.Span.End;

                    byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes($"{STInfo}|{RoslynSpanStart}|{RoslynSpanEnd}"));
                    var keyValue = BitConverter.ToUInt32(encoded, 0) % 1000000000;

                    var tokenMark = dictionary.GetValueOrDefault(keyValue);
                    if (tokenMark == null)
                    {
                        throw new DataMisalignedException("For some reason, tokenGuid not found :(");
                    }

                    if (tokenMark == previousTokenMark)
                    {
                        streamWriter.Write($"{token.ToFullString()}");
                    }
                    else
                    {
                        if (previousTokenMark != string.Empty)
                        {
                            streamWriter.Write($"</span>");
                        }
                        streamWriter.Write($"<span style=\"background-color:{colorHelper.GetIdiomColor(tokenMark)};\" class=\"{tokenMark}\" onmouseover=\"highlight(this)\" onmouseout=\"unhighlight(this)\" onclick=\"selectIdiom(this)\">{token.ToFullString()}");
                        previousTokenMark = tokenMark;
                    }
                }
                streamWriter.Write(@"
</span>
<div id=""tooltip"" role=""tooltip"">
    <span id=""tooltip-text""></span>
    <div id=""arrow"" data-popper-arrow></div>
</div>
<script src=""https://unpkg.com/@popperjs/core@2/dist/umd/popper.js""></script>
</body>
</html>
");
            }
        }

    }
}
