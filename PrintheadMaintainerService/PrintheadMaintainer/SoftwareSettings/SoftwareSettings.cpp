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

#include "../Utils/RegistryUtils.h"
#include "../Utils/DateTimeUtils.h"
#include "SoftwareSettings.h"

//extern global
int gs_intEnabled;
bool gs_bLastPrintTimeValid;
int gs_intLastPrintYear;
int gs_intLastPrintMonth;
int gs_intLastPrintDay;
int gs_intLastPrintHour;
int gs_intLastPrintMinute;
int gs_intLastPrintSecond;
int gs_intLastPrintMilliseconds;
int gs_intLastPrintDayOfWeek;
int gs_intIntervalDay;

std::wstring gs_wsBmpPath;
std::wstring gs_wsPrinterName;
//end extern global

bool bReadBmpSettingAndCheckBmp(HKEY hKeyHandle, std::wstring& wsOutputBmpPath) {
    //return true if Bmp path value exist and Bmp is accessible in filesystem

    std::wstring wsQueriedValue_BmpPath;

    int intResult = intRegistryReadSingleLineString(hKeyHandle, DEF_Registry_BmpFilePath, wsQueriedValue_BmpPath);
    if (intResult != 0) {
        //error occured
        return false;
    }


    //check read access to Bmp file
    if ((_waccess_s(wsQueriedValue_BmpPath.c_str(), 4)) != 0) {
        //we do not have read access to target filepath
        return false;
    }

    wsOutputBmpPath = wsQueriedValue_BmpPath;
    return true;

}

bool bReadLastPrintTime(HKEY hKeyHandle, int& intOutputYear, int& intOutputMonth, int& intOutputDay, int& intOutputHour, int& intOutputMinute, int& intOutputSecond, int& intOutputMilliseconds, int& intOutputDayOfWeek) {
    //return true if LastPrintTimeData Exist
    //return false if LastPrintTimeData not Exist

    int intResult_Year = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Year, intOutputYear);
    if (intResult_Year != 0) {
        //read settings failed

        return false;

    }

    int intResult_Month = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Month, intOutputMonth);
    if (intResult_Month != 0) {
        //read settings failed

        return false;

    }

    int intResult_Day = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Day, intOutputDay);
    if (intResult_Day != 0) {
        //read settings failed

        return false;

    }

    int intResult_Hour = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Hour, intOutputHour);
    if (intResult_Hour != 0) {
        //read settings failed

        return false;

    }

    int intResult_Minute = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Minute, intOutputMinute);
    if (intResult_Minute != 0) {
        //read settings failed

        return false;

    }

    int intResult_Second = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Second, intOutputSecond);
    if (intResult_Second != 0) {
        //read settings failed

        return false;

    }

    int intResult_Milliseconds = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_Milliseconds, intOutputMilliseconds);
    if (intResult_Milliseconds != 0) {
        //read settings failed

        return false;

    }

    int intResult_DayOfWeek = intRegistryReadInteger(hKeyHandle, DEF_Registry_LastPrintTime_DayOfWeek, intOutputDayOfWeek);
    if (intResult_DayOfWeek != 0) {
        //read settings failed

        return false;

    }

    return true;

}

