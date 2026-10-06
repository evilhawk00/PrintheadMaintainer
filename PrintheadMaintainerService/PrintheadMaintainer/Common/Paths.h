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
#pragma once

#include <string>

// Locations used by the service. All directory paths end with a backslash.
//
// The data directory lives inside the installation directory. The installer creates it and
// grants the service account write access; standard users can only read it. Unlike a folder
// under ProgramData, users cannot create it in advance, so they can neither plant links in it
// nor replace the files the service reads.
namespace Paths
{
    const std::wstring& ProgramDirectory();
    std::wstring DataDirectory();
    std::wstring LogDirectory();

    // The image installed with the program, printed until the user selects one.
    std::wstring DefaultImagePath();

    // The service's own copy of the image the user selected.
    std::wstring CustomImagePath();
}
