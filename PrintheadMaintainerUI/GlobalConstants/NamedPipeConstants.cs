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
namespace PrintheadMaintainerUI.GlobalConstants
{
    public static class NamedPipeConstants
    {
        //Connection Options
        public const int intConnectionTimeout = 5000;
        public const string NamedPipeName = "Printhead Maintainer";


        //job flag
        public const string JobFlag_PrintNow = "0x01";
        public const string JobFlag_WriteRegistryEnabled = "0x02";
        public const string JobFlag_WriteRegistryInterval = "0x03";
        public const string JobFlag_WriteRegistryPrinterName = "0x04";
        public const string JobFlag_WriteRegistryBmpPath = "0x05";
        public const string JobFlag_NotifyPrintingFailedRecordDisplayed = "0x06";
        public const string JobFlag_QueryServerStatus = "0x99";

        //job parameter
        public const string JobParameter_NotifyScheduledPrintingFailedRecordDisplayed = "SD";
        public const string JobParameter_NotifyManualPrintingFailedRecordDisplayed = "MD";

        //response from server
        public const string Reply_OperationSuccess = "1x01";
        public const string Reply_OperationFail = "1x02";
        public const string Reply_StatusPrintingNow = "1x03";
        public const string Reply_StatusNotPrinting = "1x04";
        public const string Reply_StatusWaitingCountdownBeforePrint = "1x05";
        public const string Reply_StatusScheduledPrintingFailedRecordUpdated = "1x06";
        public const string Reply_StatusManualPrintingFailedRecordUpdated = "1x07";
        public const string Reply_StatusPrintingSucceededRecently = "1x08";
        public const string Reply_IllegalMsgReceived = "1x99";

        //Server execution result
        public const int intServerResult_SUCCESS = 0;
        public const int intServerResult_Fail = -1;
        public const int intServerResult_IllegalMsgReceived = -2;
        public const int intServerResult_StatusPrintingNow = 1;
        public const int intServerResult_StatusNotPrinting = 2;
        public const int intServerResult_StatusWaitingCountdownBeforePrint = 3;
        public const int intServerResult_StatusScheduledPrintingFailedRecordUpdated = 4;
        public const int intServerResult_StatusManualPrintingFailedRecordUpdated = 5;
        public const int intServerResult_StatusPrintingSucceededRecently = 6;
        public const int intServerResult_ReceivedInvalidRequestID = -999;


    }
}