bool bIfLastPrintTimeIsValid(int intLastPrintYear, int intLastPrintMonth, int intLastPrintDay, int intLastPrintHour, int intLastPrintMinute, int intLastPrintSecond, int intLastPrintMilliseconds, int intLastPrintDayOfWeek) {
    //check if LastPrintTime has correct format
    //check if LastPrintTime is in Future

    if (intLastPrintYear < 1601 || intLastPrintYear > 30827) {

        return false;

    }

    if (intLastPrintMonth < 1 || intLastPrintMonth > 12) {

        return false;

    }

    if (intLastPrintDay < 1) {

        return false;

    }
    else {

        int intMaxDayLimit;

        switch (intLastPrintMonth) {
        case 1:
            intMaxDayLimit = 31;
            break;
        case 2:
            intMaxDayLimit = 29;
            break;
        case 3:
            intMaxDayLimit = 31;
            break;
        case 4:
            intMaxDayLimit = 30;
            break;
        case 5:
            intMaxDayLimit = 31;
            break;
        case 6:
            intMaxDayLimit = 30;
            break;
        case 7:
            intMaxDayLimit = 31;
            break;
        case 8:
            intMaxDayLimit = 31;
            break;
        case 9:
            intMaxDayLimit = 30;
            break;
        case 10:
            intMaxDayLimit = 31;
            break;
        case 11:
            intMaxDayLimit = 30;
            break;
        case 12:
            intMaxDayLimit = 31;
            break;
        default:
            intMaxDayLimit = 31;
            break;
        }

        if (intLastPrintDay > intMaxDayLimit) {

            return false;

        }

    }

    if (intLastPrintHour < 0 || intLastPrintHour > 23) {

        return false;

    }

    if (intLastPrintMinute < 0 || intLastPrintMinute > 59) {

        return false;

    }

    if (intLastPrintSecond < 0 || intLastPrintSecond > 59) {

        return false;

    }

    if (intLastPrintMilliseconds < 0 || intLastPrintMilliseconds > 999) {

        return false;
    }

    if (intLastPrintDayOfWeek < 0 || intLastPrintDayOfWeek > 6) {

        return false;
    }


    //check if LastPrintTime is in Future
    //GetCurrentDateTime
    SYSTEMTIME st;
    GetLocalTime(&st);

    int intLocalYear = (int)st.wYear;
    int intLocalMonth = (int)st.wMonth;
    int intLocalDay = (int)st.wDay;
    int intLocalHour = (int)st.wHour;
    int intLocalMinute = (int)st.wMinute;
    int intLocalSecond = (int)st.wSecond;

    int intResult = bCompareDateTime_If_B_is_Later_Than_A(intLocalYear, intLocalMonth, intLocalDay, intLocalHour, intLocalMinute, intLocalSecond, intLastPrintYear, intLastPrintMonth, intLastPrintDay, intLastPrintHour, intLastPrintMinute, intLastPrintSecond);
    if (intResult != 2) {
        //LastPrintTime is not before current local time
        return false;
    }

    return true;
}

bool bWriteLastPrintTimeToRegistry() {

    HKEY hKey;

    hKey = hKeyOpenRegistyHandle();

    if (hKey == NULL) {
        return false;
    }

    //GetCurrentDateTime
    SYSTEMTIME st;
    GetLocalTime(&st);

    int intLocalYear = (int)st.wYear;
    int intLocalMonth = (int)st.wMonth;
    int intLocalDay = (int)st.wDay;
    int intLocalHour = (int)st.wHour;
    int intLocalMinute = (int)st.wMinute;
    int intLocalSecond = (int)st.wSecond;
    int intLocalMilliseconds = (int)st.wMilliseconds;
    int intLocalDayOfWeek = (int)st.wDayOfWeek;


    boolean bResult_Year = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Year, intLocalYear);
    boolean bResult_Month = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Month, intLocalMonth);
    boolean bResult_Day = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Day, intLocalDay);
    boolean bResult_Hour = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Hour, intLocalHour);
    boolean bResult_Minute = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Minute, intLocalMinute);
    boolean bResult_Second = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Second, intLocalSecond);
    boolean bResult_Milliseconds = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_Milliseconds, intLocalMilliseconds);
    boolean bResult_DayOfWeek = bRegistryWriteInteger(hKey, DEF_Registry_LastPrintTime_DayOfWeek, intLocalDayOfWeek);

    bCloseRegistryHandle(hKey);

    if (bResult_Year && bResult_Month && bResult_Day && bResult_Hour && bResult_Minute && bResult_Second && bResult_Milliseconds && bResult_DayOfWeek) {

        return true;

    }
    else {

        return false;

    }
    

}

