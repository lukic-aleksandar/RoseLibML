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
using RoseLibML.LanguageServer.Transformer;

namespace RoseLibML.LanguageServer
{
    internal class GenerateCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability _capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("Generate Command Handler");

            GenerateCommandArguments generateArguments = request.Arguments.First().ToObject<GenerateCommandArguments>();

            List<string> errorsGenerate = Validation.ValidateArguments(generateArguments);
            if (errorsGenerate.Count > 0)
            {
                string validationErrors = string.Join(". ", errorsGenerate);

                Log.Logger.Error($"Generate Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }

            try
            {
                List<string> outputSnippets = GenerateMethodsFromIdiom(generateArguments);

                Log.Logger.Debug("Generate Command Handler | Succesfully done");
                return Task.FromResult(new CommandResponse(outputSnippets, "Succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("Generate Command Handler | " + e.Message);
                return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
            }
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            _capability = capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.generate" },
            };
        }

        private List<string> GenerateMethodsFromIdiom(GenerateCommandArguments arguments)
        {
            // load knowledge base
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                KnowledgeBase knowledgeBase = (JToken.ReadFrom(reader)).ToObject<KnowledgeBase>();

                Transformer.Transformer transformer = new Transformer.Transformer(knowledgeBase);
                return transformer.Generate(arguments);
            }
        }
    }
}