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

bool bIsWstringContentUnsignedInteger(const std::wstring& s)
{
	if (s.empty()) {
		return false;
	}

	if (s.find_first_not_of(L"0123456789") == std::wstring::npos) {
		//first character not matching 0-9 not found
		return true;
	}
	else {

		return false;
	}

}