bool bWritePrintinigFailureRecordToRegistry(bool bIsScheduledRecordOrNOT) {
    //If bIsScheduledRecordOrNOT is true, it writes the failure record of Scheduled Printing
    //If bIsScheduledRecordOrNot is false, it writes the failure record of Manual Printing


    HKEY hKey;

    hKey = hKeyOpenRegistyHandle();

    if (hKey == NULL) {
        return false;
    }

    //GetCurrentDateTime
    SYSTEMTIME st;
    GetLocalTime(&st);

    int intLocalYear = (int)st.wYear;
    int intLocalMonth = (int)st.wMonth;
    int intLocalDay = (int)st.wDay;
    int intLocalHour = (int)st.wHour;
    int intLocalMinute = (int)st.wMinute;
    int intLocalSecond = (int)st.wSecond;
    int intLocalDayOfWeek = (int)st.wDayOfWeek;

    boolean bResult_Year = false;
    boolean bResult_Month = false;
    boolean bResult_Day = false;
    boolean bResult_Hour = false;
    boolean bResult_Minute = false;
    boolean bResult_Second = false;
    boolean bResult_DayOfWeek = false;

    if (bIsScheduledRecordOrNOT) {
        //write Scheduled Printing failure log

        bResult_Year = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Year, intLocalYear);
        bResult_Month = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Month, intLocalMonth);
        bResult_Day = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Day, intLocalDay);
        bResult_Hour = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Hour, intLocalHour);
        bResult_Minute = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Minute, intLocalMinute);
        bResult_Second = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Second, intLocalSecond);
        bResult_DayOfWeek = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_DayOfWeek, intLocalDayOfWeek);
    
    }
    else {
        //write Manual Printing failure log
    
        bResult_Year = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Year, intLocalYear);
        bResult_Month = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Month, intLocalMonth);
        bResult_Day = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Day, intLocalDay);
        bResult_Hour = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Hour, intLocalHour);
        bResult_Minute = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Minute, intLocalMinute);
        bResult_Second = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Second, intLocalSecond);
        bResult_DayOfWeek = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_DayOfWeek, intLocalDayOfWeek);
    
    }

    if (bResult_Year && bResult_Month && bResult_Day && bResult_Hour && bResult_Minute && bResult_Second && bResult_DayOfWeek) {

        //now write displayed key to false
        //0 = not displayed, 1 = displayed
        boolean bResult = bChangeRegistryPrintingFailureRecordNotifyValue(hKey,false, bIsScheduledRecordOrNOT);
        bCloseRegistryHandle(hKey);

        if (bResult) {
            return true;
        }
        else {
            return false;
        }

        

    }
    else {

        bCloseRegistryHandle(hKey);
        return false;

    }



}

bool bChangeRegistryPrintingFailureRecordNotifyValue(HKEY hKey, bool bDisplayed, bool bIsScheduledRecordOrNOT) {
    //If bIsScheduledRecordOrNOT is true, it updates the failure record of Scheduled Printing
    //If bIsScheduledRecordOrNot is false, it updates the failure record of Manual Printing

    bool bResult = false;
    int intValueToWrite;

    if (bDisplayed) {
        intValueToWrite = 1;
    }
    else {
        intValueToWrite = 0;
    }


    if (hKey == NULL) {

        HKEY hKeyInternal = hKeyOpenRegistyHandle();

        if (hKeyInternal == NULL) {
            return false;
        }

        if (bIsScheduledRecordOrNOT) {
            //write Scheduled Printing failure log
            bResult = bRegistryWriteInteger(hKeyInternal, DEF_Registry_Scheduled_PrintFailRecord_Displayed, intValueToWrite);
        }
        else {
            //write Manual Printing failure log
            bResult = bRegistryWriteInteger(hKeyInternal, DEF_Registry_Manual_PrintFailRecord_Displayed, intValueToWrite);
        }

        bCloseRegistryHandle(hKeyInternal);
    }
    else {
    
        if (bIsScheduledRecordOrNOT) {
            //write Scheduled Printing failure log
            bResult = bRegistryWriteInteger(hKey, DEF_Registry_Scheduled_PrintFailRecord_Displayed, intValueToWrite);
        }
        else {
            //write Manual Printing failure log
            bResult = bRegistryWriteInteger(hKey, DEF_Registry_Manual_PrintFailRecord_Displayed, intValueToWrite);
        }
        
    
    }

    if (bResult) {
        return true;
    }
    else {
        return false;
    }
}

