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
namespace PrintheadMaintainerUI.NamedPipeClient
{
    public enum PrintNowResult
    {
        Accepted,      // the service prints in the background; the status shows the outcome
        Busy,          // a manual print is already queued or running
        NotConfigured, // no printer has been selected
        Failed,        // any other answer
        NotConnected,  // the service could not be reached or did not answer in time
    }
}
