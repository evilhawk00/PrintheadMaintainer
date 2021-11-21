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

int bCompareDateTime_If_B_is_Later_Than_A(int intYear_A, int intMonth_A, int intDay_A, int intHour_A, int intMinute_A, int intSecond_A, int intYear_B, int intMonth_B, int intDay_B, int intHour_B, int intMinute_B, int intSecond_B) {
	//return 1 if B is Later Than A
	//return 0 if B is the same with A
	//return 2 if A is Later Than B

	if (intYear_B > intYear_A) {
		return 1;
	}
	if (intYear_B < intYear_A) {
		return 2;
	}

	//AB have same year
	
	//compare month
	if (intMonth_B > intMonth_A) {
		return 1;
	}
	if (intMonth_B < intMonth_A) {
		return 2;
	}

	//AB have same month

	//compare day
	if (intDay_B > intDay_A) {
		return 1;
	}
	if (intDay_B < intDay_A) {
		return 2;
	}

	//AB have same day

	//compare hour
	if (intHour_B > intHour_A) {
		return 1;
	}
	if (intHour_B < intHour_A) {
		return 2;
	}

	//AB have same hour

	//compare Minute
	if (intMinute_B > intMinute_A) {
		return 1;
	}
	if (intMinute_B < intMinute_A) {
		return 2;
	}

	//AB have same minute

	//compare second
	if (intSecond_B > intSecond_A) {
		return 1;
	}
	if (intSecond_B < intSecond_A) {
		return 2;
	}

	//AB have same second, AB is the same
	return 0;

}

bool bIfDaysBetweenTwoDateIsMoreThan(const SYSTEMTIME &st_A, const SYSTEMTIME &st_B, unsigned __int64 intDaysToCheck) {
	//check if st_B is later than st_A && check if (st_B - st_A > intDaysToCheck), if both yes return true, otherwise return false

	
	FILETIME ft_A, ft_B;
	ULARGE_INTEGER t_A, t_B;
	ULONGLONG ulDifference;
	SystemTimeToFileTime(&st_A, &ft_A);
	SystemTimeToFileTime(&st_B, &ft_B);
	memcpy(&t_A, &ft_A, sizeof(t_A));
	memcpy(&t_B, &ft_B, sizeof(t_A));

	if (t_A.QuadPart < t_B.QuadPart) {
		ulDifference = t_B.QuadPart - t_A.QuadPart;
	}
	else {
		//ulDifference = t_A.QuadPart - t_B.QuadPart;
		return false;
	}

	if (ulDifference > (intDaysToCheck * 24 * 60 * 60) * (ULONGLONG)10000000)
	{
		return true;
	}
	else {
		return false;
	}


}

bool bBuildSeperateIntToSystemtime(SYSTEMTIME &stSystemtimeOutput, int intYear, int intMonth, int intDay, int intHour, int intMinute, int intSecond, int intMilliseconds, int intDayOfWeek) {

	//check if provided value is in correct format

	if (intYear < 1601 || intYear > 30827) {
		return false;
	}
	if (intMonth < 1 || intMonth > 12) {
		return false;
	}
	if (intDay < 1 || intDay > 31) {
		return false;
	}
	if (intHour < 0 || intHour > 23) {
		return false;
	}
	if (intMinute < 0 || intMinute > 59) {
		return false;
	}
	if (intSecond < 0 || intSecond > 59) {
		return false;
	}
	if (intMilliseconds < 0 || intMilliseconds > 999) {
		return false;
	}
	if (intDayOfWeek < 0 || intDayOfWeek > 6) {
		return false;
	}


	SYSTEMTIME st;
	st.wYear = intYear;
	st.wMonth = intMonth;
	st.wDay = intDay;
	st.wHour = intHour;
	st.wMinute = intMinute;
	st.wSecond = intSecond;
	st.wMilliseconds = intMilliseconds;
	st.wDayOfWeek = intDayOfWeek;

	stSystemtimeOutput = st;

	return true;

}