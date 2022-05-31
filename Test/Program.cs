﻿using System;
using System.IO;
using LibMSPackSharp;

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

                Console.WriteLine("Currently, this program does nothing. It will have reference implementations for all supported files in the future.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
            }
        }
    }
}