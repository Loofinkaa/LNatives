using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LStreams.Tests
{
    /// <summary>
    /// This class is a test of <see cref="ByteWriter"/> and <see cref="ByteReader"/>.
    /// </summary>
    public static class ByteReaderWriterTest
    {
        /// <summary>
        /// Asyncronly writes a data to the file.
        /// </summary>
        /// <param name="token">A token to cancle the operation.</param>
        public static async Task WriteOperation(CancellationToken token)
        {
            using (FileStream baseStream = File.Create("data.bin"))
            {
                using (ByteWriter writer = new ByteWriter(baseStream))
                {
                    writer.WriteStringWithInt32Prefix("This is a test string!");
                    writer.WriteInt16(67);

                    await writer.FlushAsync(token);
                }
            }
        }

        /// <summary>
        /// Reads a data from the file and print to the console.
        /// </summary>
        public static void ReadOperation()
        {
            using (FileStream baseStream = File.Open("data.bin", FileMode.Open))
            {
                using (ByteReader reader = new ByteReader(baseStream))
                {
                    Console.WriteLine("Readed string: " + reader.ReadStringWithInt32Prefix());
                    Console.Write("Readed number: " + reader.ReadInt16());
                }
            }
        }
    }
}