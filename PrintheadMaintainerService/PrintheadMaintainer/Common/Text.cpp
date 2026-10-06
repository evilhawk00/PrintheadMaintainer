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
#include "Text.h"

#include <Windows.h>
#include <limits>

bool IsControlCharacter(wchar_t ch)
{
    return ch < 0x20 || (ch >= 0x7F && ch <= 0x9F) || ch == 0x2028 || ch == 0x2029 ||
        ch == 0x061C || ch == 0x200E || ch == 0x200F || (ch >= 0x202A && ch <= 0x202E) ||
        (ch >= 0x2066 && ch <= 0x2069);
}

bool ContainsControlCharacters(std::wstring_view text)
{
    for (wchar_t ch : text)
    {
        if (IsControlCharacter(ch))
        {
            return true;
        }
    }
    return false;
}

std::wstring ReplaceControlCharacters(std::wstring_view text)
{
    std::wstring result(text);
    for (wchar_t& ch : result)
    {
        if (IsControlCharacter(ch))
        {
            ch = L'?';
        }
    }
    return result;
}

std::optional<uint64_t> ParseDecimal(std::wstring_view text)
{
    if (text.empty())
    {
        return std::nullopt;
    }

    constexpr uint64_t kMax = (std::numeric_limits<uint64_t>::max)();
    uint64_t value = 0;
    for (wchar_t ch : text)
    {
        if (ch < L'0' || ch > L'9')
        {
            return std::nullopt;
        }
        const uint64_t digit = static_cast<uint64_t>(ch - L'0');
        if (value > (kMax - digit) / 10)
        {
            return std::nullopt;
        }
        value = value * 10 + digit;
    }
    return value;
}

std::string ToUtf8(std::wstring_view text)
{
    if (text.empty())
    {
        return std::string();
    }

    const int length = ::WideCharToMultiByte(CP_UTF8, 0, text.data(), static_cast<int>(text.size()),
        nullptr, 0, nullptr, nullptr);
    if (length <= 0)
    {
        return std::string();
    }

    std::string result(static_cast<size_t>(length), '\0');
    ::WideCharToMultiByte(CP_UTF8, 0, text.data(), static_cast<int>(text.size()),
        result.data(), length, nullptr, nullptr);
    return result;
}
