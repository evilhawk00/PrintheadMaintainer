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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PrintheadMaintainerUI.NamedPipeClient
{
    /// <summary>
    /// A request to or a response from the service. The layout is described in the service's
    /// Ipc/Message.h: a 12-byte header ("PHM2", text size, data size), UTF-16 text whose first
    /// line is the command or result name followed by "Key=Value" lines, then optional binary data.
    /// </summary>
    public sealed class ServiceMessage
    {
        private const uint Magic = 0x324D4850; // "PHM2"
        private const int HeaderSize = 12;
        private const int MaxTextBytes = 64 * 1024;
        private const int MaxFields = 64;

        public const int MaxDataBytes = 128 * 1024 * 1024;

        private readonly List<KeyValuePair<string, string>> _fields = new List<KeyValuePair<string, string>>();

        public ServiceMessage(string name)
        {
            if (string.IsNullOrEmpty(name) || ContainsControlCharacters(name))
            {
                throw new ArgumentException("The message name must be a non-empty single line.", nameof(name));
            }
            Name = name;
        }

        public string Name { get; }

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public void Add(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf('=') >= 0 || ContainsControlCharacters(key))
            {
                throw new ArgumentException("Invalid field name.", nameof(key));
            }
            if (value == null || ContainsControlCharacters(value))
            {
                throw new ArgumentException("Field values cannot contain control characters.", nameof(value));
            }
            if (Get(key) != null)
            {
                throw new ArgumentException("Duplicate field name.", nameof(key));
            }
            _fields.Add(new KeyValuePair<string, string>(key, value));
        }

        /// <summary>Returns the value of the field, or null if the message does not have it.</summary>
        public string Get(string key)
        {
            foreach (KeyValuePair<string, string> field in _fields)
            {
                if (field.Key == key)
                {
                    return field.Value;
                }
            }
            return null;
        }

        public byte[] Serialize()
        {
            var text = new StringBuilder(Name);
            foreach (KeyValuePair<string, string> field in _fields)
            {
                text.Append('\n').Append(field.Key).Append('=').Append(field.Value);
            }

            byte[] textBytes = Encoding.Unicode.GetBytes(text.ToString());
            if (textBytes.Length > MaxTextBytes || Data.Length > MaxDataBytes || _fields.Count > MaxFields)
            {
                throw new InvalidOperationException("The message is too large.");
            }

            using (var stream = new MemoryStream(HeaderSize + textBytes.Length + Data.Length))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(textBytes.Length);
                writer.Write(Data.Length);
                writer.Write(textBytes);
                writer.Write(Data);
                return stream.ToArray();
            }
        }

        /// <summary>Returns null if the bytes are not a well-formed message.</summary>
        public static ServiceMessage Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderSize || BitConverter.ToUInt32(bytes, 0) != Magic)
            {
                return null;
            }

            uint textLength = BitConverter.ToUInt32(bytes, 4);
            uint dataLength = BitConverter.ToUInt32(bytes, 8);
            if (textLength % 2 != 0 || textLength > MaxTextBytes || dataLength > MaxDataBytes ||
                bytes.LongLength != HeaderSize + (long)textLength + dataLength)
            {
                return null;
            }

            string[] lines = Encoding.Unicode.GetString(bytes, HeaderSize, (int)textLength).Split('\n');
            if (lines[0].Length == 0 || ContainsControlCharacters(lines[0]))
            {
                return null;
            }

            var message = new ServiceMessage(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0)
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0 || message._fields.Count == MaxFields || ContainsControlCharacters(line) ||
                    message.Get(line.Substring(0, separator)) != null)
                {
                    return null;
                }
                message._fields.Add(new KeyValuePair<string, string>(line.Substring(0, separator), line.Substring(separator + 1)));
            }

            message.Data = new byte[dataLength];
            Buffer.BlockCopy(bytes, HeaderSize + (int)textLength, message.Data, 0, (int)dataLength);
            return message;
        }

        /// <summary>
        /// Control characters, including line breaks, are never allowed in names or values. The set
        /// is the same as the service's IsControlCharacter (Common/Text.h): C0 and C1 controls, the
        /// Unicode line and paragraph separators and the bidirectional formatting characters.
        /// </summary>
        public static bool ContainsControlCharacters(string text)
        {
            foreach (char ch in text)
            {
                if (IsControlCharacter(ch))
                {
                    return true;
                }
            }
            return false;
        }

        public static string ReplaceControlCharacters(string text)
        {
            var result = new StringBuilder(text);
            for (int i = 0; i < result.Length; i++)
            {
                if (IsControlCharacter(result[i]))
                {
                    result[i] = '?';
                }
            }
            return result.ToString();
        }

        private static bool IsControlCharacter(char ch)
        {
            return ch < 0x20 || (ch >= 0x7F && ch <= 0x9F) || ch == 0x2028 || ch == 0x2029 ||
                ch == 0x061C || ch == 0x200E || ch == 0x200F || (ch >= 0x202A && ch <= 0x202E) ||
                (ch >= 0x2066 && ch <= 0x2069);
        }
    }
}
