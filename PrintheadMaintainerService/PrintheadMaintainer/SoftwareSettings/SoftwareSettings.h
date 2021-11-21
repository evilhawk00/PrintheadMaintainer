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

#include<Windows.h>

#define DEF_Registry_Enabled L"Enabled"

#define DEF_Registry_BmpFilePath L"BmpFilePath"
#define DEF_Registry_PrinterName L"PrinterName"

#define DEF_Registry_LastPrintTime_Year L"LastPrint-Year"
#define DEF_Registry_LastPrintTime_Month L"LastPrint-Month"
#define DEF_Registry_LastPrintTime_Day L"LastPrint-Day"
#define DEF_Registry_LastPrintTime_Hour L"LastPrint-Hour"
#define DEF_Registry_LastPrintTime_Minute L"LastPrint-Minute"
#define DEF_Registry_LastPrintTime_Second L"LastPrint-Second"
#define DEF_Registry_LastPrintTime_Milliseconds L"LastPrint-Milliseconds"
#define DEF_Registry_LastPrintTime_DayOfWeek L"LastPrint-DayOfWeek"

#define DEF_Registry_IntervalDay L"Interval-Day"

#define DEF_Registry_Scheduled_PrintFailRecord_Year L"ScheduledFail-Year"
#define DEF_Registry_Scheduled_PrintFailRecord_Month L"ScheduledFail-Month"
#define DEF_Registry_Scheduled_PrintFailRecord_Day L"ScheduledFail-Day"
#define DEF_Registry_Scheduled_PrintFailRecord_Hour L"ScheduledFail-Hour"
#define DEF_Registry_Scheduled_PrintFailRecord_Minute L"ScheduledFail-Minute"
#define DEF_Registry_Scheduled_PrintFailRecord_Second L"ScheduledFail-Second"
#define DEF_Registry_Scheduled_PrintFailRecord_DayOfWeek L"ScheduledFail-DayOfWeek"

#define DEF_Registry_Scheduled_PrintFailRecord_Displayed L"ScheduledFailDisplayed"


#define DEF_Registry_Manual_PrintFailRecord_Year L"ManualFail-Year"
#define DEF_Registry_Manual_PrintFailRecord_Month L"ManualFail-Month"
#define DEF_Registry_Manual_PrintFailRecord_Day L"ManualFail-Day"
#define DEF_Registry_Manual_PrintFailRecord_Hour L"ManualFail-Hour"
#define DEF_Registry_Manual_PrintFailRecord_Minute L"ManualFail-Minute"
#define DEF_Registry_Manual_PrintFailRecord_Second L"ManualFail-Second"
#define DEF_Registry_Manual_PrintFailRecord_DayOfWeek L"ManualFail-DayOfWeek"

#define DEF_Registry_Manual_PrintFailRecord_Displayed L"ManualFailDisplayed"


extern int gs_intEnabled;
extern bool gs_bLastPrintTimeValid;
extern int gs_intLastPrintYear;
extern int gs_intLastPrintMonth;
extern int gs_intLastPrintDay;
extern int gs_intLastPrintHour;
extern int gs_intLastPrintMinute;
extern int gs_intLastPrintSecond;
extern int gs_intLastPrintMilliseconds;
extern int gs_intLastPrintDayOfWeek;
extern int gs_intIntervalDay;

extern std::wstring gs_wsBmpPath;
extern std::wstring gs_wsPrinterName;


bool bLoadSoftwareSettingFromRegistry(bool bCheckEnabledAndInterval);
bool bWriteLastPrintTimeToRegistry();
bool bWritePrintinigFailureRecordToRegistry(bool bIsScheduledRecordOrNOT);
bool bChangeRegistryPrintingFailureRecordNotifyValue(HKEY hKey, bool bDisplayed, bool bIsScheduledRecordOrNOT);