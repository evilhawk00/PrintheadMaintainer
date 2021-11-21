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
#include <vector>
#include <Windows.h>

//hkey path
#define DEF_HKEY_PATH L"SOFTWARE\\evilhawk00\\Printhead Maintainer"

HKEY hKeyOpenRegistyHandle() {
	//create key if key is not exist

	HKEY hKey;
	//DWORD dwOpenOrCreate; //REG_CREATED_NEW_KEY | REG_OPENED_EXISTING_KEY

	long lResult = RegCreateKeyExW(HKEY_LOCAL_MACHINE, DEF_HKEY_PATH, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE | KEY_SET_VALUE, NULL, &hKey, NULL);
	if (lResult != ERROR_SUCCESS) {

		//function failed
		return NULL;

	}

	return hKey;
}

bool bCloseRegistryHandle(HKEY hKeyHandle) {

	if (hKeyHandle != NULL) {

		long lResult = RegCloseKey(hKeyHandle);
		if (lResult != ERROR_SUCCESS) {

			//function failed
			return false;

		}


	}
	return true;
}

bool bRegistryWriteSingleLineString(HKEY hKeyHandle, LPCWSTR lpValueName, const std::wstring& wsValueData) {
	//only for datatype REG_SZ but not REG_MULTI_SZ!!



	long lResult = RegSetValueExW(hKeyHandle, lpValueName, 0, REG_SZ, (const BYTE*)wsValueData.c_str(), (wsValueData.length() + 1) * sizeof(wchar_t));
	if (lResult != ERROR_SUCCESS) {

		//function failed
		return false;

	}

	return true;

}

bool bRegistryWriteInteger(HKEY hKeyHandle, LPCWSTR lpValueName, int intValueData) {
	//only for datatype REG_DWORD!!

	long lResult = RegSetValueExW(hKeyHandle, lpValueName, 0, REG_DWORD, (const BYTE*)&intValueData, sizeof(intValueData));
	if (lResult != ERROR_SUCCESS) {

		//function failed
		return false;

	}

	return true;

}

int intRegistryReadSingleLineString(HKEY hKeyHandle, LPCWSTR lpValueName, std::wstring& wsOutputQueryResult) {
	//only for datatype REG_SZ but not REG_MULTI_SZ!!
	//return 0 if no error
	//return -1 if value not exist or other error
	//return -2 if datatype not match
	//queried value returns in wsOutputQueryResult

	DWORD dwDataTypeOfReceivedData;

	wchar_t Buffer[MAX_PATH];
	DWORD dwSize = sizeof(Buffer);

	long lResult = RegQueryValueExW(hKeyHandle, lpValueName, NULL, &dwDataTypeOfReceivedData, (LPBYTE)&Buffer, &dwSize);
	if (lResult != ERROR_SUCCESS) {

		//function failed
		return -1;

	}

	//check if returned datatype is correct
	if (dwDataTypeOfReceivedData != REG_SZ) {
		return -2;
	}

	wsOutputQueryResult = Buffer;

	return 0;
}

int intRegistryReadInteger(HKEY hKeyHandle, LPCWSTR lpValueName, int& intOutputQueryResult) {
	//only for datatype REG_DWORD!!
	//return 0 if no error
	//return -1 if value not exist or other error
	//return -2 if datatype not match
	//queried value returns in intOutputQueryResult

	DWORD dwDataTypeOfReceivedData;

	DWORD dwQueriedData;
	DWORD dwSize = sizeof(dwQueriedData);

	long lResult = RegQueryValueExW(hKeyHandle, lpValueName, NULL, &dwDataTypeOfReceivedData, (LPBYTE)&dwQueriedData, &dwSize);
	if (lResult != ERROR_SUCCESS) {

		//function failed
		return -1;

	}

	//check if returned datatype is correct
	if (dwDataTypeOfReceivedData != REG_DWORD) {
		return -2;
	}

	intOutputQueryResult = dwQueriedData;

	return 0;

}