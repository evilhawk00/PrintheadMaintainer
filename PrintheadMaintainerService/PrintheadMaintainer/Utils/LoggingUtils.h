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
#include <Windows.h>

int intUtf8Log_CreateNewUTF8LogFileAndWriteWstring(const std::wstring& wsLogFilePath, const std::wstring& wsWriteContent);
int intUtf8Log_WriteUtf8BomAndWstring(HANDLE hFile, const std::wstring& wsWriteContent);
int intUtf8Log_PrependNewLineAndTruncateOldLastLine(HANDLE hFile, const std::wstring& wsPrependContent, bool bTruncateOldLastLineOrNot, long lFilePointerEndOfSecondLastLine);
int intCountLinesInTextFile(HANDLE hFile, long& lOutputFilePointerEndOfFirstLine, long& lOutputFilePointerEndOfSecondLastLine);