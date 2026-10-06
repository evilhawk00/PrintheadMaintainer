/*
*
* Copyright (C) 2026  YAN-LIN, CHEN
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
*
*/
using System;

namespace PrintheadMaintainerUI.Models
{
    /// <summary>
    /// An image decoded by the UI, in the only format the service accepts (see PrintImage.h in
    /// the service): 24-bit BGR pixels, rows top to bottom, each row padded to a multiple of four
    /// bytes. The service never parses image files itself.
    /// </summary>
    public sealed class PrintImage
    {
        public const int MaxDimension = 10000;
        public const long MaxPixelCount = 40000000;

        public PrintImage(int width, int height, byte[] pixels, string sourceName)
        {
            if (width < 1 || height < 1 || width > MaxDimension || height > MaxDimension ||
                (long)width * height > MaxPixelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "The image size is not supported.");
            }
            if (pixels == null || pixels.Length != RowStride(width) * height)
            {
                throw new ArgumentException("The pixel data does not match the image size.", nameof(pixels));
            }

            Width = width;
            Height = height;
            Pixels = pixels;
            SourceName = sourceName ?? string.Empty;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] Pixels { get; }

        /// <summary>The name of the file the image was decoded from, for display only.</summary>
        public string SourceName { get; }

        public static int RowStride(int width)
        {
            return (width * 3 + 3) / 4 * 4;
        }
    }
}
