﻿/* This file is part of libmspack.
 * (C) 2003-2004 Stuart Caie.
 *
 * The Quantum method was created by David Stafford, adapted by Microsoft
 * Corporation.
 *
 * This decompressor is based on an implementation by Matthew Russotto, used
 * with permission.
 *
 * libmspack is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * For further details, see the file COPYING.LIB distributed with libmspack
 */

/* Quantum decompression implementation */

/* This decompressor was researched and implemented by Matthew Russotto. It
 * has since been tidied up by Stuart Caie. More information can be found at
 * http://www.speakeasy.org/~russotto/quantumcomp.html
 */

using System;
using System.IO;
using static LibMSPackSharp.Compression.Constants;

namespace LibMSPackSharp.Compression
{
    public partial class QTM
    {
        /// <summary>
        /// allocates Quantum decompression state for decoding the given stream.
        /// 
        ///  - returns null if window_bits is outwith the range 10 to 21 (inclusive).
        ///  - uses system.alloc() to allocate memory
        ///  - returns null if not enough memory
        ///  - window_bits is the size of the Quantum window, from 1Kb(10) to 2Mb(21).
        ///  - input_buffer_size is the number of bytes to use to store bitstream data.
        /// </summary>
        public static QTM Init(SystemImpl system, FileStream input, FileStream output, int window_bits, int input_buffer_size)
        {
            uint window_size = (uint)(1 << window_bits);

            if (system == null)
                return null;

            // Quantum supports window sizes of 2^10 (1Kb) through 2^21 (2Mb)
            if (window_bits < 10 || window_bits > 21)
                return null;

            // Round up input buffer size to multiple of two
            input_buffer_size = (input_buffer_size + 1) & -2;
            if (input_buffer_size < 2)
                return null;

            // Allocate decompression state
            QTM qtm = new QTM()
            {
                // Allocate decompression window and input buffer
                Window = new byte[window_size],
                InputBuffer = new byte[input_buffer_size],

                // Initialise decompression state
                System = system,
                InputFileHandle = input,
                OutputFileHandle = output,
                InputBufferSize = (uint)input_buffer_size,
                WindowSize = window_size,
                WindowPosition = 0,
                FrameTODO = QTM_FRAME_SIZE,
                HeaderRead = 0,
                Error = Error.MSPACK_ERR_OK,

                InputPointer = 0,
                InputEnd = 0,
                OutputPointer = 0,
                OutputEnd = 0,
                BitBuffer = 0,
                BitsLeft = 0,
                EndOfInput = 0,
            };

            // Initialise arithmetic coding models
            // - model 4    depends on window size, ranges from 20 to 24
            // - model 5    depends on window size, ranges from 20 to 36
            // - model 6pos depends on window size, ranges from 20 to 42

            int i = window_bits * 2;
            qtm.InitModel(qtm.Model0, qtm.Model0Symbols, 0, 64);
            qtm.InitModel(qtm.Model1, qtm.Model1Symbols, 64, 64);
            qtm.InitModel(qtm.Model2, qtm.Model2Symbols, 128, 64);
            qtm.InitModel(qtm.Model3, qtm.Model3Symbols, 192, 64);
            qtm.InitModel(qtm.Model4, qtm.Model4Symbols, 0, (i > 24) ? 24 : i);
            qtm.InitModel(qtm.Model5, qtm.Model5Symbols, 0, (i > 36) ? 36 : i);
            qtm.InitModel(qtm.Model6, qtm.Model6Symbols, 0, i);
            qtm.InitModel(qtm.Model6Len, qtm.Model6LenSymbols, 0, 27);
            qtm.InitModel(qtm.Model7, qtm.Model7Symbols, 0, 7);

            // All ok
            return qtm;
        }

