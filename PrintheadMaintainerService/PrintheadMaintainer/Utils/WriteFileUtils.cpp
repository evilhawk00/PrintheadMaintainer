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

int intTruncateFileToEmpty(HANDLE hFILE) {
	//return 0 if success
	//return -1 if failed

	if (hFILE != INVALID_HANDLE_VALUE) {

		//move file pointer to front
		bool bResult1 = SetFilePointerEx(hFILE, intToLargeInt(0), NULL, FILE_BEGIN);
		if (!bResult1) {
			//failed
			return -1;
		}

		bool bResult2 = SetEndOfFile(hFILE);
		if (!bResult2) {
			//failed	
			return -1;
		}
		else {
			return 0;
		}
	}
	else {

		return -1;
	}
}

DWORD dwGetFilePointer(HANDLE hFile) {
	//returns pointer if success
	//returns -1 if failed
	//doesn't work for file size bigger than 4GB

	DWORD dwResult = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);
	
	if (dwResult != INVALID_SET_FILE_POINTER) {
		
		return dwResult;
	}
	else {
	
		return -1;
	}
}