using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using RoseLibML.Util;

namespace RoseLibLS.Util
{
    class ProgressListener : IProgressListener
    {
        private readonly int totalIterations;
        private readonly IWorkDoneObserver reporter;

        public ProgressListener(IWorkDoneObserver reporter, int totalIterations)
        {
            this.totalIterations = totalIterations;
            this.reporter = reporter;
        }

        public void Update(int iteration)
        {
            int percentage = (int)(((double)iteration / (double)totalIterations) * 100);

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = percentage,
                    Message = $"MCMC phase in progress - iteration {iteration+1}.",
                }
            );
        }
    }
}
