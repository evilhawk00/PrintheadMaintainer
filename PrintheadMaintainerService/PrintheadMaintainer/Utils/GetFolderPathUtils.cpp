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
#include <string>
#include <Windows.h>
#include <strsafe.h>
#include <shlobj_core.h>

int wsGetProgramDataPath(std::wstring &wsOutputPath) {
    //return 0 if success
    //return -1 if failed

    PWSTR pszPath = NULL;

    HRESULT hr = SHGetKnownFolderPath(FOLDERID_ProgramData, KF_FLAG_CREATE, NULL, &pszPath);

    if (SUCCEEDED(hr)) {
        
        wsOutputPath = pszPath;
        
        CoTaskMemFree(pszPath);
        return 0;

    }

    CoTaskMemFree(pszPath);
    return -1;

}
