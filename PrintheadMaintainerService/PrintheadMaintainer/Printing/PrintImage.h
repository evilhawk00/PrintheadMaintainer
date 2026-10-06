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

#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

// The image the user selects is never parsed by the service. The UI decodes the file and sends
// raw pixels: 24-bit BGR, rows top to bottom, each row padded to a multiple of four bytes. The
// service only checks the sizes and writes the pixels into a bitmap file whose headers it
// generates itself, so the printing code only ever loads files of this one simple format.
namespace PrintImage
{
    constexpr uint32_t kMaxDimension = 10000;
    constexpr uint64_t kMaxPixelCount = 40'000'000;

    constexpr size_t RowStride(uint32_t width)
    {
        return (static_cast<size_t>(width) * 3 + 3) / 4 * 4;
    }

    bool IsValidSize(uint64_t width, uint64_t height);

    // Writes the pixels to a temporary file and moves it over the destination, so the
    // destination always holds a complete image. pixels.size() must be RowStride(width) * height.
    bool Save(const std::wstring& path, uint32_t width, uint32_t height, const std::vector<uint8_t>& pixels);
}
