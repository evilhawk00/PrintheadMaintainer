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
namespace PrintheadMaintainerUI.Interfaces
{
    /// <summary>Switches the page shown in the main window.</summary>
    public interface INavigator
    {
        void ShowHome();

        void ShowPrintNow();

        /// <summary>Shows the print page and starts printing, which the page then follows.</summary>
        void PrintNow();

        /// <summary>A print can be started now, see <see cref="PrintNow"/>.</summary>
        bool CanPrintNow { get; }

        void ShowPostpone();

        /// <summary>Shows the settings as the service has them, dropping unsaved changes.</summary>
        void ShowSettings();

        void ShowLogs();

        void ShowAbout();

        /// <summary>Lets the user pick a printer for the settings page.</summary>
        void ChoosePrinter(string selectedPrinter);

        /// <summary>Goes back to the settings page, keeping its unsaved changes.</summary>
        /// <param name="chosenPrinter">The printer the user picked, or null if they cancelled.</param>
        void ReturnToSettings(string chosenPrinter);
    }
}
