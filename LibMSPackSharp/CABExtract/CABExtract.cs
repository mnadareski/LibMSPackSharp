/* cabextract - a program to extract Microsoft Cabinet files
 * (C) 2000-2019 Stuart Caie <kyzer@cabextract.org.uk>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

/* cabextract uses libmspack to access cabinet files. libmspack is
 * available from https://www.cabextract.org.uk/libmspack/
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibMSPackSharp.CAB;
using static LibMSPackSharp.Constants;

namespace LibMSPackSharp.CABExtract
{
    public class CABExtract
    {
        /// <summary>
        /// Set of supported options for the program
        /// </summary>
        private static readonly _Option[] OptionList = new _Option[]
        {
            new _Option { Name = "directory",    HasArgument = ArgumentType.RequiredArgument,    Flag = null, Value = "d" },
            new _Option { Name = "fix",          HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "f" },
            new _Option { Name = "filter",       HasArgument = ArgumentType.RequiredArgument,    Flag = null, Value = "F" },
            new _Option { Name = "help",         HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "h" },
            new _Option { Name = "list",         HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "l" },
            new _Option { Name = "lowercase",    HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "L" },
            new _Option { Name = "pipe",         HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "p" },
            new _Option { Name = "quiet",        HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "q" },
            new _Option { Name = "single",       HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "s" },
            new _Option { Name = "test",         HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "t" },
            new _Option { Name = "version",      HasArgument = ArgumentType.NoArgument,          Flag = null, Value = "v" },
        };

        /// <summary>
        /// Decompressor to use for extraction
        /// </summary>
        private static Decompressor CABDecompressor = null;

        /// <summary>
        /// Commandline-inputted cabinet paths
        /// </summary>
        private static List<_FileMem> MemorizedArguments = null;

        /// <summary>
        /// Spanned-found cabinet paths
        /// </summary>
        private static List<_FileMem> MemorizedSpanned = null;

        /// <summary>
        /// Cabinet paths found while processing
        /// </summary>
        private static List<_FileMem> MemorizedSeen = null;

        /// <summary>
        /// Internal set of arguments
        /// </summary>
        private static _Arguments Arguments = new _Arguments();

        /// <summary>
        /// Extract using a set of arguments and paths
        /// </summary>
        /// <remarks>
        /// This method should only be used if piped direct from the commandline
        /// </remarks>
        public static void Extract(string[] args)
        {
            // Parse options
            int index = 0;
            for (; index < args.Length; index++)
            {
                bool cont = true;
                switch (args[index])
                {
                    case "d":
                        Arguments.Directory = (index < args.Length - 1 ? Path.GetFullPath(args[++index]) : null);
                        break;

                    case "f":
                        Arguments.Fix = true;
                        break;

                    case "F":
                        AddFilter((index < args.Length - 1 ? args[++index] : null));
                        break;

                    case "h":
                        Arguments.Help = true;
                        break;

                    case "l":
                        Arguments.View = true;
                        break;

                    case "L":
                        Arguments.Lower = true;
                        break;

                    case "p":
                        Arguments.Pipe = true;
                        break;

                    case "q":
                        Arguments.Quiet = true;
                        break;

                    case "s":
                        Arguments.Single = true;
                        break;

                    case "t":
                        Arguments.Test = true;
                        break;

                    case "v":
                        Arguments.View = true;
                        break;

                    default:
                        cont = false;
                        break;
                }

                if (!cont)
                    break;
            }

            if (Arguments.Help)
            {
                Console.Error.WriteLine($"Usage: CABExtract [options] [-d dir] <cabinet file(s)>");
                Console.Error.WriteLine();
                Console.Error.WriteLine("This will extract all files from a cabinet or executable cabinet.");
                Console.Error.WriteLine("For multi-part cabinets, only specify the first file in the set.");
                Console.Error.WriteLine();

                Console.Error.WriteLine("Options:");
                Console.Error.WriteLine("  -v   --version     print version / list cabinet");
                Console.Error.WriteLine("  -h   --help        show this help page");
                Console.Error.WriteLine("  -l   --list        list contents of cabinet");
                Console.Error.WriteLine("  -t   --test        test cabinet integrity");
                Console.Error.WriteLine("  -q   --quiet       only print errors and warnings");
                Console.Error.WriteLine("  -L   --lowercase   make filenames lowercase");
                Console.Error.WriteLine("  -f   --fix         salvage as much as possible from corrupted cabinets");
                Console.Error.WriteLine("  -p   --pipe        pipe extracted files to stdout");
                Console.Error.WriteLine("  -s   --single      restrict search to cabs on the command line");
                Console.Error.WriteLine("  -F   --filter      extract only files that match the given pattern");
                Console.Error.WriteLine("  -e   --encoding    assume non-UTF8 filenames have the given encoding");
                Console.Error.WriteLine("  -d   --directory   extract all files to the given directory");
                Console.Error.WriteLine();


                Console.Error.WriteLine("CABExtract VERSION (C) 2000-2019 Stuart Caie <kyzer@cabextract.org.uk>");
                Console.Error.WriteLine("This is free software with ABSOLUTELY NO WARRANTY.");
                return;
            }

            if (Arguments.Test && Arguments.View)
            {
                Console.Error.WriteLine("CABExtract: You cannot use --test and --list at the same time.");
                Console.Error.WriteLine("Try '%s --help' for more information.");
                return;
            }

            if (index == args.Length)
            {
                // No arguments other than the options
                if (Arguments.View)
                {
                    Console.WriteLine("CABExtract version VERSION");
                    return;
                }
                else
                {
                    Console.Error.WriteLine("CABExtract: No cabinet files specified.");
                    Console.Error.WriteLine("Try 'CABExtract --help' for more information.");
                    return;
                }
            }

            // Memorise command-line cabs if necessary
            if (Arguments.Single)
            {
                for (int i = index; i < args.Length; i++)
                {
                    MemorizeFile(MemorizedArguments, Path.GetFullPath(args[i]), null);
                }
            }

            // Extracting to stdout implies shutting up on stdout
            if (Arguments.Pipe && !Arguments.View)
                Arguments.Quiet = true;

            // Open libmspack
            CABDecompressor = Library.CreateCABDecompressor(null);
            if (CABDecompressor == null)
            {
                Console.Error.WriteLine("Can't create libmspack CAB decompressor");
                return;
            }

            // Turn on/off 'fix MSZIP' and 'salvage' mode
            CABDecompressor.FixMSZip = Arguments.Fix;
            CABDecompressor.Salvage = Arguments.Fix;

            // Process cabinets
            int errorCount = 0;
            for (; index < args.Length; index++)
            {
                errorCount += ProcessCabinet(Path.GetFullPath(args[index]));
            }

            // Error summary
            if (!Arguments.Quiet)
            {
                if (errorCount != 0)
                    Console.WriteLine($"\nAll done, errors in processing {errorCount} file(s)");
                else
                    Console.WriteLine("\nAll done, no errors.");
            }

            // Close libmspack
            Library.DestroyCABDecompressor(CABDecompressor);

            // Empty file-memory lists
            ForgetFiles(MemorizedArguments);
            ForgetFiles(MemorizedSpanned);
            ForgetFiles(MemorizedSeen);

            return;
        }

        /// <summary>
        /// Extract using a set of arguments and paths
        /// </summary>
        /// <remarks>
        /// This method should only be used when called from other paths
        /// </remarks>
        public static void Extract(
            List<string> paths,
            bool help = false,
            bool lower = false,
            bool pipe = false,
            bool view = false,
            bool quiet = false,
            bool single = false,
            bool fix = false,
            bool test = false,
            string directory = null,
            List<string> filters = null
        )
        {
            // Parse options
            Arguments = new _Arguments()
            {
                Help = help,
                Lower = lower,
                Pipe = pipe,
                View = view,
                Quiet = quiet,
                Single = single,
                Fix = fix,
                Test = test,
                Directory = directory,
                Filters = filters,
            };

            if (Arguments.Help)
            {
                Console.Error.WriteLine($"Usage: CABExtract [options] [-d dir] <cabinet file(s)>");
                Console.Error.WriteLine();
                Console.Error.WriteLine("This will extract all files from a cabinet or executable cabinet.");
                Console.Error.WriteLine("For multi-part cabinets, only specify the first file in the set.");
                Console.Error.WriteLine();

                Console.Error.WriteLine("Options:");
                Console.Error.WriteLine("  -v   --version     print version / list cabinet");
                Console.Error.WriteLine("  -h   --help        show this help page");
                Console.Error.WriteLine("  -l   --list        list contents of cabinet");
                Console.Error.WriteLine("  -t   --test        test cabinet integrity");
                Console.Error.WriteLine("  -q   --quiet       only print errors and warnings");
                Console.Error.WriteLine("  -L   --lowercase   make filenames lowercase");
                Console.Error.WriteLine("  -f   --fix         salvage as much as possible from corrupted cabinets");
                Console.Error.WriteLine("  -p   --pipe        pipe extracted files to stdout");
                Console.Error.WriteLine("  -s   --single      restrict search to cabs on the command line");
                Console.Error.WriteLine("  -F   --filter      extract only files that match the given pattern");
                Console.Error.WriteLine("  -e   --encoding    assume non-UTF8 filenames have the given encoding");
                Console.Error.WriteLine("  -d   --directory   extract all files to the given directory");
                Console.Error.WriteLine();


                Console.Error.WriteLine("CABExtract VERSION (C) 2000-2019 Stuart Caie <kyzer@cabextract.org.uk>");
                Console.Error.WriteLine("This is free software with ABSOLUTELY NO WARRANTY.");
                return;
            }

            if (Arguments.Test && Arguments.View)
            {
                Console.Error.WriteLine("CABExtract: You cannot use --test and --list at the same time.");
                Console.Error.WriteLine("Try '%s --help' for more information.");
                return;
            }

            if (paths == null || !paths.Any())
            {
                // No arguments other than the options
                if (Arguments.View)
                {
                    Console.WriteLine("CABExtract version VERSION");
                    return;
                }
                else
                {
                    Console.Error.WriteLine("CABExtract: No cabinet files specified.");
                    Console.Error.WriteLine("Try 'CABExtract --help' for more information.");
                    return;
                }
            }

            // Memorise command-line cabs if necessary
            if (Arguments.Single)
            {
                foreach (string path in paths)
                {
                    MemorizeFile(MemorizedArguments, Path.GetFullPath(path), null);
                }
            }

            // Extracting to stdout implies shutting up on stdout
            if (Arguments.Pipe && !Arguments.View)
                Arguments.Quiet = true;

            // Open libmspack
            CABDecompressor = Library.CreateCABDecompressor(null);
            if (CABDecompressor == null)
            {
                Console.Error.WriteLine("Can't create libmspack CAB decompressor");
                return;
            }

            // Turn on/off 'fix MSZIP' and 'salvage' mode
            CABDecompressor.FixMSZip = Arguments.Fix;
            CABDecompressor.Salvage = Arguments.Fix;

            // Process cabinets
            int errorCount = 0;
            foreach (string path in paths)
            {
                errorCount += ProcessCabinet(Path.GetFullPath(path));
            }

            // Error summary
            if (!Arguments.Quiet)
            {
                if (errorCount != 0)
                    Console.WriteLine($"\nAll done, errors in processing {errorCount} file(s)");
                else
                    Console.WriteLine("\nAll done, no errors.");
            }

            // Close libmspack
            Library.DestroyCABDecompressor(CABDecompressor);

            // Empty file-memory lists
            ForgetFiles(MemorizedArguments);
            ForgetFiles(MemorizedSpanned);
            ForgetFiles(MemorizedSeen);

            return;
        }

        /// <summary>
        /// Processes each file argument on the command line, as specified by the
        /// command line options. This does the main bulk of work in cabextract.
        /// </summary>
        /// <param name="basename">The file to process</param>
        /// <returns>The number of files with errors, usually 0 for success or 1 for failure</returns>
        public static int ProcessCabinet(string basename)
        {
            Cabinet basecab, cab;
            InternalFile file;
            bool viewhdr = false;
            string from = null, name;
            int errors = 0;

            // Do not process repeat cabinets
            if (RecallFile(MemorizedSeen, basename, ref from) || RecallFile(MemorizedSpanned, basename, ref from))
            {
                if (!Arguments.Quiet)
                {
                    if (string.IsNullOrEmpty(from))
                        Console.WriteLine($"{basename}: skipping known cabinet");
                    else
                        Console.WriteLine($"{basename}: skipping known cabinet (from {from})\n");
                }

                return 0; // Return success
            }

            MemorizeFile(MemorizedSeen, basename, null);

            // Search the file for cabinets
            if ((basecab = CABDecompressor.Search(basename)) == null)
            {
                if (CABDecompressor.Error != Error.MSPACK_ERR_OK)
                    Console.Error.WriteLine($"{basename}: {Library.ErrorToString(CABDecompressor.Error)}");
                else
                    Console.Error.WriteLine($"{basename}: no valid cabinets found");

                return 1;
            }

            // Iterate over all cabinets found in that file
            for (cab = basecab; cab != null; cab = cab.Next)
            {
                // Load all spanning cabinets
                LoadSpanningCabinets(cab, basename);

                // Print headers
                if (!viewhdr)
                {
                    if (Arguments.View)
                    {
                        if (!Arguments.Quiet)
                            Console.WriteLine($"Viewing cabinet:{basename}");

                        Console.WriteLine(" File size | Date       Time     | Name");
                        Console.WriteLine("-----------+---------------------+-------------");
                    }
                    else
                    {
                        if (!Arguments.Quiet)
                        {
                            Console.WriteLine($"{(Arguments.Test ? "Testing" : "Extracting")} cabinet: {basename}");
                        }
                    }

                    viewhdr = true;
                }

                // Process all files
                for (file = cab.Files; file != null; file = file.Next)
                {
                    // Create the full UNIX output filename
                    if ((name = CreateOutputName(file.Filename, Arguments.Directory, Arguments.Lower)) != null)
                    {
                        errors++;
                        continue;
                    }

                    // If filtering, do so now. Skip if file doesn't match any filter
                    if (Arguments.Filters != null && Arguments.Filters.Any())
                    {
                        bool matched = false;
                        foreach (string f in Arguments.Filters)
                        {
                            if (Path.GetFileName(name).Equals(f, StringComparison.OrdinalIgnoreCase))
                            {
                                matched = true;
                                break;
                            }
                        }

                        if (!matched)
                            continue;
                    }

                    // View, extract or test the file
                    if (Arguments.View)
                    {
                        Console.WriteLine($"{file.Header.UncompressedSize}"
                            + $" | {file.Header.LastModifiedDateYear:4}/{file.Header.LastModifiedDateMonth:2}/{file.Header.LastModifiedDateDay:2}"
                            + $" {file.Header.LastModifiedTimeHour:2}:{file.Header.LastModifiedTimeMinute:2}:{file.Header.LastModifiedTimeSecond:2}"
                            + $" | {name}");
                    }
                    else if (Arguments.Test)
                    {
                        if (CABDecompressor.Extract(file, TEST_FNAME) != Error.MSPACK_ERR_OK)
                        {
                            // File failed to extract
                            Console.WriteLine($"  {name}  failed ({Library.ErrorToString(CABDecompressor.Error)})");
                            errors++;
                        }
                        else
                        {
                            // File extracted OK, print the MD5 checksum in md5_result. Print
                            // the checksum right-aligned to 79 columns if that's possible,
                            // otherwise just print it 2 spaces after the filename and "OK"
                            byte[] result = (CABDecompressor.State.OutputFileHandle as TestStream).MD5Context.Hash;

                            // "  filename  OK  " is 8 chars + the length of filename,
                            // the MD5 checksum itself is 32 chars.
                            int spaces = 79 - (name.Length + 8 + 32);
                            Console.WriteLine($"  {name}  OK  ".PadRight(spaces, ' '));
                            Console.WriteLine(BitConverter.ToString(result).Replace("-", string.Empty));
                        }
                    }
                    else
                    {
                        // Extract the file
                        if (Arguments.Pipe)
                        {
                            // Extracting to stdout
                            if (CABDecompressor.Extract(file, STDOUT_FNAME) != Error.MSPACK_ERR_OK)
                            {
                                Console.Error.WriteLine("%s(%s): %s\n", STDOUT_FNAME, name, Library.ErrorToString(CABDecompressor.Error));
                                errors++;
                            }
                        }
                        else
                        {
                            // Extracting to a regular file
                            if (!Arguments.Quiet)
                                Console.WriteLine("  extracting %s\n", name);

                            if (!EnsureFilepath(name))
                            {
                                Console.Error.WriteLine("%s: can't create file path\n", name);
                                errors++;
                            }
                            else
                            {
                                if (CABDecompressor.Extract(file, name) != Error.MSPACK_ERR_OK)
                                {
                                    Console.Error.WriteLine("%s: %s\n", name, Library.ErrorToString(CABDecompressor.Error));
                                    errors++;
                                }
                                else
                                {
                                    file.SetDateAndPerm(name);
                                }
                            }
                        }
                    }
                }
            }

            CABDecompressor.Close(basecab);
            return errors;
        }

        /// <summary>
        /// Follows the spanning cabinet chain specified in a cabinet, loading
        /// and attaching the spanning cabinets as it goes.
        /// </summary>
        /// <param name="basecab">The base cabinet to start the chain from.</param>
        /// <param name="basename">The full pathname of the base cabinet, so spanning cabinets can be found in the same path as the base cabinet.</param>
        /// <see cref="FindCabinetFile(string, string)"/>
        public static void LoadSpanningCabinets(Cabinet basecab, string basename)
        {
            Cabinet cab, cab2;
            string name;

            // Load any spanning cabinets -- backwards
            for (cab = basecab; cab.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_PREVCAB); cab = cab.PreviousCabinet)
            {
                if ((name = FindCabinetFile(basename, cab.PreviousName)) == null)
                {
                    Console.Error.WriteLine($"{basename}: can't find {cab.PreviousName}");
                    break;
                }

                string from = null;
                if (Arguments.Single && !RecallFile(MemorizedArguments, name, ref from))
                    break;

                if (!Arguments.Quiet)
                    Console.WriteLine($"{basename}: extends backwards to {cab.PreviousName} ({cab.PreviousInfo})");

                if ((cab2 = CABDecompressor.Open(name)) == null || CABDecompressor.Prepend(cab, cab2) != Error.MSPACK_ERR_OK)
                {
                    Console.Error.WriteLine($"{basename}: can't prepend {cab.PreviousName}: {Library.ErrorToString(CABDecompressor.Error)}");
                    if (cab2 != null)
                        CABDecompressor.Close(cab2);

                    break;
                }

                MemorizeFile(MemorizedSpanned, name, basename);
            }

            // Load any spanning cabinets -- forwards
            for (cab = basecab; cab.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_NEXTCAB); cab = cab.NextCabinet)
            {
                if ((name = FindCabinetFile(basename, cab.NextName)) == null)
                {
                    Console.Error.WriteLine($"{basename}: can't find {cab.NextName}");
                    break;
                }

                string from = null;
                if (Arguments.Single && !RecallFile(MemorizedArguments, name, ref from))
                    break;

                if (!Arguments.Quiet)
                    Console.WriteLine($"{basename}: extends to {cab.NextName} ({cab.NextInfo})");

                if ((cab2 = CABDecompressor.Open(name)) == null || CABDecompressor.Append(cab, cab2) != Error.MSPACK_ERR_OK)
                {
                    Console.Error.WriteLine($"{basename}: can't append {cab.NextName}: {Library.ErrorToString(CABDecompressor.Error)}");
                    if (cab2 != null)
                        CABDecompressor.Close(cab2);

                    break;
                }

                MemorizeFile(MemorizedSpanned, name, basename);
            }
        }

        /// <summary>
        /// Matches a cabinet's filename case-insensitively in the filesystem and
        /// returns the case-correct form.
        /// </summary>
        /// <param name="origcab">If this is non-null, the pathname part of this filename will be extracted, and the search will be conducted in that directory.</param>
        /// <param name="cabname">The internal CAB filename to search for.</param>
        /// <returns>A copy of the full, case-correct filename of the given cabinet filename, or null if the specified filename does not exist on disk.</returns>
        private static string FindCabinetFile(string origcab, string cabname)
        {
            if (string.IsNullOrWhiteSpace(origcab))
                return null;

            string directory = Path.GetDirectoryName(origcab);
            string newfile = Path.Combine(directory, cabname);
            if (!File.Exists(newfile))
                return null;

            return newfile;
        }

        /// <summary>
        /// Creates a UNIX filename from the internal CAB filename and the given parameters.
        /// </summary>
        /// <param name="fname">The internal CAB filename.</param>
        /// <param name="dir">A directory path to prepend to the output filename.</param>
        /// <param name="lower">If true, filename should be made lower-case.</param>
        /// <returns>A freshly allocated and created filename</returns>
        private static string CreateOutputName(string fname, string dir, bool lower)
        {
            if (lower)
            {
                fname = fname.ToLowerInvariant();
                dir = dir.ToLowerInvariant();
            }

            return Path.Combine(dir, fname);
        }

        #region Support Functions

        /// <summary>
        /// Memorizes a file by its device and inode number rather than its name. If
        /// the file does not exist, it will not be memorised.
        /// </summary>
        /// <param name="fml">Address of the FileMem list that will memorise this file.</param>
        /// <param name="name">Name of the file to memorise.</param>
        /// <param name="from">A string that, if not null, will be duplicated stored with the memorised file.</param>
        /// <see cref="RecallFile(List{_FileMem}, string, ref string)"/>
        /// <see cref="ForgetFiles(List{_FileMem})"/>
        private static void MemorizeFile(List<_FileMem> fml, string name, string from)
        {
            if (fml == null || string.IsNullOrWhiteSpace(name))
                return;

            if (fml.Any(fm => fm.Name.Equals(name)))
                return;

            fml.Add(new _FileMem { Name = name, From = from });
        }

        /// <summary>
        /// Determines if a file has been memorised before, by its device and inode
        /// number. If the file does not exist, it cannot be recalled.
        /// </summary>
        /// <param name="fml">List to search for previously memorised file</param>
        /// <param name="name">Name of file to recall.</param>
        /// <param name="from">If non-null, this is an address that the associated "from" description pointer will be stored.</param>
        /// <returns>True if the file has been previously memorised, false if the file is unknown or does not exist.</returns>
        /// <see cref="MemorizeFile(List{_FileMem}, string, string)"/>
        /// <see cref="ForgetFiles(List{_FileMem})"/>
        private static bool RecallFile(List<_FileMem> fml, string name, ref string from)
        {
            if (fml == null || string.IsNullOrWhiteSpace(name))
                return false;

            if (!fml.Any(fm => fm.Name.Equals(name)))
                return false;

            _FileMem fileMem = fml.First(fm => fm.Name.Equals(name));
            from = fileMem.From;

            return true;
        }

        /// <summary>
        /// Frees all memory used by a FileMem list.
        /// </summary>
        /// <param name="fml">fml address of the list to free</param>
        /// <see cref="MemorizeFile(List{_FileMem}, string, string)"/>
        private static void ForgetFiles(List<_FileMem> fml) => fml.Clear();

        /// <summary>
        /// Adds a filter to args.filters
        /// </summary>
        /// <param name="filter"></param>
        private static void AddFilter(string filter)
        {
            if (Arguments.Filters == null)
                Arguments.Filters = new List<string>();

            Arguments.Filters.Add(filter);
        }

        /// <summary>
        /// Ensures that all directory components in a filepath exist. New directory
        /// components are created, if necessary.
        /// </summary>
        /// <param name="path">The filepath to check</param>
        /// <returns></returns>
        private static bool EnsureFilepath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
