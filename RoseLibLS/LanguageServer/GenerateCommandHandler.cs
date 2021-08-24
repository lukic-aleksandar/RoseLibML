using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RoseLibLS.Transformer;
using Microsoft.Build.Locator;
using RoseLibLS.Util;

namespace RoseLibLS.LanguageServer
{
    internal class GenerateCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability capability;

        private readonly KnowledgeBase knowledgeBase;

        public GenerateCommandHandler()
        {
            MSBuildLocator.RegisterDefaults();

            // load knowledge base
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                knowledgeBase = (JToken.ReadFrom(reader)).ToObject<KnowledgeBase>();
            }
        }

        public async Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("Generate Command Handler");

            List<OutputSnippet> outputSnippets = request.Arguments.First().ToObject<List<OutputSnippet>>();

            List<string> errorsGenerate = Validation.ValidateArguments(outputSnippets);
            if (errorsGenerate.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsGenerate);

                Log.Logger.Error($"Generate Command Handler | {validationErrors}", validationErrors);
                return new CommandResponse(validationErrors, true);
            }

            try
            {
                CSIdiomTransformer transformer = new CSIdiomTransformer(knowledgeBase);
                bool success = await transformer.Generate(outputSnippets);

                if (!success)
                {
                    Log.Logger.Error("Generate Command Handler | An error occurred while generating.");
                    return new CommandResponse("An error occurred while generating.", true);
                }

                Log.Logger.Debug("Generate Command Handler | Succesfully done");
                return new CommandResponse(null, "Succesfully done.", false);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Generate Command Handler | " + e.Message);
                return new CommandResponse("An error occurred. Please try again.", true); ;
            }
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            this.capability = capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.generate" },
            };
        }
    }
}