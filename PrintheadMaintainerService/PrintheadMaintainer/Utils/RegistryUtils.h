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
#pragma once

#include <string>
#include <vector>

#include <Windows.h>

HKEY hKeyOpenRegistyHandle();
bool bCloseRegistryHandle(HKEY hKeyHandle);
bool bRegistryWriteSingleLineString(HKEY hKeyHandle, LPCWSTR lpValueName, const std::wstring& wsValueData);
bool bRegistryWriteInteger(HKEY hKeyHandle, LPCWSTR lpValueName, int intValueData);
int intRegistryReadSingleLineString(HKEY hKeyHandle, LPCWSTR lpValueName, std::wstring& wsOutputQueryResult);
int intRegistryReadInteger(HKEY hKeyHandle, LPCWSTR lpValueName, int& intOutputQueryResult);