﻿/* This file is part of libmspack.
 * (C) 2003-2004 Stuart Caie.
 *
 * The deflate method was created by Phil Katz. MSZIP is equivalent to the
 * deflate method.
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

using static LibMSPackSharp.Compression.Constants;

namespace LibMSPackSharp.Compression
{
    public partial class MSZIP : CompressionStream
    {
        /// <summary>
        /// 32kb history window
        /// </summary>
        public byte[] Window { get; set; } = new byte[MSZIP_FRAME_SIZE];

        /// <summary>
        /// Offset within window
        /// </summary>
        public uint WindowPosition { get; set; }

        public bool RepairMode { get; set; }

        public int BytesOutput { get; set; }

        #region Huffman code lengths

        public byte[] LITERAL_len { get; set; } = new byte[MSZIP_LITERAL_MAXSYMBOLS];
        public byte[] DISTANCE_len { get; set; } = new byte[MSZIP_DISTANCE_MAXSYMBOLS];

        #endregion

        #region Huffman decoding tables

        public ushort[] LITERAL_table { get; set; } = new ushort[MSZIP_LITERAL_TABLESIZE];
        public ushort[] DISTANCE_table { get; set; } = new ushort[MSZIP_DISTANCE_TABLESIZE];

        #endregion
    }
}
