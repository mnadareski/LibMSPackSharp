/* libmspack -- a library for working with Microsoft compression formats.
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

using System;
using System.IO;

namespace LibMSPackSharp.CAB
{
    /// <summary>
    /// A structure which represents a single file in a cabinet or cabinet set.
    /// 
    /// All fields are READ ONLY.
    /// </summary>
    public class InternalFile
    {
        #region Internal

        /// <summary>
        /// File header information
        /// </summary>
        internal _FileHeader Header { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The next file in the cabinet or cabinet set, or NULL if this is the final file.
        /// </summary>
        public InternalFile Next { get; set; }

        /// <summary>
        /// The filename of the file.
        /// 
        /// A null terminated string of up to 255 bytes in length, it may be in
        /// either ISO-8859-1 or UTF8 format, depending on the file attributes.
        /// </summary>
        /// <see cref="_FileHeader.Attributes"/>
        public string Filename { get; set; }

        /// <summary>
        /// A pointer to the folder that contains this file.
        /// </summary>
        public Folder Folder { get; set; }

        #endregion

        #region Public Functionality

        /// <summary>
        /// Sets the last-modified time and file permissions on a file.
        /// </summary>
        /// <param name="filename">The name of the UNIX file whose last-modified time and file permissions will be set.</param>
        public bool SetDateAndPerm(string filename)
        {
            DateTime tm = new DateTime(
                Header.LastModifiedDateYear,
                Header.LastModifiedDateMonth,
                Header.LastModifiedDateDay,
                Header.LastModifiedTimeHour,
                Header.LastModifiedTimeMinute,
                Header.LastModifiedTimeSecond);
            
            try
            {
                FileInfo fi = new FileInfo(filename);
                fi.LastWriteTime = tm;

                System.IO.FileAttributes fa = 0;
                if (Header.Attributes.HasFlag(FileAttributes.MSCAB_ATTRIB_RDONLY))
                    fa |= System.IO.FileAttributes.ReadOnly;

                fi.Attributes = fa;

                return true;
            }
            catch
            {
                return false;
            }        }

        #endregion
    }
}
