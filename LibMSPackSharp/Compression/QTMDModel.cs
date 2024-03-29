﻿/* This file is part of libmspack.
 * (C) 2003-2004 Stuart Caie.
 *
 * The Quantum method was created by David Stafford, adapted by Microsoft
 * Corporation.
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

namespace LibMSPackSharp.Compression
{
    public class QTMDModel
    {
        public int ShiftsLeft { get; set; }

        public int Entries { get; set; }

        public QTMDModelSym[] Syms { get; set; }
    }
}
