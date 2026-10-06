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
#include "PrintImage.h"

#include <Windows.h>

#include "../Common/WinHandle.h"

namespace
{
    bool WriteAll(HANDLE file, const void* data, size_t size)
    {
        const auto* bytes = static_cast<const BYTE*>(data);
        while (size > 0)
        {
            const DWORD chunk = size > 0x40000000 ? 0x40000000 : static_cast<DWORD>(size);
            DWORD written = 0;
            if (!::WriteFile(file, bytes, chunk, &written, nullptr) || written == 0)
            {
                return false;
            }
            bytes += written;
            size -= written;
        }
        return true;
    }
}

namespace PrintImage
{
    bool IsValidSize(uint64_t width, uint64_t height)
    {
        return width >= 1 && height >= 1 && width <= kMaxDimension && height <= kMaxDimension &&
            width * height <= kMaxPixelCount;
    }

    bool Save(const std::wstring& path, uint32_t width, uint32_t height, const std::vector<uint8_t>& pixels)
    {
        const size_t stride = RowStride(width);
        if (!IsValidSize(width, height) || pixels.size() != stride * height)
        {
            return false;
        }

        BITMAPINFOHEADER info{};
        info.biSize = sizeof(info);
        info.biWidth = static_cast<LONG>(width);
        info.biHeight = static_cast<LONG>(height); // positive height: rows are stored bottom up
        info.biPlanes = 1;
        info.biBitCount = 24;
        info.biCompression = BI_RGB;
        info.biSizeImage = static_cast<DWORD>(pixels.size());

        BITMAPFILEHEADER header{};
        header.bfType = 0x4D42; // "BM"
        header.bfOffBits = sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER);
        header.bfSize = header.bfOffBits + info.biSizeImage;

        const std::wstring temporaryPath = path + L".tmp";
        {
            UniqueFileHandle file(::CreateFileW(temporaryPath.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
                FILE_ATTRIBUTE_NORMAL, nullptr));
            if (!file)
            {
                return false;
            }

            bool written = WriteAll(file.Get(), &header, sizeof(header)) && WriteAll(file.Get(), &info, sizeof(info));
            for (uint32_t row = height; written && row > 0; --row)
            {
                written = WriteAll(file.Get(), pixels.data() + (row - 1) * stride, stride);
            }
            if (!written || !::FlushFileBuffers(file.Get()))
            {
                file.Reset();
                ::DeleteFileW(temporaryPath.c_str());
                return false;
            }
        }

        if (!::MoveFileExW(temporaryPath.c_str(), path.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
        {
            ::DeleteFileW(temporaryPath.c_str());
            return false;
        }
        return true;
    }
}
