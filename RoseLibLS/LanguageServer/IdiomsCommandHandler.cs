using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using RoseLibLS.Transformer;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer
{
    internal class IdiomsCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability capability;

        private readonly KnowledgeBase knowledgeBase;

        public IdiomsCommandHandler()
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
            Log.Logger.Debug("Idioms Command Handler");

            GetIdiomsCommandArguments getIdiomsArguments = request.Arguments.First().ToObject<GetIdiomsCommandArguments>();

            try
            {
                List<CodeIdiom> idioms = LoadCodeIdioms(getIdiomsArguments.RootNodeType);

                Log.Logger.Debug("Idioms Command Handler | Succesfully done");
                return Task.FromResult(new CommandResponse(idioms, "Succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("Idioms Command Handler | " + e.Message);
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
                Commands = new List<string> { "rose-lib-ml.getIdioms" },
            };
        }

        private List<CodeIdiom> LoadCodeIdioms(string rootNodeType)
        {
            List<CodeIdiom> idioms = new List<CodeIdiom>();

            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"idioms_proposal.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                idioms = ((JArray)JToken.ReadFrom(reader)).ToObject<List<CodeIdiom>>();

                // filter by root node type
                if (rootNodeType != null)
                {
                    var filteredIdioms = idioms.Where(i => i.RootNodeType.ToLower().Contains(rootNodeType.ToLower()));
                    idioms = filteredIdioms.ToList();
                }

                // add information about available composers
                foreach (var idiom in idioms)
                {
                    if (!knowledgeBase.RootTypeToComposerMapping.ContainsKey(idiom.RootNodeType))
                    {
                        idioms.Remove(idiom);
                        continue;
                    }

                    idiom.Composers = knowledgeBase.RootTypeToComposerMapping[idiom.RootNodeType];
                }
            }

            return idioms;
        }
    }
}
