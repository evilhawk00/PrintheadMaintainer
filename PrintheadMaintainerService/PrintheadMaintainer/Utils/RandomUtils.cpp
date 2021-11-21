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
#include <stdlib.h>
#include <time.h>

bool bInitialized = false;

static const char RandomHexPool[] = "0123456789ABCDEF";
static const int RanPoolSize = sizeof(RandomHexPool) - 1;

wchar_t GetRamdomChar() {
	return RandomHexPool[rand() % RanPoolSize];
}

std:: wstring wsGenerateRamdomHexString(int intNumberOfChar) {

	if (!bInitialized) {
		//set random seed
		srand((unsigned int)time(0));
		bInitialized = true;
	}

	std::wstring wsRandomString = L"";
	for (int i = 0; i < intNumberOfChar -1 ; i++) {
		wsRandomString += GetRamdomChar();
	}

	return wsRandomString;
}
