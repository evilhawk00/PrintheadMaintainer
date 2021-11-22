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
using Microsoft.Win32;
using PrintheadMaintainerUI.GlobalConstants;
using PrintheadMaintainerUI.Mediators;
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Utils;
using System.Threading;

namespace PrintheadMaintainerUI.Threads
{
    public class RealtimeStatusUpdater
    {
        private static bool bIfPrintInNextMinuteNotificationDisplayed = false;
        public static void VoidSyncLoopRealtimeStatusUpdater() {

            //at fitst start, check for preivious failure
            VoidCheckPrintFailureRecordAndNotify(true);
            VoidCheckPrintFailureRecordAndNotify(false);

            while (true) {

                //encode msg
                string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
                string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_QueryServerStatus, null);
                int intServerResult = IPCClient.IntSyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg);
                VoidQueryServerForStatus_Callback(intServerResult);

                Thread.Sleep(10000); //update interval 10sec
            }
            
            
        }

        public static void VoidSyncNotifyServerPrintingFailedRecordDisplayed(bool bIsScheduledRecordOrNOT) {

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg;
            if (bIsScheduledRecordOrNOT)
            {
                //Scheduled Printing Record
                strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_NotifyPrintingFailedRecordDisplayed, NamedPipeConstants.JobParameter_NotifyScheduledPrintingFailedRecordDisplayed);
            }
            else {
                //Manual Printing Record
                strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_NotifyPrintingFailedRecordDisplayed, NamedPipeConstants.JobParameter_NotifyManualPrintingFailedRecordDisplayed);
            }
            _ = IPCClient.IntSyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg);
        }

        public static void VoidQueryServerForStatus() {

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_QueryServerStatus, null);
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidQueryServerForStatus_Callback);



        }

        public static void VoidQueryServerForStatus_Callback(int intResult) {

            switch (intResult)
            {
                case NamedPipeConstants.intServerResult_StatusNotPrinting:
                    bIfPrintInNextMinuteNotificationDisplayed = false;
                    break;
                case NamedPipeConstants.intServerResult_StatusScheduledPrintingFailedRecordUpdated:
                    Mediator.Notify("RefreshLastPrint", "");
                    VoidCheckPrintFailureRecordAndNotify(true);
                    break;
                case NamedPipeConstants.intServerResult_StatusManualPrintingFailedRecordUpdated:
                    Mediator.Notify("RefreshLastPrint", "");
                    VoidCheckPrintFailureRecordAndNotify(false);
                    break;
                case NamedPipeConstants.intServerResult_StatusPrintingSucceededRecently:
                    Mediator.Notify("RefreshLastPrint", "");
                    Mediator.Notify("UpdateFooter", "");
                    break;
                case NamedPipeConstants.intServerResult_StatusWaitingCountdownBeforePrint:
                    if (!bIfPrintInNextMinuteNotificationDisplayed)
                    {
                        UWPNotificationUtils.VoidShowUWPNotification("Preparing to print...", "Printhead Maintainer will print a page in the next few minutes.");
                        bIfPrintInNextMinuteNotificationDisplayed = true;
                    }
                    break;
                case NamedPipeConstants.intServerResult_StatusPrintingNow:
                    break;
                default:
                    break;
            }
        }



        private static string StrCheckAndGenerateLastFailureTimeStringFromRegistry(bool bIsScheduledRecordOrNOT)
        {

            //get last print
            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                return null;
            }
            //We coded in this format for better execution performance, though it is messy.


            //check if displayed flag is false;
            int intResultDisplayed;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultDisplayed = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Displayed);
            }
            else {
                //check manual printing failure record
                intResultDisplayed = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Displayed);
            }
            if (intResultDisplayed != 0) {
                //the record has shown the user
                return null;
            }

            int intResultYear;
            if (bIsScheduledRecordOrNOT) {
                //check scheduled printing failure record
                intResultYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Year);
            }
            else {
                //check manual printing failure record
                intResultYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Year);
            }     
            if (intResultYear < 0)
            {
                return null;
            }

            int intResultMonth;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Month);
            }
            else
            {
                //check manual printing failure record
                intResultMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Month);
            }
            if (intResultMonth < 0)
            {
                return null;
            }

            int intResultDay;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Day);
            }
            else
            {
                //check manual printing failure record
                intResultDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Day);
            }
            if (intResultDay < 0)
            {
                return null;
            }

            int intResultDayOfWeek;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultDayOfWeek = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_DayOfWeek);
            }
            else
            {
                //check manual printing failure record
                intResultDayOfWeek = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_DayOfWeek);
            }
            if (intResultDayOfWeek < 0)
            {
                return null;
            }

            int intResultHour;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Hour);
            }
            else
            {
                //check manual printing failure record
                intResultHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Hour);
            }
            if (intResultHour < 0)
            {
                return null;
            }

            int intResultMinute;
            if (bIsScheduledRecordOrNOT)
            {
                //check scheduled printing failure record
                intResultMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Minute);
            }
            else
            {
                //check manual printing failure record
                intResultMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Manual_PrintFailRecord_Minute);
            }
            if (intResultMinute < 0)
            {
                return null;
            }

            string strFixedMonth = intResultMonth.ToString();
            if (intResultMonth < 10)
            {
                strFixedMonth = "0" + strFixedMonth;
            }
            string strFixedDay = intResultDay.ToString();
            if (intResultDay < 10)
            {
                strFixedDay = "0" + strFixedDay;
            }
            string strFixedMinute = intResultMinute.ToString();
            if (intResultMinute < 10)
            {
                strFixedMinute = "0" + strFixedMinute;
            }

            string strDayOfWeek;
            switch (intResultDayOfWeek)
            {
                case 0:
                    strDayOfWeek = "Sun";
                    break;
                case 1:
                    strDayOfWeek = "Mon";
                    break;
                case 2:
                    strDayOfWeek = "Tue";
                    break;
                case 3:
                    strDayOfWeek = "Wed";
                    break;
                case 4:
                    strDayOfWeek = "Thu";
                    break;
                case 5:
                    strDayOfWeek = "Fri";
                    break;
                case 6:
                    strDayOfWeek = "Sat";
                    break;
                default:
                    strDayOfWeek = "";
                    break;

            }


            string strGeneratedOutput = intResultYear.ToString() + "/" + strFixedMonth + "/" + strFixedDay + " " + intResultHour.ToString() + ":" + strFixedMinute + " " + strDayOfWeek;
            return strGeneratedOutput;
        }


        private static void VoidCheckPrintFailureRecordAndNotify(bool bIsScheduledRecordOrNOT) {

            string strFailureTime = StrCheckAndGenerateLastFailureTimeStringFromRegistry(bIsScheduledRecordOrNOT);
            if (strFailureTime != null)
            {
                if (bIsScheduledRecordOrNOT)
                {
                    //Scheduled Printing Failed
                    UWPNotificationUtils.VoidShowUWPNotification("Scheduled printing failure", "Printing failed at " + strFailureTime + ". Please check your connection to the printer.");
                }
                else {
                    //Manual Printing Failed
                    UWPNotificationUtils.VoidShowUWPNotification("Manual printing failure", "Printing failed at " + strFailureTime + ". Please check your connection to the printer.");
                }
                
                VoidSyncNotifyServerPrintingFailedRecordDisplayed(bIsScheduledRecordOrNOT);

            }

        }

    }
}