        /// <summary>
        /// Decompresses, or decompresses more of, a Quantum stream.
        /// 
        /// - out_bytes of data will be decompressed and the function will return
        ///   with an MSPACK_ERR_OK return code.
        ///
        /// - decompressing will stop as soon as out_bytes is reached. if the true
        ///   amount of bytes decoded spills over that amount, they will be kept for
        ///   a later invocation of qtmd_decompress().
        ///
        /// - the output bytes will be passed to the system.write() function given in
        ///   qtmd_init(), using the output file handle given in qtmd_init(). More
        ///   than one call may be made to system.write()
        ///
        /// - Quantum will read input bytes as necessary using the system.read()
        ///   function given in qtmd_init(), using the input file handle given in
        ///   qtmd_init(). This will continue until system.read() returns 0 bytes,
        ///   or an error.
        /// </summary>
        public Error Decompress(long out_bytes)
        {
            uint frame_todo, frame_end, window_posn, match_offset, range;
            byte[] window;
            int runsrc, rundest;
            int i, j, selector, extra, sym, match_length;
            ushort H, L, C, symf;

            // Easy answers
            if (out_bytes < 0)
                return Error.MSPACK_ERR_ARGS;

            if (Error != Error.MSPACK_ERR_OK)
                return Error;

            // Flush out any stored-up bytes before we begin
            i = OutputEnd - OutputPointer;
            if (i > out_bytes)
                i = (int)out_bytes;

            if (i > 0)
            {
                if (System.Write(OutputFileHandle, Window, OutputPointer, i) != i)
                    return Error = Error.MSPACK_ERR_WRITE;

                OutputPointer += i;
                out_bytes -= i;
            }

            if (out_bytes == 0)
                return Error.MSPACK_ERR_OK;

            // Restore local state
            window = Window;
            window_posn = WindowPosition;
            frame_todo = FrameTODO;
            H = High;
            L = Low;
            C = Current;

            // While we do not have enough decoded bytes in reserve
            while ((OutputEnd - OutputPointer) < out_bytes)
            {
                // Read header if necessary. Initialises H, L and C
                if (HeaderRead != 0)
                {
                    H = 0xFFFF;
                    L = 0;
                    C = (ushort)READ_BITS_MSB(16);
                    HeaderRead = 1;
                }

                // Decode more, up to the number of bytes needed, the frame boundary,
                // or the window boundary, whichever comes first
                frame_end = (uint)(window_posn + (out_bytes - (OutputEnd - OutputPointer)));
                if ((window_posn + frame_todo) < frame_end)
                    frame_end = window_posn + frame_todo;
                if (frame_end > WindowSize)
                    frame_end = WindowSize;

                while (window_posn < frame_end)
                {
                    selector = GET_SYMBOL(Model7, ref H, ref L, ref C);
                    if (selector < 4)
                    {
                        // Literal byte
                        QTMDModel mdl;
                        switch (selector)
                        {
                            case 0:
                                mdl = Model0;
                                break;

                            case 1:
                                mdl = Model1;
                                break;

                            case 2:
                                mdl = Model2;
                                break;

                            case 3:
                            default:
                                mdl = Model3;
                                break;
                        }

                        sym = GET_SYMBOL(mdl, ref H, ref L, ref C);
                        window[window_posn++] = (byte)sym;
                        frame_todo--;
                    }
                    else
                    {
                        // Match repeated string
                        switch (selector)
                        {
                            // Selector 4 = fixed length match (3 bytes)
                            case 4:
                                sym = GET_SYMBOL(Model4, ref H, ref L, ref C);
                                extra = (int)READ_MANY_BITS_MSB(QTMExtraBits[sym]);
                                match_offset = (uint)(QTMPositionBase[sym] + extra + 1);
                                match_length = 3;
                                break;

                            // Selector 5 = fixed length match (4 bytes)
                            case 5:
                                sym = GET_SYMBOL(Model5, ref H, ref L, ref C);
                                extra = (int)READ_MANY_BITS_MSB(QTMExtraBits[sym]);
                                match_offset = (uint)(QTMPositionBase[sym] + extra + 1);
                                match_length = 4;
                                break;

                            // Selector 6 = variable length match
                            case 6:
                                sym = GET_SYMBOL(Model6Len, ref H, ref L, ref C);
                                extra = (int)READ_MANY_BITS_MSB(QTMLengthExtra[sym]);
                                match_length = QTMLengthBase[sym] + extra + 5;

                                sym = GET_SYMBOL(Model6, ref H, ref L, ref C);
                                extra = (int)READ_MANY_BITS_MSB(QTMExtraBits[sym]);
                                match_offset = (uint)(QTMPositionBase[sym] + extra + 1);
                                break;

                            default:
                                // Should be impossible, model7 can only return 0-6
                                Console.WriteLine($"Got {selector} from selector");
                                return Error = Error.MSPACK_ERR_DECRUNCH;
                        }

                        rundest = (int)window_posn;
                        frame_todo -= (uint)match_length;

                        // Does match destination wrap the window? This situation is possible
                        // where the window size is less than the 32k frame size, but matches
                        // must not go beyond a frame boundary
                        if ((window_posn + match_length) > WindowSize)
                        {
                            // Copy first part of match, before window end
                            i = (int)(WindowSize - window_posn);
                            j = (int)(window_posn - match_offset);

                            while (i-- > 0)
                            {
                                window[rundest++] = window[j++ & (WindowSize - 1)];
                            }

                            // Flush currently stored data
                            i = (int)(WindowSize - OutputPointer);

                            // This should not happen, but if it does then this code
                            // can't handle the situation (can't flush up to the end of
                            // the window, but can't break out either because we haven't
                            // finished writing the match). Bail out in this case
                            if (i > out_bytes)
                            {
                                Console.WriteLine($"During window-wrap match; {i} bytes to flush but only need {out_bytes}");
                                return Error = Error.MSPACK_ERR_DECRUNCH;
                            }

                            if (System.Write(OutputFileHandle, window, OutputPointer, i) != i)
                                return Error = Error.MSPACK_ERR_WRITE;

                            out_bytes -= i;
                            OutputPointer = 0;
                            OutputEnd = 0;

                            // Copy second part of match, after window wrap
                            rundest = 0;
                            i = (int)(match_length - (WindowSize - window_posn));
                            while (i-- > 0)
                            {
                                window[rundest++] = window[j++ & (WindowSize - 1)];
                            }

                            window_posn = (uint)(window_posn + match_length - WindowSize);

                            break; // Because "window_posn < frame_end" has now failed
                        }
                        else
                        {
                            // Normal match - output won't wrap window or frame end
                            i = match_length;

                            // Does match _offset_ wrap the window?
                            if (match_offset > window_posn)
                            {
                                // j = length from match offset to end of window
                                j = (int)(match_offset - window_posn);
                                if (j > (int)WindowSize)
                                {
                                    Console.WriteLine("Match offset beyond window boundaries");
                                    return Error = Error.MSPACK_ERR_DECRUNCH;
                                }

                                runsrc = (int)(WindowSize - j);
                                if (j < i)
                                {
                                    // If match goes over the window edge, do two copy runs
                                    i -= j;
                                    while (j-- > 0)
                                    {
                                        window[rundest++] = window[runsrc++];
                                    }

                                    runsrc = 0;
                                }

                                while (i-- > 0)
                                {
                                    window[rundest++] = window[runsrc++];
                                }
                            }
                            else
                            {
                                runsrc = (int)(rundest - match_offset);
                                while (i-- > 0)
                                {
                                    window[rundest++] = window[runsrc++];
                                }
                            }

                            window_posn += (uint)match_length;
                        }
                    }
                }

                OutputEnd = (int)window_posn;

                // If we subtracted too much from frame_todo, it will
                // wrap around past zero and go above its max value
                if (frame_todo > QTM_FRAME_SIZE)
                {
                    Console.WriteLine("Overshot frame alignment");
                    return Error = Error.MSPACK_ERR_DECRUNCH;
                }

                // Another frame completed?
                if (frame_todo == 0)
                {
                    // Re-align input
                    if ((BitsLeft & 7) != 0)
                        REMOVE_BITS_MSB(BitsLeft & 7);

                    // Special Quantum hack -- cabd.c injects a trailer byte to allow the
                    // decompressor to realign itself. CAB Quantum blocks, unlike LZX
                    // blocks, can have anything from 0 to 4 trailing null bytes.
                    do
                    {
                        i = (int)READ_BITS_MSB(8);
                    } while (i != 0xFF);

                    HeaderRead = 0;

                    frame_todo = QTM_FRAME_SIZE;
                }

                // Window wrap?
                if (window_posn == WindowSize)
                {
                    // Flush all currently stored data
                    i = (OutputEnd - OutputPointer);

                    // Break out if we have more than enough to finish this request
                    if (i >= out_bytes)
                        break;

                    if (System.Write(OutputFileHandle, window, OutputPointer, i) != i)
                        return Error = Error.MSPACK_ERR_WRITE;

                    out_bytes -= i;
                    OutputPointer = 0;
                    OutputEnd = 0;
                    window_posn = 0;
                }

            }

            if (out_bytes > 0)
            {
                i = (int)out_bytes;

                if (System.Write(OutputFileHandle, window, OutputPointer, i) != i)
                    return Error = Error.MSPACK_ERR_WRITE;

                OutputPointer += i;
            }

            // Store local state
            WindowPosition = window_posn;
            FrameTODO = frame_todo;
            High = H;
            Low = L;
            Current = C;

            return Error.MSPACK_ERR_OK;
        }

