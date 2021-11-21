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
#include <Windows.h>

#include "LargeIntegerUtils.h"

bool bIsFileSizeBiggerThanLimit(HANDLE hFile, int intMBLimit) {
	//This supports files bigger than 4GB


	if (hFile != INVALID_HANDLE_VALUE) {
		LARGE_INTEGER liFileSizeLimit = intToLargeInt(intMBLimit * 1048576); //Bytes in binary,1MB = 1048576 Bytes in binary

		LARGE_INTEGER liFileSize;
		GetFileSizeEx(hFile, &liFileSize);


		if (liFileSize.QuadPart > liFileSizeLimit.QuadPart) {
			//Target Filesize bigger than the limit
			return true;
		}
		else {
			//Target Filesize smaller than limit
			return false;
		}



	}
	return false;
}