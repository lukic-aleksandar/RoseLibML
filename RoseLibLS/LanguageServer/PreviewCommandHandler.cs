using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using RoseLibLS.Transformer;
using RoseLibLS.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer
{
    internal class PreviewCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability capability;

        private readonly KnowledgeBase knowledgeBase;

        public PreviewCommandHandler()
        {
            // load knowledge base
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                knowledgeBase = (JToken.ReadFrom(reader)).ToObject<KnowledgeBase>();
            }
        }

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("Preview Command Handler");

            PreviewCommandArguments previewArguments = request.Arguments.First().ToObject<PreviewCommandArguments>();

            List<string> errorsPreview = Validation.ValidateArguments(previewArguments);
            if (errorsPreview.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsPreview);

                Log.Logger.Error($"Preview Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }

            try
            {
                CSIdiomTransformer transformer = new CSIdiomTransformer(knowledgeBase);
                string previewFragment = transformer.TransformFragmentString(previewArguments.Fragment, previewArguments.MethodParameters, true);

                Log.Logger.Debug("Preview Command Handler | Succesfully done");
                return Task.FromResult(new CommandResponse(previewFragment, "Succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("Preview Command Handler | " + e.Message);
                return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
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
                Commands = new List<string> { "rose-lib-ml.getPreview" },
            };
        }
    }
}
