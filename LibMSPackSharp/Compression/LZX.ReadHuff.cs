﻿/* This file is part of libmspack.
 * (C) 2003-2013 Stuart Caie.
 *
 * The LZX method was created by Jonathan Forbes and Tomi Poutanen, adapted
 * by Microsoft Corporation.
 *
 * libmspack is free software { get; set; } you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

using System;
using static LibMSPackSharp.Compression.Constants;

namespace LibMSPackSharp.Compression
{
    public partial class LZX : CompressionStream
    {
        /// <inheritdoc/>
        public override Error HUFF_ERROR() => Error.MSPACK_ERR_DECRUNCH;

        /// <summary>
        /// BUILD_TABLE(tbl) builds a huffman lookup table from code lengths
        /// </summary>
        private Error BUILD_TABLE(ushort[] table, byte[] lengths, int tablebits, int maxsymbols)
        {
            if (!MakeDecodeTableMSB(maxsymbols, tablebits, lengths, table))
            {
                if (Debug) Console.WriteLine("Failed to build table");
                return Error = Error.MSPACK_ERR_DECRUNCH;
            }

            return Error = Error.MSPACK_ERR_OK;
        }

        private Error BUILD_TABLE_MAYBE_EMPTY()
        {
            LENGTH_empty = false;
            if (!MakeDecodeTableMSB(LZX_LENGTH_MAXSYMBOLS, LZX_LENGTH_TABLEBITS, LENGTH_len, LENGTH_table))
            {
                for (int i = 0; i < LZX_LENGTH_MAXSYMBOLS; i++)
                {
                    if (LENGTH_len[i] > 0)
                    {
                        if (Debug) Console.WriteLine("Failed to build LENGTH table");
                        return Error = Error.MSPACK_ERR_DECRUNCH;
                    }
                }

                // Empty tree - allow it, but don't decode symbols with it
                LENGTH_empty = true;
            }

            return Error = Error.MSPACK_ERR_OK;
        }

        /// <summary>
        /// READ_LENGTHS(tablename, first, last) reads in code lengths for symbols
        /// first to last in the given table. The code lengths are stored in their
        /// own special LZX way.
        /// </summary>
        private Error ReadLengths(byte[] lens, uint first, uint last)
        {
            uint x, y;
            int z;

            // Read lengths for pretree (20 symbols, lengths stored in fixed 4 bits) 
            for (x = 0; x < LZX_PRETREE_MAXSYMBOLS; x++)
            {
                y = (uint)READ_BITS_MSB(4);
                PRETREE_len[x] = (byte)y;
            }

            BUILD_TABLE(PRETREE_table, PRETREE_len, LZX_PRETREE_TABLEBITS, LZX_PRETREE_MAXSYMBOLS);
            if (Error != Error.MSPACK_ERR_OK)
                return Error;

            for (x = first; x < last;)
            {
                z = (int)READ_HUFFSYM_MSB(PRETREE_table, PRETREE_len, LZX_PRETREE_TABLEBITS, LZX_PRETREE_MAXSYMBOLS);
                if (z == 17)
                {
                    // Code = 17, run of ([read 4 bits]+4) zeros
                    y = (uint)READ_BITS_MSB(4);
                    y += 4;
                    while (y-- > 0)
                    {
                        lens[x++] = 0;
                    }
                }
                else if (z == 18)
                {
                    // Code = 18, run of ([read 5 bits]+20) zeros
                    y = (uint)READ_BITS_MSB(5);
                    y += 20;
                    while (y-- > 0)
                    {
                        lens[x++] = 0;
                    }
                }
                else if (z == 19)
                {
                    // Code = 19, run of ([read 1 bit]+4) [read huffman symbol]
                    y = (uint)READ_BITS_MSB(1);
                    y += 4;

                    z = (int)READ_HUFFSYM_MSB(PRETREE_table, PRETREE_len, LZX_PRETREE_TABLEBITS, LZX_PRETREE_MAXSYMBOLS);
                    z = lens[x] - z;
                    if (z < 0)
                        z += 17;

                    while (y-- > 0)
                    {
                        lens[x++] = (byte)z;
                    }
                }
                else
                {
                    // Code = 0 to 16, delta current length entry
                    z = lens[x] - z;
                    if (z < 0)
                        z += 17;

                    lens[x++] = (byte)z;
                }
            }

            return Error.MSPACK_ERR_OK;
        }
    }
}