        private ushort GET_SYMBOL(QTMDModel model, ref ushort H, ref ushort L, ref ushort C)
        {
            uint range = (uint)((H - L) & 0xFFFF) + 1;
            ushort symf = (ushort)(((((C - L + 1) * model.Syms[0].CumulativeFrequency) - 1) / range) & 0xFFFF);

            int i = 1;
            for (; i < model.Entries; i++)
            {
                if (model.Syms[i].CumulativeFrequency <= symf)
                    break;
            }

            ushort temp = model.Syms[i - 1].Sym;

            range = (uint)(H - L) + 1;
            symf = model.Syms[0].CumulativeFrequency;
            H = (ushort)(L + ((model.Syms[i - 1].CumulativeFrequency * range) / symf) - 1);
            L = (ushort)(L + ((model.Syms[i].CumulativeFrequency * range) / symf));

            do
            {
                model.Syms[--i].CumulativeFrequency += 8;
            } while (i > 0);

            if (model.Syms[0].CumulativeFrequency > 3800)
                UpdateModel(model);

            while (true)
            {
                if ((L & 0x8000) != (H & 0x8000))
                {
                    if ((L & 0x4000) != 0 && (H & 0x4000) == 0)
                    {
                        // Underflow case
                        C ^= 0x4000;
                        L &= 0x3FFF;
                        H |= 0x4000;
                    }
                    else
                    {
                        break;
                    }
                }

                L <<= 1;
                H = (ushort)((H << 1) | 1);

                ENSURE_BITS(1);
                C = (ushort)((C << 1) | (PEEK_BITS_MSB(1)));
                REMOVE_BITS_MSB(1);
            }

            return temp;
        }

