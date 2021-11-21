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
#include <stdexcept>
#include <wtypes.h>

#include "../PrintingEntryPoint/PrintingEntryPoint.h"
#include "../SoftwareSettings/SoftwareSettings.h"
#include "../PrintingStatusReporter/PrintingStatusReporter.h"
#include "../Logging/Logging.h"
#include "../Utils/RegistryUtils.h"
#include "../Utils/WstringUtils.h"


#define JobFlag_PrintNow L"0x01"
#define JobFlag_WriteRegistryEnabled L"0x02"
#define JobFlag_WriteRegistryInterval L"0x03"
#define JobFlag_WriteRegistryPrinterName L"0x04"
#define JobFlag_WriteRegistryBmpPath L"0x05"
#define JobFlag_NotifyPrintingFailedRecordDisplayed L"0x06"
#define JobFlag_QueryServerStatus L"0x99"

//job parameter
#define JobParameter_NotifyScheduledPrintingFailedRecordDisplayed L"SD"
#define JobParameter_NotifyManualPrintingFailedRecordDisplayed L"MD"


#define Reply_OperationSuccess L"1x01"
#define Reply_OperationFail L"1x02"
#define Reply_StatusPrintingNow L"1x03"
#define Reply_StatusNotPrinting L"1x04"
#define Reply_StatusWaitingCountdownBeforePrint L"1x05"
#define Reply_StatusScheduledPrintingFailedRecordUpdated L"1x06"
#define Reply_StatusManualPrintingFailedRecordUpdated L"1x07"
#define Reply_StatusPrintingSucceededRecently L"1x08"
#define Reply_IllegalMsgReceived L"1x99"

//Server execution result
#define intServerResult_SUCCESS 0
#define intServerResult_Fail -1
#define intServerResult_IllegalMsgReceived -2
#define intServerResult_StatusPrintingNow 1
#define intServerResult_StatusNotPrinting 2
#define intServerResult_StatusWaitingCountdownBeforePrint 3
#define intServerResult_StatusScheduledPrintingFailedRecordUpdated 4
#define intServerResult_StatusManualPrintingFailedRecordUpdated 5
#define intServerResult_StatusPrintingSucceededRecently 6
#define intServerResult_ReceivedInvalidRequestID -999


//first 8 wchar_t is RequestID
//9~12 wchar_t indicates job (Ex. 0x01, 0x02)
//13~end is the passed value

