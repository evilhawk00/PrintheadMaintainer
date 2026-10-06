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
#include <optional>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

// Protocol between the UI and the service (the UI side is NamedPipeClient/ServiceMessage.cs).
//
// Each connection carries one request and one response, each sent as a single pipe message:
//
//   offset 0   uint32  magic, the bytes "PHM2"
//   offset 4   uint32  size of the text part in bytes
//   offset 8   uint32  size of the binary part in bytes
//   offset 12  text    UTF-16LE lines separated by '\n'. The first line is the command name
//                      (request) or the result name (response); every further line is
//                      "Key=Value", at most kMaxFields of them. Values never contain control
//                      characters (IsControlCharacter in Common/Text.h).
//   ...        binary  raw data; only used for image pixels in ApplySettings
//
// All integers are little-endian.
namespace Ipc
{
    constexpr uint32_t kMagic = 0x324D4850; // "PHM2"
    constexpr size_t kHeaderSize = 12;
    constexpr size_t kMaxTextBytes = 64 * 1024;
    constexpr size_t kMaxDataBytes = 128 * 1024 * 1024;
    constexpr size_t kMaxFields = 64;
    constexpr size_t kMaxMessageBytes = kHeaderSize + kMaxTextBytes + kMaxDataBytes;

    struct Message
    {
        std::wstring name;
        std::vector<std::pair<std::wstring, std::wstring>> fields;
        std::vector<uint8_t> data;

        explicit Message(std::wstring messageName = std::wstring()) : name(std::move(messageName)) {}

        const std::wstring* Find(std::wstring_view key) const;

        // Control characters in the value are replaced, so the message is always well-formed.
        void Add(std::wstring key, std::wstring_view value);
    };

    // Takes the received bytes (moved in, so that large image data is not copied).
    // Returns nothing for malformed messages: a bad header, sizes that do not add up, an empty
    // name, lines without '=', empty or duplicate keys, too many fields, or control characters.
    std::optional<Message> Parse(std::vector<uint8_t> bytes);

    // Throws std::length_error if the message exceeds the limits above.
    std::vector<uint8_t> Serialize(const Message& message);
}
