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
    internal class IdiomsCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability _capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("Idioms Command Handler");

            GetIdiomsCommandArguments getIdiomsArguments = request.Arguments.First().ToObject<GetIdiomsCommandArguments>();

            try
            {
                List<CodeIdiom> idioms = LoadCodeIdioms(getIdiomsArguments.RootType);

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
            _capability = capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.getIdioms" },
            };
        }

        private List<CodeIdiom> LoadCodeIdioms(string rootType)
        {
            List<CodeIdiom> idioms = new List<CodeIdiom>();

            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"idioms_proposal.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                idioms = ((JArray)JToken.ReadFrom(reader)).ToObject<List<CodeIdiom>>();

                // filter by root node type
                if (rootType != null)
                {
                    var filteredIdioms = idioms.Where(x => x.RootCSType.ToLower().Contains(rootType.ToLower()));
                    idioms = filteredIdioms.ToList();
                }
            }

            return idioms;
        }
    }
}
