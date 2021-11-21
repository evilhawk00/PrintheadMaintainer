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
namespace PrintheadMaintainerUI.Singletons
{
    public sealed class PrinterNameSingletons
    {
        private static readonly PrinterNameSingletons instance = new PrinterNameSingletons();

        static PrinterNameSingletons()
        {
           
        }

        private PrinterNameSingletons()
        {
            
        }

        public static PrinterNameSingletons Instance
        {
            get
            {
                return instance;
            }
        }
        public string strPrinterName { get; set; }
    }
}
