/* cabinfo -- dumps useful information from cabinets
 * (C) 2000-2018 Stuart Caie <kyzer@cabextract.org.uk>
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

using System;
using LibMSPackSharp.CAB;

namespace LibMSPackSharp.CABExtract
{
    public static class CABInfo
    {
        public static Cabinet Search(string file)
        {
            Decompressor cabDecompressor = Library.CreateCABDecompressor(null);
            Cabinet found = cabDecompressor.Search(file);
            return found;
        }

        public static void GetInfo(Cabinet cabinet)
        {
            Console.WriteLine($"CABINET HEADER @{cabinet.BaseOffset}");
            Console.WriteLine($"- signature      = {cabinet.Header.Signature}");
            Console.WriteLine($"- overall length = {cabinet.Header.CabinetSize} bytes");
            Console.WriteLine($"- files offset   = {cabinet.Header.FileOffset}");
            Console.WriteLine($"- format version = {cabinet.Header.MajorVersion}.{cabinet.Header.MinorVersion}");
            Console.WriteLine($"- folder count   = {cabinet.Header.NumFolders}");
            Console.WriteLine($"- file count     = {cabinet.Header.NumFiles}");
            Console.WriteLine($"- header flags   ="
                + (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_PREVCAB) ? " PREV_CABINET" : "")
                + (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_NEXTCAB) ? " NEXT_CABINET" : "")
                + (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_RESV) ? " RESERVE_PRESENT" : ""));
            Console.WriteLine($"- set ID         = {cabinet.Header.SetID}");
            Console.WriteLine($"- set index      = {cabinet.Header.CabinetIndex}");

            if (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_RESV))
            {
                Console.WriteLine($"- header reserve = {cabinet.Header.HeaderReserved} bytes");
                Console.WriteLine($"- folder reserve = {cabinet.Header.FolderReserved} bytes");
                Console.WriteLine($"- data reserve   = {cabinet.Header.DataReserved} bytes");
            }

            if (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_PREVCAB))
            {
                Console.WriteLine($"- prev cabinet   = {cabinet.PreviousName}");
                Console.WriteLine($"- prev disk      = {cabinet.PreviousInfo}");
            }

            if (cabinet.Header.Flags.HasFlag(HeaderFlags.MSCAB_HDR_NEXTCAB))
            {
                Console.WriteLine($"- next cabinet   = {cabinet.NextName}");
                Console.WriteLine($"- next disk      = {cabinet.NextInfo}");
            }

            Console.WriteLine();
            Console.WriteLine($"FOLDERS SECTION @{cabinet.BaseOffset}"); // TODO: Get actual offset of folder data

            int i = 0;
            Folder folder = cabinet.Folders;
            while (folder != null)
            {
                string compression;
                switch (folder.Header.CompType & CompressionType.COMPTYPE_MASK)
                {
                    case CompressionType.COMPTYPE_NONE:
                        compression = "Stored";
                        break;

                    case CompressionType.COMPTYPE_MSZIP:
                        compression = "MSZIP";
                        break;

                    case CompressionType.COMPTYPE_QUANTUM:
                        compression = "Quantum";
                        break;

                    case CompressionType.COMPTYPE_LZX:
                        compression = "LZX";
                        break;

                    default:
                        compression = "Unknown";
                        break;
                }

                Console.WriteLine($"- Folder 0x{i:x4} @{folder.Data.Offset}:");
                Console.WriteLine($"    Data Blocks - {folder.Header.NumBlocks}");
                Console.WriteLine($"    Data Offset - {folder.Header.DataOffset}");
                Console.WriteLine($"    Compression - {compression} ({folder.Header.CompType:x})");

                if (folder.DataBlocks != null)
                {
                    for (int j = 0; j < folder.DataBlocks.Count; j++)
                    {
                        _DataBlockHeader dataBlockHeader = folder.DataBlocks[j];
                        Console.WriteLine($"    - Data Block {j}");
                        Console.WriteLine($"        Checksum          - {dataBlockHeader.CheckSum:x8}");
                        Console.WriteLine($"        Compressed Size   - {dataBlockHeader.CompressedSize}");
                        Console.WriteLine($"        Uncompressed Size - {dataBlockHeader.UncompressedSize}");
                    }
                }

                folder = folder.Next;
            }

            Console.WriteLine();
            Console.WriteLine($"FILES SECTION @{cabinet.BaseOffset}"); // TODO: Get actual offset of file data

            i = 0;
            InternalFile file = cabinet.Files;
            while (file != null)
            {
                string folder_type;
                switch (file.Header.FolderIndex)
                {
                    case FileFlags.CONTINUED_PREV_AND_NEXT:
                        folder_type = "continued from prev and to next cabinet";
                        break;

                    case FileFlags.CONTINUED_FROM_PREV:
                        folder_type = "continued from prev cabinet";
                        break;

                    case FileFlags.CONTINUED_TO_NEXT:
                        folder_type = "continued to next cabinet";
                        break;

                    default:
                        folder_type = "normal folder";
                        break;
                }

                Console.WriteLine($"- file {i:5} @{cabinet.BaseOffset}"); // TODO: Get actual offset of file header
                Console.WriteLine($"    name   = {file.Filename.TrimEnd('\0')}{(file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_UTF_NAME) ? " (UTF-8)" : "")}");
                Console.WriteLine($"    folder = 0x{file.Folder.Data.Offset:x4} [{folder_type}]");
                Console.WriteLine($"    length = {file.Header.UncompressedSize} bytes");
                Console.WriteLine($"    offset = {file.Header.FolderOffset} bytes");
                Console.WriteLine($"    date   = {file.Header.LastModifiedDateYear:4}/{file.Header.LastModifiedDateMonth:2}/{file.Header.LastModifiedDateDay:2}"
                    + $"{file.Header.LastModifiedTimeHour:2}:{file.Header.LastModifiedTimeMinute:2}:{file.Header.LastModifiedTimeSecond:2}");
                Console.WriteLine($"    attrs   ="
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_RDONLY) ? " RDONLY" : "")
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_HIDDEN) ? " HIDDEN" : "")
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_SYSTEM) ? " SYSTEM" : "")
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_ARCH) ? " ARCH" : "")
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_EXEC) ? " EXEC" : "")
                    + (file.Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_UTF_NAME) ? " UTF-8" : ""));

                file = file.Next;
            }
        }
    }
}
