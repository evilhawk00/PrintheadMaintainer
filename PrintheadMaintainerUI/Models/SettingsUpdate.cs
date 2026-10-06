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
namespace PrintheadMaintainerUI.Models
{
    /// <summary>
    /// The settings to change in one request. The service validates every value before it stores
    /// any of them, so either all of the changes are applied or none. Null leaves a setting as it is.
    /// </summary>
    public sealed class SettingsUpdate
    {
        public bool? Enabled { get; set; }

        public int? IntervalDays { get; set; }

        public string PrinterName { get; set; }

        public int? PaperSource { get; set; }

        public ImageSelection Image { get; set; }
    }
}
