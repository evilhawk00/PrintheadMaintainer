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
#include "Message.h"

#include <cstring>
#include <stdexcept>

#include "../Common/Text.h"

static_assert(sizeof(wchar_t) == 2, "the protocol text is UTF-16");

namespace
{
    uint32_t ReadUInt32(const uint8_t* bytes)
    {
        return static_cast<uint32_t>(bytes[0]) | (static_cast<uint32_t>(bytes[1]) << 8) |
            (static_cast<uint32_t>(bytes[2]) << 16) | (static_cast<uint32_t>(bytes[3]) << 24);
    }

    void AppendUInt32(std::vector<uint8_t>& bytes, uint32_t value)
    {
        for (int shift = 0; shift < 32; shift += 8)
        {
            bytes.push_back(static_cast<uint8_t>(value >> shift));
        }
    }
}

namespace Ipc
{
    const std::wstring* Message::Find(std::wstring_view key) const
    {
        for (const auto& field : fields)
        {
            if (field.first == key)
            {
                return &field.second;
            }
        }
        return nullptr;
    }

    void Message::Add(std::wstring key, std::wstring_view value)
    {
        fields.emplace_back(std::move(key), ReplaceControlCharacters(value));
    }

    std::optional<Message> Parse(std::vector<uint8_t> bytes)
    {
        if (bytes.size() < kHeaderSize || ReadUInt32(&bytes[0]) != kMagic)
        {
            return std::nullopt;
        }
        const size_t textBytes = ReadUInt32(&bytes[4]);
        const size_t dataBytes = ReadUInt32(&bytes[8]);
        if (textBytes % sizeof(wchar_t) != 0 || textBytes > kMaxTextBytes || dataBytes > kMaxDataBytes ||
            bytes.size() != kHeaderSize + textBytes + dataBytes)
        {
            return std::nullopt;
        }

        std::wstring text(textBytes / sizeof(wchar_t), L'\0');
        if (textBytes != 0)
        {
            std::memcpy(text.data(), bytes.data() + kHeaderSize, textBytes);
        }

        Message message;
        bool isFirstLine = true;
        size_t lineStart = 0;
        while (lineStart <= text.size())
        {
            size_t lineEnd = text.find(L'\n', lineStart);
            if (lineEnd == std::wstring::npos)
            {
                lineEnd = text.size();
            }
            const std::wstring_view line(text.data() + lineStart, lineEnd - lineStart);
            lineStart = lineEnd + 1;

            if (ContainsControlCharacters(line))
            {
                return std::nullopt;
            }
            if (isFirstLine)
            {
                if (line.empty())
                {
                    return std::nullopt;
                }
                message.name = std::wstring(line);
                isFirstLine = false;
                continue;
            }
            if (line.empty())
            {
                continue;
            }

            const size_t separator = line.find(L'=');
            if (separator == std::wstring_view::npos || separator == 0)
            {
                return std::nullopt;
            }
            std::wstring key(line.substr(0, separator));
            if (message.fields.size() == kMaxFields || message.Find(key) != nullptr)
            {
                return std::nullopt;
            }
            message.fields.emplace_back(std::move(key), std::wstring(line.substr(separator + 1)));
        }

        bytes.erase(bytes.begin(), bytes.begin() + static_cast<std::ptrdiff_t>(kHeaderSize + textBytes));
        message.data = std::move(bytes);
        return message;
    }

    std::vector<uint8_t> Serialize(const Message& message)
    {
        std::wstring text = message.name;
        for (const auto& field : message.fields)
        {
            text += L'\n';
            text += field.first;
            text += L'=';
            text += field.second;
        }

        const size_t textBytes = text.size() * sizeof(wchar_t);
        if (textBytes > kMaxTextBytes || message.data.size() > kMaxDataBytes || message.fields.size() > kMaxFields)
        {
            throw std::length_error("The message exceeds the protocol limits.");
        }

        std::vector<uint8_t> bytes;
        bytes.reserve(kHeaderSize + textBytes + message.data.size());
        AppendUInt32(bytes, kMagic);
        AppendUInt32(bytes, static_cast<uint32_t>(textBytes));
        AppendUInt32(bytes, static_cast<uint32_t>(message.data.size()));

        const auto* textData = reinterpret_cast<const uint8_t*>(text.data());
        bytes.insert(bytes.end(), textData, textData + textBytes);
        bytes.insert(bytes.end(), message.data.begin(), message.data.end());
        return bytes;
    }
}
