using System;

namespace LBuffers.Tests
{
    public class Program
    {
        /// <summary>
        /// The static entry point to the program.
        /// </summary>
        /// <param name="args">Additional command line args.</param>
        public static void Main(string[] args)
        {
            Console.WriteLine(BufferSliceAndOwnerTest.Test());
        }
    }
}