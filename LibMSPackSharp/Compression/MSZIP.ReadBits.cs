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

namespace LibMSPackSharp.Compression
{
    public partial class MSZIP : CompressionStream
    {
        /// <inheritdoc/>
        public override void READ_BYTES()
        {
            READ_IF_NEEDED();
            if (Error != Error.MSPACK_ERR_OK)
                return;

            INJECT_BITS_LSB(InputBuffer[InputPointer++], 8);
        }
    }
}
