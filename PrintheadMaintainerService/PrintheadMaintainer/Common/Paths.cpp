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
#include "Paths.h"

#include <Windows.h>

namespace
{
    std::wstring QueryProgramDirectory()
    {
        std::wstring path(MAX_PATH, L'\0');
        for (;;)
        {
            const DWORD length = ::GetModuleFileNameW(nullptr, path.data(), static_cast<DWORD>(path.size()));
            if (length == 0)
            {
                return std::wstring();
            }
            if (length < path.size())
            {
                path.resize(length);
                break;
            }
            path.resize(path.size() * 2); // truncated; retry with a larger buffer
        }
        path.resize(path.find_last_of(L'\\') + 1);
        return path;
    }
}

namespace Paths
{
    const std::wstring& ProgramDirectory()
    {
        static const std::wstring directory = QueryProgramDirectory();
        return directory;
    }

    std::wstring DataDirectory()
    {
        return ProgramDirectory() + L"Data\\";
    }

    std::wstring LogDirectory()
    {
        return DataDirectory() + L"Logs\\";
    }

    std::wstring DefaultImagePath()
    {
        return ProgramDirectory() + L"Resources\\DefaultPrint.bmp";
    }

    std::wstring CustomImagePath()
    {
        return DataDirectory() + L"PrintImage.bmp";
    }
}
