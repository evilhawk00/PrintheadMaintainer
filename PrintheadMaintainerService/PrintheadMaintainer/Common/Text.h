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

#include <cstdint>
#include <optional>
#include <string>
#include <string_view>

// True for C0/C1 control characters, the Unicode line and paragraph separators and the
// bidirectional formatting characters (which can make text display in a different order),
// none of which belong in printer names, file names or log entries.
bool IsControlCharacter(wchar_t ch);

bool ContainsControlCharacters(std::wstring_view text);

// Replaces control characters so that the text stays on a single line.
std::wstring ReplaceControlCharacters(std::wstring_view text);

// Parses a non-negative decimal number made of digits only (no sign, no spaces).
// Returns nothing when the text is empty, contains other characters or overflows.
std::optional<uint64_t> ParseDecimal(std::wstring_view text);

std::string ToUtf8(std::wstring_view text);
