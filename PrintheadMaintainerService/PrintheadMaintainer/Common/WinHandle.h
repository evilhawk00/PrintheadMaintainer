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

#include <Windows.h>

// Owns a Win32 handle and closes it when it goes out of scope.
// Traits provides the handle type, its invalid value and the close function.
template <typename Traits>
class UniqueHandle
{
public:
    using Handle = typename Traits::Handle;

    UniqueHandle() noexcept = default;
    explicit UniqueHandle(Handle handle) noexcept : m_handle(handle) {}
    ~UniqueHandle() { Reset(); }

    UniqueHandle(const UniqueHandle&) = delete;
    UniqueHandle& operator=(const UniqueHandle&) = delete;

    UniqueHandle(UniqueHandle&& other) noexcept : m_handle(other.Release()) {}
    UniqueHandle& operator=(UniqueHandle&& other) noexcept
    {
        if (this != &other)
        {
            Reset(other.Release());
        }
        return *this;
    }

    Handle Get() const noexcept { return m_handle; }
    bool IsValid() const noexcept { return m_handle != Traits::Invalid(); }
    explicit operator bool() const noexcept { return IsValid(); }

    // Closes the current handle and returns the address of the storage, for APIs
    // that return a handle through an out parameter.
    Handle* Put() noexcept
    {
        Reset();
        return &m_handle;
    }

    Handle Release() noexcept
    {
        Handle handle = m_handle;
        m_handle = Traits::Invalid();
        return handle;
    }

    void Reset(Handle handle = Traits::Invalid()) noexcept
    {
        if (IsValid())
        {
            Traits::Close(m_handle);
        }
        m_handle = handle;
    }

private:
    Handle m_handle = Traits::Invalid();
};

struct KernelHandleTraits
{
    using Handle = HANDLE;
    static Handle Invalid() noexcept { return nullptr; }
    static void Close(Handle handle) noexcept { ::CloseHandle(handle); }
};

// CreateFile and CreateNamedPipe report failure with INVALID_HANDLE_VALUE instead of NULL.
struct FileHandleTraits
{
    using Handle = HANDLE;
    static Handle Invalid() noexcept { return INVALID_HANDLE_VALUE; }
    static void Close(Handle handle) noexcept { ::CloseHandle(handle); }
};

struct RegKeyTraits
{
    using Handle = HKEY;
    static Handle Invalid() noexcept { return nullptr; }
    static void Close(Handle handle) noexcept { ::RegCloseKey(handle); }
};

struct LocalMemoryTraits
{
    using Handle = HLOCAL;
    static Handle Invalid() noexcept { return nullptr; }
    static void Close(Handle handle) noexcept { ::LocalFree(handle); }
};

using UniqueKernelHandle = UniqueHandle<KernelHandleTraits>;
using UniqueFileHandle = UniqueHandle<FileHandleTraits>;
using UniqueRegKey = UniqueHandle<RegKeyTraits>;
using UniqueLocalMemory = UniqueHandle<LocalMemoryTraits>;
