/*
* 
* Copyright (C) 2021  YAN-LIN, CHEN
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
using Microsoft.Win32;
using System;

namespace PrintheadMaintainerUI.Utils
{
    public static class FileSelectorUtils
    {

        public static string StrBrowseBmpFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter
            dlg.DefaultExt = ".bmp";
            dlg.Filter = "BMP Files (*.bmp)|*.bmp";

            Nullable<bool> nbResult = dlg.ShowDialog();

            if (nbResult.HasValue && nbResult.Value)
            {
                string strFilePath = dlg.FileName;
                return strFilePath;
            }

            return null;

        }

    }
}
