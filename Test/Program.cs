using System;
using System.Collections.Generic;
using System.IO;
using LibMSPackSharp.CAB;
using LibMSPackSharp.CABExtract;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (Directory.Exists(arg))
                {
                    foreach (string file in Directory.EnumerateFiles(arg))
                    {
                        ProcessFile(file);
                    }
                }
                else if (File.Exists(arg))
                {
                    ProcessFile(arg);
                }
                else
                {
                    Console.WriteLine($"{arg} is not a file or folder, skipping...");
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Process a single file
        /// </summary>
        /// <param name="file">Path to a single file</param>
        private static void ProcessFile(string file)
        {
            try
            {
                Console.WriteLine($"Path: {file}");

                // CAB Implementation
                Console.WriteLine("Cabinet information dumper by Stuart Caie <kyzer@cabextract.org.uk>. Ported to C# by Matt Nadareski.");

                Cabinet found = CABInfo.Search(file);
                if (found == null)
                {
                    Console.WriteLine($"Could not find a cabinet in the file provided. Skipping...");
                    return;
                }

                CABInfo.GetInfo(found);

                List<string> paths = new List<string> { file };
                string directory = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                CABExtract.Extract(paths, directory: directory);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
            }
        }
    }
}