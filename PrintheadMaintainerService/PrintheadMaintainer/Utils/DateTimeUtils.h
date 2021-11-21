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

int bCompareDateTime_If_B_is_Later_Than_A(int intYear_A, int intMonth_A, int intDay_A, int intHour_A, int intMinute_A, int intSecond_A, int intYear_B, int intMonth_B, int intDay_B, int intHour_B, int intMinute_B, int intSecond_B);
bool bIfDaysBetweenTwoDateIsMoreThan(const SYSTEMTIME &st_A, const SYSTEMTIME &st_B, unsigned __int64 intDaysToCheck);
bool bBuildSeperateIntToSystemtime(SYSTEMTIME& stSystemtimeOutput, int intYear, int intMonth, int intDay, int intHour, int intMinute, int intSecond, int intMilliseconds, int intDayOfWeek);