int intProcessClientRequestByMsg(const std::wstring &strClientRequestMsg, std::wstring& OutputRequestID) {

	if (strClientRequestMsg.length() < 12) {
		//Invalid format
		return intServerResult_IllegalMsgReceived;
	}

	std::wstring wsRequestID, wsCommand;

	try {
		wsRequestID = strClientRequestMsg.substr(0, 8);
	}
	catch (std::out_of_range) {
		return intServerResult_ReceivedInvalidRequestID;
	}

	try {
		wsCommand = strClientRequestMsg.substr(8, 4);
	}
	catch (std::out_of_range) {
		return intServerResult_IllegalMsgReceived;
	}

	//output RequestID
	OutputRequestID = wsRequestID;

	//read command
	if (wsCommand == JobFlag_QueryServerStatus) {
		//first check if Scheduled printing failure record has updated, if yes, report updated, if not, second check Manual printing failure record, third check printing succeeded notify flag, if also not, report current status
		
		if (bGetScheduledPrintingFailureRecordNotifyFlag()) {
			//check scheduled printing failure notify flag

			//report updated and clear flag
			vUpdateScheduledPrintingFailureRecordNotifyFlag(false);
			return intServerResult_StatusScheduledPrintingFailedRecordUpdated;
		}
		else if (bGetManualPrintingFailureRecordNotifyFlag()) {
			//check manual printing failure notify flag

			//report updated and clear flag
			vUpdateManualPrintingFailureRecordNotifyFlag(false);
			return intServerResult_StatusManualPrintingFailedRecordUpdated;
		}
		else if (bGetPrintingSucceededRecentlyNotifyFlag()) {
			//check printing succeeded recently notify flag

			//report succeeded and clear flag
			vUpdatePrintingSucceededRecentlyNotifyFlag(false);
			return intServerResult_StatusPrintingSucceededRecently;
		}
		else {
			//reply current printing status
			int intPrintingStatus = intGetPrintingStatus();
			switch(intPrintingStatus) {
				case intPrinterStatus_NotPrinting:
					return intServerResult_StatusNotPrinting;
					break;
				case intPrinterStatus_Printing:
					return intServerResult_StatusPrintingNow;
					break;
				case intPrinterStatus_WaitingCountdownBeforePrint:
					return intServerResult_StatusWaitingCountdownBeforePrint;
					break;
				default:
					return intServerResult_Fail;
			}
		
		}

	}else if (wsCommand == JobFlag_PrintNow) {

		//print
		int intPrintResult = intStartPrintingJob(false,false);
		if (intPrintResult == 0) {
			return intServerResult_SUCCESS;
		}
		else {
			return intServerResult_Fail;
		}

	}

	std::wstring wsReceivedValue, wsRegistryValueName;

	try {
		wsReceivedValue = strClientRequestMsg.substr(12);
	}
	catch (std::out_of_range) {
		return intServerResult_IllegalMsgReceived;
	}

	if (wsCommand == JobFlag_NotifyPrintingFailedRecordDisplayed) {
		//client told us it has displayed the printing failure notification,Now check if it is Scheduled or Manual displayed

		boolean bResult;

		if (wsReceivedValue == JobParameter_NotifyScheduledPrintingFailedRecordDisplayed) {
			bResult = bChangeRegistryPrintingFailureRecordNotifyValue(NULL, true, true);		
		}
		else if (wsReceivedValue == JobParameter_NotifyManualPrintingFailedRecordDisplayed) {
			bResult = bChangeRegistryPrintingFailureRecordNotifyValue(NULL, true, false);
		}
		else
		{
			return intServerResult_IllegalMsgReceived;
		}

		if (bResult) {
			return intServerResult_SUCCESS;
		}
		else {
			return intServerResult_Fail;
		}
	}

	bool bValueIsInt = false;

	std::wstring wsLogParameter;
	int intLogType = 0;
	

	if (wsCommand == JobFlag_WriteRegistryEnabled) {

		wsRegistryValueName = DEF_Registry_Enabled;
		bValueIsInt = true;
		
	}
	else if (wsCommand == JobFlag_WriteRegistryInterval) {

		wsRegistryValueName = DEF_Registry_IntervalDay;
		bValueIsInt = true;
		intLogType = DEF_intLogType_Settings_Change_Interval_To;
		wsLogParameter = wsReceivedValue;
	}
	else if (wsCommand == JobFlag_WriteRegistryPrinterName) {

		wsRegistryValueName = DEF_Registry_PrinterName;
		intLogType = DEF_intLogType_Settings_Change_Printer_To;
		wsLogParameter = wsReceivedValue;
	}
	else if (wsCommand == JobFlag_WriteRegistryBmpPath) {

		wsRegistryValueName = DEF_Registry_BmpFilePath;
		intLogType = DEF_intLogType_Settings_Change_Image_To;
		wsLogParameter = wsReceivedValue;
	}
	else {

		//JobFlag unknown
		return intServerResult_IllegalMsgReceived;
	}

	HKEY hKey;
	hKey = hKeyOpenRegistyHandle();

	if (hKey == NULL) {
		return intServerResult_Fail;
	}

	bool bRegistryWriteResult = false;



	if (bValueIsInt) {

		bool bResult = bIsWstringContentUnsignedInteger(wsReceivedValue);
		if (bResult) {
			//content of wsReceivedValue is pure integer
			int intInputData;
			//try convert wstring to int
			try {
				intInputData = std::stoi(wsReceivedValue);
			}
			catch (std::invalid_argument) {
				return intServerResult_IllegalMsgReceived;
			}

			//before writing, check if wsReceivedValue has correct value
			if (wsRegistryValueName == DEF_Registry_Enabled) {
				//check if value is 0 or 1
				
				if (intInputData == 1) {
					intLogType = DEF_intLogType_Settings_Change_Enabled_ON;
				}
				else if (intInputData == 0) {
					intLogType = DEF_intLogType_Settings_Change_Enabled_OFF;
				}
				else {
					return intServerResult_IllegalMsgReceived;
				}
			}
			else if (wsRegistryValueName == DEF_Registry_IntervalDay) {
				//Make sure interval value > 0
				if (intInputData < 1) {
					return intServerResult_IllegalMsgReceived;
				}
			}

			bRegistryWriteResult = bRegistryWriteInteger(hKey, wsRegistryValueName.c_str(), intInputData);

		}
		else {
			//invalid value data, abort
			bCloseRegistryHandle(hKey);
			return intServerResult_IllegalMsgReceived;
		}

	}
	else {

		bRegistryWriteResult = bRegistryWriteSingleLineString(hKey, wsRegistryValueName.c_str(), wsReceivedValue);

	}

	Logging::intWriteLogToLogFile_ThreadSafe(intLogType, wsLogParameter);

	bCloseRegistryHandle(hKey);

	if (bRegistryWriteResult) {
		return intServerResult_SUCCESS;
	}
	else {
		return intServerResult_Fail;
	}
}

int intGenerateResponseMsg(const std::wstring &wsRequestID, int intResultCode, std::wstring& wsOutput) {
	//this function returns -1 if failed, return 0 if sucess

	std::wstring wsResponse;
	std::wstring wsResultPart;

	switch (intResultCode) {
	case intServerResult_SUCCESS:
		wsResultPart = Reply_OperationSuccess;
		break;
	case intServerResult_Fail:
		wsResultPart = Reply_OperationFail;
		break;
	case intServerResult_IllegalMsgReceived:
		wsResultPart = Reply_IllegalMsgReceived;
		break;
	case intServerResult_ReceivedInvalidRequestID:
		wsResultPart = Reply_IllegalMsgReceived;
		break;
	case intServerResult_StatusPrintingNow:
		wsResultPart = Reply_StatusPrintingNow;
		break;
	case intServerResult_StatusNotPrinting:
		wsResultPart = Reply_StatusNotPrinting;
		break;
	case intServerResult_StatusWaitingCountdownBeforePrint:
		wsResultPart = Reply_StatusWaitingCountdownBeforePrint;
		break;
	case intServerResult_StatusScheduledPrintingFailedRecordUpdated:
		wsResultPart = Reply_StatusScheduledPrintingFailedRecordUpdated;
		break;
	case intServerResult_StatusManualPrintingFailedRecordUpdated:
		wsResultPart = Reply_StatusManualPrintingFailedRecordUpdated;
		break;
	case intServerResult_StatusPrintingSucceededRecently:
		wsResultPart = Reply_StatusPrintingSucceededRecently;
		break;
	default:
		return -1;

	}

	if (intResultCode != intServerResult_ReceivedInvalidRequestID) {
		wsResponse = wsRequestID + wsResultPart;
		wsOutput = wsResponse;
	}
	else {
		wsResponse = L"FFFFFFFF" + wsResultPart;
		wsOutput = wsResponse;
	}

	return 0;
}