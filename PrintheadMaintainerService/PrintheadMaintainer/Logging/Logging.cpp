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
#include "Logging.h"

#include <string>
#include <Shlwapi.h>

#include "../Utils/GetFolderPathUtils.h"
#include "../Utils/FileSizeUtils.h"
#include "../Utils/WriteFileUtils.h"
#include "../Utils/LoggingUtils.h"

#pragma comment ( lib,"Shlwapi.lib" ) 

#define MAX_LINES_IN_LOG_FILE 300

static std::mutex m_lock;

int Logging::intWriteLogToLogFile_ThreadSafe(int intLogType, const std::wstring &wsCustomParameter) {

    //multi-thread lock
    std::lock_guard<std::mutex> lock(m_lock);

    //generate line to write log
    std::wstring wsLogLineContent = wsGetCompleteLogLineByInt(intLogType, wsCustomParameter);

    //open file handle
    std::wstring wsProgramDataPath;
    int intResult = wsGetProgramDataPath(wsProgramDataPath);
    if (intResult != 0) {
        //get programdata path failed
        return -1;
    }

    std::wstring wsLogFileDirectory = wsProgramDataPath + L"\\Printhead Maintainer\\";

    //try create directory if not exist
    int intResultCD = CreateDirectory(wsLogFileDirectory.c_str(), NULL);
    if (intResultCD == 0) {
        //create directory failed, now check if its because ERROR_ALREADY_EXISTS
        if (GetLastError() != ERROR_ALREADY_EXISTS) {
            //its not because ERROR_ALREADY_EXISTS, something wrong, abort
            return -1;
        }
    }

    std::wstring wsLogFilePath = wsLogFileDirectory + L"Printhead Maintainer.log";

    bool bResultPFE = PathFileExists(wsLogFilePath.c_str());


    

    if (bResultPFE) {
        //target log file exists

        HANDLE  hFile = NULL;
        hFile = CreateFile(wsLogFilePath.c_str(), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);


        if (hFile == INVALID_HANDLE_VALUE) {

            return -1;

        }

        //now check file size
        bool bResult = bIsFileSizeBiggerThanLimit(hFile, 2);
        //checkfile size, if > 2mb, clear it

        if (bResult) {
            //truncate the file and just write append
            int intResultTFE = intTruncateFileToEmpty(hFile);
            if (intResultTFE != 0) {
                //failed
                CloseHandle(hFile);
                return -1;
            }

            //write from begging
            
            intUtf8Log_WriteUtf8BomAndWstring(hFile, wsLogLineContent);
            CloseHandle(hFile);
        }
        else {
            //file size not exceed limit, readlines , check if lines more than 300 lines.
            long lFilePointerOfEndOfFirstLine;
            long lFilePointerOfEndOfSecondLastLine;
            int intLineCounts = intCountLinesInTextFile(hFile, lFilePointerOfEndOfFirstLine, lFilePointerOfEndOfSecondLastLine);
            if (intLineCounts == -1) {
                //error
                CloseHandle(hFile);
                return -1;
            }

            bool bTruncateLastLine = false;

            if (intLineCounts > MAX_LINES_IN_LOG_FILE) {

                bTruncateLastLine = true;
            }

            int intWriteLogResult = intUtf8Log_PrependNewLineAndTruncateOldLastLine(hFile, wsLogLineContent, bTruncateLastLine, lFilePointerOfEndOfSecondLastLine);

            if (intWriteLogResult != 0) {
            
                CloseHandle(hFile);
                return -1;
            }

            CloseHandle(hFile);

        }

    }
    else {
        //target log file not exist
        //just write new line and end

        int intResult = intUtf8Log_CreateNewUTF8LogFileAndWriteWstring(wsLogFilePath, wsLogLineContent);
        if (intResult != 0) {
            return -1;
        }

        
    }

    return 0;

}

std::wstring Logging::wsGetCompleteLogLineByInt(int intLogType, const std::wstring &wsCustomParameter) {

    return wsGenerateTimeStampString() + L"  " + wsGetLogMessageByInt(intLogType, wsCustomParameter);

}

std::wstring Logging::wsGenerateTimeStampString() {

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

    std::wstring wsFixedMonth;
    std::wstring wsFixedDay;
    std::wstring wsFixedHour;
    std::wstring wsFixedMinute;
    std::wstring wsFixedSecond;

    if (intLocalMonth < 10) {
        wsFixedMonth = L"0" + std::to_wstring(intLocalMonth);
    }
    else {
        wsFixedMonth = std::to_wstring(intLocalMonth);
    }

    if (intLocalDay < 10) {
        wsFixedDay = L"0" + std::to_wstring(intLocalDay);
    }
    else {
        wsFixedDay = std::to_wstring(intLocalDay);
    }

    if (intLocalHour < 10) {
        wsFixedHour = L"0" + std::to_wstring(intLocalHour);
    }
    else {
        wsFixedHour = std::to_wstring(intLocalHour);
    }

    if (intLocalMinute < 10) {
        wsFixedMinute = L"0" + std::to_wstring(intLocalMinute);
    }
    else {
        wsFixedMinute = std::to_wstring(intLocalMinute);
    }

    if (intLocalSecond < 10) {
        wsFixedSecond = L"0" + std::to_wstring(intLocalSecond);
    }
    else {
        wsFixedSecond = std::to_wstring(intLocalSecond);
    }

    std::wstring wsFullTimeStamp = std::to_wstring(intLocalYear) + L"/" + wsFixedMonth + L"/" + wsFixedDay + L" " + wsFixedHour + L":" + wsFixedMinute + L":" + wsFixedSecond;

    return wsFullTimeStamp;
}

std::wstring Logging::wsGetLogMessageByInt(int intLogType, const std::wstring &wsCustomParameter) {

    std::wstring wsWindowsNewLineMarker = L"\r\n";

    switch (intLogType) {
    case DEF_intLogType_Scheduled_Printing_OK:
        return L"Scheduled printing succeed" + wsWindowsNewLineMarker;
    case DEF_intLogType_Scheduled_Printing_FAIL:
        return L"Scheduled printing failed" + wsWindowsNewLineMarker;
    case DEF_intLogType_Manual_Printing_OK:
        return L"Manual printing succeed" + wsWindowsNewLineMarker;
    case DEF_intLogType_Manual_Printing_FAIL:
        return L"Manual printing failed" + wsWindowsNewLineMarker;
    case DEF_intLogType_ServiceStart: {
        DWORD dwPID = GetCurrentProcessId();
        return L"Service has been started, PID " + std::to_wstring(dwPID) + wsWindowsNewLineMarker;
    }
    case DEF_intLogType_ServiceStop:
        return L"Service has been shutdown" + wsWindowsNewLineMarker;
    case DEF_intLogType_Settings_Change_Image_To:
        return L"The target image for printing has been changed to " + wsCustomParameter + wsWindowsNewLineMarker;
    case DEF_intLogType_Settings_Change_Interval_To:
        return L"The Scheduled printing interval has been changed to " + wsCustomParameter + L" days" + wsWindowsNewLineMarker;
    case DEF_intLogType_Settings_Change_Enabled_ON:
        return L"The scheduled printing has been set to enabled" + wsWindowsNewLineMarker;
    case DEF_intLogType_Settings_Change_Enabled_OFF:
        return L"The scheduled printing has been set to disabled" + wsWindowsNewLineMarker;
    case DEF_intLogType_Settings_Change_Printer_To:
        return L"The target printer has been changed to " + wsCustomParameter + wsWindowsNewLineMarker;
    default:
        return L"Unknown error occured" + wsWindowsNewLineMarker;

    }

}