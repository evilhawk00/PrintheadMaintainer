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
    /// <summary>The image the service prints: the one installed with the program, or a custom one.</summary>
    public sealed class ImageSelection
    {
        public static readonly ImageSelection Default = new ImageSelection(null);

        private ImageSelection(PrintImage customImage)
        {
            CustomImage = customImage;
        }

        /// <summary>Null for the image installed with the program.</summary>
        public PrintImage CustomImage { get; }

        public static ImageSelection Custom(PrintImage image)
        {
            return new ImageSelection(image ?? throw new ArgumentNullException(nameof(image)));
        }
    }
}
