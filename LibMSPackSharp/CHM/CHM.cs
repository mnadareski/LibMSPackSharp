﻿/* libmspack -- a library for working with Microsoft compression formats.
 * (C) 2003-2019 Stuart Caie <kyzer@cabextract.org.uk>
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

namespace LibMSPackSharp.CHM
{
    /// <summary>
    /// A structure which represents a CHM helpfile.
    /// 
    /// All fields are READ ONLY.
    /// </summary>
    public class CHM : BaseHeader
    {
        #region Internal

        /// <summary>
        /// CHM header information
        /// </summary>
        internal _CHMHeader Header { get; set; }

        /// <summary>
        /// Header section table information
        /// </summary>
        internal _HeaderSectionTable HeaderSectionTable { get; set; }

        /// <summary>
        /// Header section 0 information
        /// </summary>
        internal _HeaderSection0 HeaderSection0 { get; set; }

        /// <summary>
        /// Header section 1 information
        /// </summary>
        internal _HeaderSection1 HeaderSection1 { get; set; }

        #endregion

        /// <summary>
        /// The filename of the CHM helpfile. This is given by the library user
        /// and may be in any format.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// A list of all non-system files in the CHM helpfile.
        /// </summary>
        public DecompressFile Files { get; set; }

        /// <summary>
        /// A list of all system files in the CHM helpfile.
        /// 
        /// System files are files which begin with "::". They are meta-files
        /// generated by the CHM creation process.
        /// </summary>
        public DecompressFile SysFiles { get; set; }

        /// <summary>
        /// The section 0 (uncompressed) data in this CHM helpfile.
        /// </summary>
        public UncompressedSection Sec0 { get; set; }

        /// <summary>
        /// The section 1 (MSCompressed) data in this CHM helpfile.
        /// </summary>
        public MSCompressedSection Sec1 { get; set; }

        /// <summary>
        /// A cache of loaded chunks, filled in by mschm_decoder::fast_find().
        /// Available only in CHM decoder version 2 and above.
        /// </summary>
        public byte[][] ChunkCache { get; set; }
    }
}