        private void UpdateModel(QTMDModel model)
        {
            QTMDModelSym tmp;
            int i, j;

            if (--model.ShiftsLeft > 0)
            {
                for (i = model.Entries - 1; i >= 0; i--)
                {
                    // -1, not -2; the 0 entry saves this
                    model.Syms[i].CumulativeFrequency >>= 1;
                    if (model.Syms[i].CumulativeFrequency <= model.Syms[i + 1].CumulativeFrequency)
                        model.Syms[i].CumulativeFrequency = (ushort)(model.Syms[i + 1].CumulativeFrequency + 1);
                }
            }
            else
            {
                model.ShiftsLeft = 50;
                for (i = 0; i < model.Entries; i++)
                {
                    // No -1, want to include the 0 entry

                    // This converts CumFreqs into frequencies, then shifts right
                    model.Syms[i].CumulativeFrequency -= model.Syms[i + 1].CumulativeFrequency;
                    model.Syms[i].CumulativeFrequency++; // Avoid losing things entirely
                    model.Syms[i].CumulativeFrequency >>= 1;
                }

                // Now sort by frequencies, decreasing order -- this must be an
                // inplace selection sort, or a sort with the same (in)stability
                // characteristics
                for (i = 0; i < model.Entries - 1; i++)
                {
                    for (j = i + 1; j < model.Entries; j++)
                    {
                        if (model.Syms[i].CumulativeFrequency < model.Syms[j].CumulativeFrequency)
                        {
                            tmp = model.Syms[i];
                            model.Syms[i] = model.Syms[j];
                            model.Syms[j] = tmp;
                        }
                    }
                }

                // Then convert frequencies back to CumFreq
                for (i = model.Entries - 1; i >= 0; i--)
                {
                    model.Syms[i].CumulativeFrequency += model.Syms[i + 1].CumulativeFrequency;
                }
            }
        }

        private void InitModel(QTMDModel model, QTMDModelSym[] syms, int start, int len)
        {
            model.ShiftsLeft = 4;
            model.Entries = len;
            model.Syms = syms;

            for (int i = 0; i <= len; i++)
            {
                // Actual symbol
                syms[i].Sym = (ushort)(start + i);

                // Current frequency of that symbol
                syms[i].CumulativeFrequency = (ushort)(len - i);
            }
        }
    }
}
