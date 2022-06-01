﻿/* This file is part of libmspack.
 * (C) 2003-2004 Stuart Caie.
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

namespace LibMSPackSharp
{
    internal class Constants
    {
        #region CAB

        // CAB data blocks are <= 32768 bytes in uncompressed form.Uncompressed
        // blocks have zero growth. MSZIP guarantees that it won't grow above
        // uncompressed size by more than 12 bytes.LZX guarantees it won't grow
        // more than 6144 bytes.Quantum has no documentation, but the largest
        // block seen in the wild is 337 bytes above uncompressed size.

        public const int CAB_BLOCKMAX = 32768;
        public const int CAB_INPUTMAX = CAB_BLOCKMAX + 6144;

        // input buffer needs to be CAB_INPUTMAX + 1 byte to allow for max-sized block
        // plus 1 trailer byte added by cabd_sys_read_block() for Quantum alignment.
        // 
        // When MSCABD_PARAM_SALVAGE is set, block size is not checked so can be
        // up to 65535 bytes, so max input buffer size needed is 65535 + 1

        public const int CAB_INPUTMAX_SALVAGE = 65535;
        public const int CAB_INPUTBUF = CAB_INPUTMAX_SALVAGE + 1;

        // There are no more than 65535 data blocks per folder, so a folder cannot
        // be more than 32768*65535 bytes in length.As files cannot span more than
        // one folder, this is also their max offset, length and offset+length limit.

        public const int CAB_FOLDERMAX = 65535;
        public const int CAB_LENGTHMAX = CAB_BLOCKMAX * CAB_FOLDERMAX;

        #endregion

        #region CABExtract

        public const string OPTSTRING = "d:e:fF:hlLpqstv";

        /// <summary>
        /// A special filename. Extracting to this filename will send the output
        /// to standard output instead of a file on disk. The magic happens in
        /// cabx_open() when the STDOUT_FNAME pointer is given as a filename, so
        /// treat this like a constant rather than a string.
        /// </summary>
        public const string STDOUT_FNAME = "stdout";

        /// <summary>
        /// A special filename. Extracting to this filename will send the output
        /// through an MD5 checksum calculator, instead of a file on disk. The
        /// magic happens in cabx_open() when the TEST_FNAME pointer is given as a
        /// filename, so treat this like a constant rather than a string. 
        /// </summary>
        public const string TEST_FNAME = "test";

        #endregion

        #region CHM

        // Filenames of the system files used for decompression.
        // - Content and ControlData are essential.
        // - ResetTable is preferred, but SpanInfo can be used if not available
        public const string ContentName = "::DataSpace/Storage/MSCompressed/Content";
        public const string ControlName = "::DataSpace/Storage/MSCompressed/ControlData";
        public const string SpanInfoName = "::DataSpace/Storage/MSCompressed/SpanInfo";
        public const string ResetTableName = "::DataSpace/Storage/MSCompressed/Transform/{7FC28940-9D31-11D0-9B27-00A0C91E9C7C}/InstanceData/ResetTable";

        #endregion

        #region HLP

        // None currently

        #endregion

        #region KWAJ

        // Input buffer size during decompression - not worth parameterising IMHO
        public const int KWAJ_INPUT_SIZE = (2048);

        // Huffman codes that are 9 bits or less are decoded immediately
        public const int KWAJ_TABLEBITS = (9);

        // Number of codes in each huffman table

        public const int KWAJ_MATCHLEN1_SYMS = (16);
        public const int KWAJ_MATCHLEN2_SYMS = (16);
        public const int KWAJ_LITLEN_SYMS = (32);
        public const int KWAJ_OFFSET_SYMS = (64);
        public const int KWAJ_LITERAL_SYMS = (256);

        // Define decoding table sizes

        public const int KWAJ_TABLESIZE = (1 << KWAJ_TABLEBITS);

        //public const int KWAJ_MATCHLEN1_TBLSIZE = (KWAJ_MATCHLEN1_SYMS * 4);
        public const int KWAJ_MATCHLEN1_TBLSIZE = (KWAJ_TABLESIZE + (KWAJ_MATCHLEN1_SYMS * 2));

        //public const int KWAJ_MATCHLEN2_TBLSIZE = (KWAJ_MATCHLEN2_SYMS * 4);
        public const int KWAJ_MATCHLEN2_TBLSIZE = (KWAJ_TABLESIZE + (KWAJ_MATCHLEN2_SYMS * 2));

        //public const int KWAJ_LITLEN_TBLSIZE = (KWAJ_LITLEN_SYMS * 4);
        public const int KWAJ_LITLEN_TBLSIZE = (KWAJ_TABLESIZE + (KWAJ_LITLEN_SYMS * 2));

        //public const int KWAJ_OFFSET_TBLSIZE = (KWAJ_OFFSET_SYMS * 4);
        public const int KWAJ_OFFSET_TBLSIZE = (KWAJ_TABLESIZE + (KWAJ_OFFSET_SYMS * 2));

        //public const int KWAJ_LITERAL_TBLSIZE = (KWAJ_LITERAL_SYMS * 4);
        public const int KWAJ_LITERAL_TBLSIZE = (KWAJ_TABLESIZE + (KWAJ_LITERAL_SYMS * 2));

        #endregion

        #region LIT

        // None currently

        #endregion

        #region OAB

        // _Header
        public const int oabhead_VersionHi = 0x0000;
        public const int oabhead_VersionLo = 0x0004;
        public const int oabhead_BlockMax = 0x0008;
        public const int oabhead_TargetSize = 0x000c;
        public const int oabhead_SIZEOF = 0x0010;

        // _Block
        public const int oabblk_Flags = 0x0000;
        public const int oabblk_CompSize = 0x0004;
        public const int oabblk_UncompSize = 0x0008;
        public const int oabblk_CRC = 0x000c;
        public const int oabblk_SIZEOF = 0x0010;

        // _PatchHeader
        public const int patchhead_VersionHi = 0x0000;
        public const int patchhead_VersionLo = 0x0004;
        public const int patchhead_BlockMax = 0x0008;
        public const int patchhead_SourceSize = 0x000c;
        public const int patchhead_TargetSize = 0x0010;
        public const int patchhead_SourceCRC = 0x0014;
        public const int patchhead_TargetCRC = 0x0018;
        public const int patchhead_SIZEOF = 0x001c;

        // _PatchBlock
        public const int patchblk_PatchSize = 0x0000;
        public const int patchblk_TargetSize = 0x0004;
        public const int patchblk_SourceSize = 0x0008;
        public const int patchblk_CRC = 0x000c;
        public const int patchblk_SIZEOF = 0x0010;

        #endregion

        #region SZDD

        /// <summary>
        /// Input buffer size during decompression - not worth parameterising IMHO
        /// </summary>
        public const int SZDD_INPUT_SIZE = 2048;

        public static readonly byte[] expandSignature = new byte[8]
        {
            0x53, 0x5A, 0x44, 0x44, 0x88, 0xF0, 0x27, 0x33
        };

        public static readonly byte[] qbasicSignature = new byte[8]
        {
            0x53, 0x5A, 0x20, 0x88, 0xF0, 0x27, 0x33, 0xD1
        };

        #endregion
    }
}
