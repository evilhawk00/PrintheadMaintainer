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

#include <mutex>
#include <Windows.h>


#define DEF_intLogType_Scheduled_Printing_OK 1
#define DEF_intLogType_Scheduled_Printing_FAIL 2
#define DEF_intLogType_Manual_Printing_OK 3
#define DEF_intLogType_Manual_Printing_FAIL 4
#define DEF_intLogType_ServiceStart 5
#define DEF_intLogType_ServiceStop 6
#define DEF_intLogType_Settings_Change_Image_To 7
#define DEF_intLogType_Settings_Change_Interval_To 8
#define DEF_intLogType_Settings_Change_Enabled_ON 9
#define DEF_intLogType_Settings_Change_Enabled_OFF 10
#define DEF_intLogType_Settings_Change_Printer_To 11

namespace Logging
{

    std::wstring wsGetCompleteLogLineByInt(int intLogType, const std::wstring &wsCustomParameter);
    std::wstring wsGetLogMessageByInt(int intLogType, const std::wstring &wsCustomParameter);
    std::wstring wsGenerateTimeStampString();

    int intWriteLogToLogFile_ThreadSafe(int intLogType, const std::wstring& wsCustomParameter);

};