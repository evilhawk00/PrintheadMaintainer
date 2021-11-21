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
#include <string>
#include <Windows.h>

std::string wstring_to_utf8_string(const std::wstring& s)
{
	std::string strUtf8;
	int intlen = WideCharToMultiByte(CP_UTF8, 0, s.c_str(), s.length(), NULL, 0, NULL, NULL);
	if (intlen > 0)
	{
		strUtf8.resize(intlen);
		WideCharToMultiByte(CP_UTF8, 0, s.c_str(), s.length(), &strUtf8[0], intlen, NULL, NULL);
	}
	return strUtf8;
}