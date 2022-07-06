﻿/* This file is part of libmspack.
 * (C) 2003-2018 Stuart Caie.
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

using System;

namespace LibMSPackSharp.CAB
{
    // CFHEADER
    internal class _CabinetHeader
    {
        #region Normal Header

        /// <summary>
        /// "MSCF"
        /// </summary>
        /// <remarks>0x00</remarks>
        public uint Signature { get; private set; }

        /// <summary>
        /// Reserved field; MUST be set to 0 (zero).
        /// </summary>
        /// <remarks>0x04</remarks>
        public uint Reserved1 { get; private set; }

        /// <summary>
        /// Specifies the total size of the cabinet file, in bytes.
        /// </summary>
        /// <remarks>0x08</remarks>
        public int CabinetSize { get; private set; }

        /// <summary>
        /// Reserved field; MUST be set to 0 (zero).
        /// </summary>
        /// <remarks>0x0C</remarks>
        public uint Reserved2 { get; private set; }

        /// <summary>
        /// Specifies the absolute file offset, in bytes, of the first CFFILE field entry
        /// </summary>
        /// <remarks>0x10</remarks>
        public uint FileOffset { get; private set; }

        /// <summary>
        /// Reserved field; MUST be set to 0 (zero).
        /// </summary>
        /// <remarks>0x14</remarks>
        public uint Reserved3 { get; private set; }

        /// <summary>
        /// Specifies the minor cabinet file format version. This value MUST be set to 3 (three).
        /// </summary>
        /// <remarks>0x18</remarks>
        public byte MinorVersion { get; private set; }

        /// <summary>
        /// Specifies the major cabinet file format version. This value MUST be set to 1 (one).
        /// </summary>
        /// <remarks>0x19</remarks>
        public byte MajorVersion { get; private set; }

        /// <summary>
        /// Specifies the number of CFFOLDER field entries in this cabinet file.
        /// </summary>
        /// <remarks>0x1A</remarks>
        public short NumFolders { get; private set; }

        /// <summary>
        /// Specifies the number of CFFILE field entries in this cabinet file.
        /// </summary>
        /// <remarks>0x1C</remarks>
        public short NumFiles { get; private set; }

        /// <summary>
        /// Specifies bit-mapped values that indicate the presence of optional data.
        /// </summary>
        /// <remarks>0x1E</remarks>
        /// <see cref="Cabinet.PreviousCabinetName"/>
        /// <see cref="Cabinet.PreviousDiskName"/>
        /// <see cref="Cabinet.NextCabinetName"/>
        /// <see cref="Cabinet.NextDiskName"/>
        /// <see cref="Cabinet.HeaderResv"/>
        public HeaderFlags Flags { get; private set; }

        /// <summary>
        /// Specifies an arbitrarily derived (random) value that binds a collection of linked cabinet files
        /// together.All cabinet files in a set will contain the same setID field value.This field is used by
        /// cabinet file extractors to ensure that cabinet files are not inadvertently mixed.This value has no
        /// meaning in a cabinet file that is not in a set.

        /// </summary>
        /// <remarks>0x20</remarks>
        public ushort SetID { get; private set; }

        /// <summary>
        /// The index number of the cabinet within the set. Numbering should
        /// start from 0 for the first cabinet in the set, and increment by 1 for
        /// each following cabinet.
        /// </summary>
        /// <remarks>0x22</remarks>
        public ushort CabinetIndex { get; private set; }

        /// <summary>
        /// Total size of the normal Cabinet header in bytes
        /// </summary>
        public const int Size = 0x24;

        #endregion

        #region Extended Header

        /// <summary>
        /// The number of bytes reserved in the header area of the cabinet.
        /// 
        /// If this is non-zero and flags has MSCAB_HDR_RESV set, this data can
        /// be read by the calling application. It is of the given length,
        /// located at offset (base_offset + MSCAB_HDR_RESV_OFFSET) in the
        /// cabinet file.
        /// </summary>
        /// <remarks>0x24</remarks>
        /// <see cref="Flags"/>
        public ushort HeaderReserved { get; private set; }

        /// <summary>
        /// The number of bytes reserved in the folder area of the cabinet
        /// </summary>
        /// <remarks>0x26</remarks>
        public byte FolderReserved { get; private set; }

        /// <summary>
        /// The number of bytes reserved in the data area of the cabinet
        /// </summary>
        /// <remarks>0x27</remarks>
        public byte DataReserved { get; private set; }

        /// <summary>
        /// Total size of the extended Cabinet header in bytes
        /// </summary>
        public const int ExtendedSize = 0x04;

        #endregion

        /// <summary>
        /// Private constructor
        /// </summary>
        private _CabinetHeader() { }

        /// <summary>
        /// Create a _CabinetHeader from a byte array, if possible
        /// </summary>
        public static Error Create(byte[] buffer, out _CabinetHeader header)
        {
            header = null;
            if (buffer == null || buffer.Length < Size)
                return Error.MSPACK_ERR_READ;

            header = new _CabinetHeader();

            header.Signature = BitConverter.ToUInt32(buffer, 0x00);
            if (header.Signature != 0x4643534D)
                return Error.MSPACK_ERR_SIGNATURE;

            header.Reserved1 = BitConverter.ToUInt32(buffer, 0x04);
            header.CabinetSize = BitConverter.ToInt32(buffer, 0x08);
            header.Reserved2 = BitConverter.ToUInt32(buffer, 0x0C);
            header.FileOffset = BitConverter.ToUInt32(buffer, 0x10);
            header.Reserved3 = BitConverter.ToUInt32(buffer, 0x14);

            // Expect version 1.3, but don't validate
            header.MinorVersion = buffer[0x18];
            header.MajorVersion = buffer[0x19];

            header.NumFolders = BitConverter.ToInt16(buffer, 0x1A);
            if (header.NumFolders == 0)
                return Error.MSPACK_ERR_DATAFORMAT;

            header.NumFiles = BitConverter.ToInt16(buffer, 0x1C);
            if (header.NumFiles == 0)
                return Error.MSPACK_ERR_DATAFORMAT;

            header.Flags = (HeaderFlags)BitConverter.ToUInt16(buffer, 0x1E);
            header.SetID = BitConverter.ToUInt16(buffer, 0x20);
            header.CabinetIndex = BitConverter.ToUInt16(buffer, 0x20);

            return Error.MSPACK_ERR_OK;
        }

        /// <summary>
        /// Populate the extended header fields from buffer
        /// </summary>
        /// <param name="buffer"></param>
        public void PopulateExtendedHeader(byte[] buffer)
        {
            if (buffer.Length < ExtendedSize)
                return;

            // Expect less than 60,000, but don't validate
            HeaderReserved = BitConverter.ToUInt16(buffer, 0x00);
            FolderReserved = buffer[0x02];
            DataReserved = buffer[0x03];
        }
    }
}
