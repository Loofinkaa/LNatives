using System.Threading;
using System.Threading.Tasks;

namespace LStreams.Tests
{
    public static class Program
    {
        public static CancellationTokenSource Source { get; private set; } = new CancellationTokenSource();

        /// <summary>
        /// The static entry point to the program.
        /// </summary>
        /// <param name="args">Additional command line args.</param>
        public static async Task Main(string[] args)
        {
            ByteReaderWriterTest.ReadOperation();
        }
    }
}