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
using PrintheadMaintainerUI.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintheadMaintainerUI.Printing
{
    /// <summary>
    /// Turns a BMP file into the pixels the service prints. The file is decoded here, in the
    /// user's own process, so that the service never parses image files. Images larger than the
    /// service accepts are scaled down, keeping their proportions; the service stretches the image
    /// over the whole page anyway. Decoding a large image takes a while, so call it from a
    /// background thread.
    /// </summary>
    public static class PrintImageLoader
    {
        /// <exception cref="IOException">The file cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The user may not read the file.</exception>
        /// <exception cref="InvalidDataException">The file is not a BMP image that can be decoded.</exception>
        public static PrintImage Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    // Without caching, pixels are decoded (and scaled) row by row as CopyPixels
                    // asks for them, so even a huge image never has to fit in memory at full size.
                    var decoder = new BmpBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
                    BitmapSource source = decoder.Frames[0];
                    double scale = GetScale(source.PixelWidth, source.PixelHeight);
                    if (scale < 1)
                    {
                        source = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                    }
                    source = new FormatConvertedBitmap(source, PixelFormats.Bgr24, null, 0);

                    int width = source.PixelWidth;
                    int height = source.PixelHeight;
                    int stride = PrintImage.RowStride(width);
                    var pixels = new byte[stride * height];
                    source.CopyPixels(pixels, stride, 0);
                    return new PrintImage(width, height, pixels, Path.GetFileName(path));
                }
                catch (Exception e) when (e is NotSupportedException || e is FormatException || e is ArgumentException ||
                    e is OverflowException || e is ExternalException)
                {
                    throw new InvalidDataException("The file is not a BMP image that can be printed.", e);
                }
            }
        }

        private static double GetScale(int width, int height)
        {
            double scale = Math.Min((double)PrintImage.MaxDimension / Math.Max(width, height),
                Math.Sqrt((double)PrintImage.MaxPixelCount / ((double)width * height)));

            // Stay a little below the limits so that rounding cannot push the result over them.
            return scale >= 1 ? 1 : scale * 0.999;
        }
    }
}