bool bLoadSoftwareSettingFromRegistry(bool bCheckEnabledAndInterval) {
    //return true if success with no error
    //return false if failed and Value Enabled has wrong value


    HKEY hKey;

    std::wstring wsQueriedValue_BmpFilePath;
    std::wstring wsQueriedValue_PrinterName;

    hKey = hKeyOpenRegistyHandle();

    if (hKey == NULL) {
        return false;
    }

    //check Bmp setting and Bmp File
    bool bResult_Bmp = bReadBmpSettingAndCheckBmp(hKey, wsQueriedValue_BmpFilePath);
    if (!bResult_Bmp) {
        //Bmp setting not exist or Bmp file is inaccessible in file system
        return false;
    }

    //assign queried value to global
    gs_wsBmpPath = wsQueriedValue_BmpFilePath;


    int intResult_PrinterName = intRegistryReadSingleLineString(hKey, DEF_Registry_PrinterName, wsQueriedValue_PrinterName);
    if (intResult_PrinterName != 0) {
        //error occured
        return false;
    }

    //Check Value of PrinterName
    if (wsQueriedValue_PrinterName.empty()) {
        //PrinterName is empty, error abort
        return false;
    }

    //assign queried value to global
    gs_wsPrinterName = wsQueriedValue_PrinterName;

    if (bCheckEnabledAndInterval) {

        bool bIfValueLastPrintTimeExist;

        int intQueriedValue_Enabled;

        int intQueriedValue_LastPrintYear;
        int intQueriedValue_LastPrintMonth;
        int intQueriedValue_LastPrintDay;
        int intQueriedValue_LastPrintHour;
        int intQueriedValue_LastPrintMinute;
        int intQueriedValue_LastPrintSecond;
        int intQueriedValue_LastPrintMilliseconds;
        int intQueriedValue_LastPrintDayOfWeek;
        int intQueriedValue_IntervalDay;



        int intResult_Enabled = intRegistryReadInteger(hKey, DEF_Registry_Enabled, intQueriedValue_Enabled);
        if (intResult_Enabled != 0) {
            //read settings failed

            bCloseRegistryHandle(hKey);
            return false;
            //notify UI this problem and quit
        }

        //check value of Enabled
        if (intQueriedValue_Enabled != 0 && intQueriedValue_Enabled != 1) {
            //this value is invalid

            bCloseRegistryHandle(hKey);
            return false;
        }

        //assign queried value to global
        gs_intEnabled = intQueriedValue_Enabled;



        //check interval-Day
        int intResult_IntervalDay = intRegistryReadInteger(hKey, DEF_Registry_IntervalDay, intQueriedValue_IntervalDay);
        if (intResult_IntervalDay != 0) {
            //error occured
            return false;
        }

        if (intQueriedValue_IntervalDay < 1) {
            //interval setting is not valid
            return false;
        }

        //assign queried value to global
        gs_intIntervalDay = intQueriedValue_IntervalDay;

        //now check LastPrintTime

        bIfValueLastPrintTimeExist = bReadLastPrintTime(hKey, intQueriedValue_LastPrintYear, intQueriedValue_LastPrintMonth, intQueriedValue_LastPrintDay,
            intQueriedValue_LastPrintHour, intQueriedValue_LastPrintMinute, intQueriedValue_LastPrintSecond, intQueriedValue_LastPrintMilliseconds, intQueriedValue_LastPrintDayOfWeek);

        if (bIfValueLastPrintTimeExist) {

            //validate the format of LastPrintTime
            bool bValidateResult = bIfLastPrintTimeIsValid(intQueriedValue_LastPrintYear, intQueriedValue_LastPrintMonth, intQueriedValue_LastPrintDay,
                intQueriedValue_LastPrintHour, intQueriedValue_LastPrintMinute, intQueriedValue_LastPrintSecond, intQueriedValue_LastPrintMilliseconds, intQueriedValue_LastPrintDayOfWeek);

            if (bValidateResult) {

                //assign queried value to global
                gs_bLastPrintTimeValid = true;

                gs_intLastPrintYear = intQueriedValue_LastPrintYear;
                gs_intLastPrintMonth = intQueriedValue_LastPrintMonth;
                gs_intLastPrintDay = intQueriedValue_LastPrintDay;
                gs_intLastPrintHour = intQueriedValue_LastPrintHour;
                gs_intLastPrintMinute = intQueriedValue_LastPrintMinute;
                gs_intLastPrintSecond = intQueriedValue_LastPrintSecond;
                gs_intLastPrintMilliseconds = intQueriedValue_LastPrintMilliseconds;
                gs_intLastPrintDayOfWeek = intQueriedValue_LastPrintDayOfWeek;

            }
            else {
                gs_bLastPrintTimeValid = false;
            }

        }
        else {
            gs_bLastPrintTimeValid = false;
        }


    }



    bCloseRegistryHandle(hKey);
    return true;

}


