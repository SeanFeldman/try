using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<WorkspaceServer.Servers.Scripting.RedirectConsoleOutput>;

namespace WorkspaceServer.Servers.Scripting
{
    public class RedirectConsoleOutput : IDisposable
    {
        private TextWriter originalOutputWriter;
        private TextWriter originalErrorWriter;
        private readonly StringWriter outputWriter = new StringWriter();
        private readonly StringWriter errorWriter = new StringWriter();

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int alreadyDisposed = NOT_DISPOSED;

        private static readonly SemaphoreSlim consoleLock = new SemaphoreSlim(1, 1);

        private RedirectConsoleOutput()
        {
        }

        public static async Task<RedirectConsoleOutput> Acquire()
        {
            var redirector = new RedirectConsoleOutput();
            await consoleLock.WaitAsync();

            try
            {
                redirector.originalOutputWriter = Console.Out;
                redirector.originalErrorWriter = Console.Error;

                Console.SetOut(redirector.outputWriter);
                Console.SetError(redirector.errorWriter);
            }
            catch
            {
                consoleLock.Release();
                throw;
            }

            return redirector;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref alreadyDisposed, DISPOSED, NOT_DISPOSED) == NOT_DISPOSED)
            {
                if (originalOutputWriter != null)
                {
                    Console.SetOut(originalOutputWriter);
                }
                if (originalErrorWriter != null)
                {
                    Console.SetError(originalErrorWriter);
                }
               
                consoleLock.Release();
            }
        }

        public override string ToString() => outputWriter.ToString().Trim();

        public void Clear() => outputWriter.GetStringBuilder().Clear();

        public bool IsEmpty() => outputWriter.ToString().Length == 0;
    }
}
