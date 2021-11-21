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

#include "../SoftwareSettings/SoftwareSettings.h"
#include "../PrintingStatusReporter/PrintingStatusReporter.h"
#include "../Logging/Logging.h"
#include "../Utils/DateTimeUtils.h"
#include "../Utils/PrinterUtils.h"

int intStartPrintingJob(bool bCheckEnabledAndInterval, bool bWaitCountdownBeforPrint) {
//if bCheckEnabledAndInterval is false, it will not check "LastPrintTime" and "Enabled", when PrinterName is valid, it prints immediately
//return -1 if operation failed
//return -2 if interval check not passed, time too close

    //first read configs

    bool bResult = bLoadSoftwareSettingFromRegistry(bCheckEnabledAndInterval);

    if (!bResult) {
        //Setting corrupted
        return -1;
    }

    //flag to determine if we should do printing
    bool bPrintOrNot = false;
        
    //Here start to deterine if we should print
    if (bCheckEnabledAndInterval){
        //interval  and enabled is necessary
        if (gs_intEnabled == 1) {
            //it is enabled, now check interval
            if (gs_bLastPrintTimeValid) {

                //LastPrintTimeFound, now check time between interval

                //Get current time
                SYSTEMTIME stCurrent;
                GetLocalTime(&stCurrent);

                //build LastPrintTime to SYSTEMTIME
                SYSTEMTIME stLastPrintTime;
                bool bResult = bBuildSeperateIntToSystemtime(stLastPrintTime, gs_intLastPrintYear, gs_intLastPrintMonth, gs_intLastPrintDay, gs_intLastPrintHour, gs_intLastPrintMinute, gs_intLastPrintSecond, gs_intLastPrintMilliseconds, gs_intLastPrintDayOfWeek);
                if (bResult) {

                    bool bIntervalCheckResult = bIfDaysBetweenTwoDateIsMoreThan(stLastPrintTime, stCurrent, gs_intIntervalDay);
                    if (bIntervalCheckResult) {
                        //interval check passed, execute printing

                        bPrintOrNot = true;

                    }
                    else {
                        //time too close, too close, not executed
                        return -2;
                    }

                }
                else {
                    return -1;
                }

            }
            else {
                //bCheckEnabledAndInterval is true and it is enabled, but LastPrintTimeFound not found, dont check interval just print directly
                bPrintOrNot = true;
            }

        }


    }
    else {
            
        //or
        //bCheckEnabledAndInterval is false, just print directly
        bPrintOrNot = true;
        
    }

        
    //check flag if need to print
    if (!bPrintOrNot) {
        //Interval and Enabled result shows we don't need to print, return
        return 0;
    }


    //To make sure there's no printing conflict from different threads, first check printing status
    int intPrintingStatus = intGetPrintingStatus();
    while (intPrintingStatus != intPrinterStatus_NotPrinting) {
        //keep waiting until status is NotPrinting
        Sleep(5000);
        intPrintingStatus = intGetPrintingStatus();
    }

    if (bWaitCountdownBeforPrint) {
        //waiting countdown before print
        vUpdatePrintingStatus(intPrinterStatus_WaitingCountdownBeforePrint);
        Sleep(60000);
    }
            
            
    vUpdatePrintingStatus(intPrinterStatus_Printing);

    std::wstring wsRandomDocumentName;
    bool bPrintingHasError = false;

    //Before send print signal, detect if printer is offline or has error
    int intCheckResult = intIfPrinterHasError(gs_wsPrinterName);
    if (intCheckResult != 0) {
        //printer has error, dont send, abort

        bPrintingHasError = true;
    }

    if (!bPrintingHasError) {
    
        wsRandomDocumentName = wsGenerateRandomDocumentName();
        //send signal to the printer
        int intSendResult = intSendPrintBmpJobToPrinter(wsRandomDocumentName, gs_wsBmpPath, gs_wsPrinterName);
        if (intSendResult != 0) {
            //signal sent failure

            bPrintingHasError = true;

        }

    }
    
    if (!bPrintingHasError) {
    
        //after sent, wait 60 seconds and check printer, check if has error and document still exist in queue, if no error but doc still in queue ==> keep waiting(max 2min)
        Sleep(60000);
        
        //check error again
        int intReCheckErrorResult = intIfPrinterHasError(gs_wsPrinterName);
        if (intReCheckErrorResult != 0) {
            //printer has error
            bPrintingHasError = true;

            //we check if job is still in queue , if yes we delete it
            intIfJobStillInQueueWithOptionalDelete(wsRandomDocumentName, gs_wsPrinterName, true);
            
        }
        else {
            //no error, we check if job is still in queue, if yes we keep waiting
            int intCheckJobResult = intIfJobStillInQueueWithOptionalDelete(wsRandomDocumentName, gs_wsPrinterName, false);
            if (intCheckJobResult != 0) {
                //no error, but Job is still in queue, or exception happened, so we wait more time and check error, if exceed limit we delete it and report error

                bool bJobDisappeared = false;

                //start wait loop
                for (int intWaitCounter = 0; intWaitCounter < 12; intWaitCounter++) {

                    Sleep(10000); // every 10 sec check job and error again

                    int intJobRecheckResult = intIfJobStillInQueueWithOptionalDelete(wsRandomDocumentName, gs_wsPrinterName, false);
                    if (intJobRecheckResult == 0) {
                        //Job is not in queue anymore, no more waiting, we wait another 30 sec before next error check
                        bJobDisappeared = true;
                        Sleep(30000);
                        break;
                    }

                    int intErrorRecheckResult = intIfPrinterHasError(gs_wsPrinterName);
                    if (intErrorRecheckResult != 0) {
                        //printer has error, dont wait anymore, abort to delete
                        bPrintingHasError = true;
                        break;

                    }

                }
                //Max wait time 2 minute exceeded

                //if job still exist, we need to delete printing job from queue
                if (!bJobDisappeared) {
                    //last check, if job still exist, delete job
                    int intJobFinalResult = intIfJobStillInQueueWithOptionalDelete(wsRandomDocumentName, gs_wsPrinterName, true);
                    if (intJobFinalResult != 0) {
                        //job still inside queue, so we've deleted it, printing error
                        bPrintingHasError = true;
                    }

                }
                else {
                    //job disappeared, so we last check error
                    int intFinalCheckErrorResult = intIfPrinterHasError(gs_wsPrinterName);
                    if (intFinalCheckErrorResult != 0) {
                        //printer has error
                        bPrintingHasError = true;

                    }
                }
            }

        }

    }

    //final part, two decisions based on the result of printing
    if (!bPrintingHasError) {
        //printing success

        bWriteLastPrintTimeToRegistry();
        vUpdatePrintingSucceededRecentlyNotifyFlag(true);
        vUpdatePrintingStatus(intPrinterStatus_NotPrinting);

        //write log
        std::wstring wsDummy;
        if (bCheckEnabledAndInterval) {
            Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_Scheduled_Printing_OK, wsDummy);
        }
        else {
            Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_Manual_Printing_OK, wsDummy);
        }

        return 0;
    
    }
    else {
        //printing error
    
        if (bCheckEnabledAndInterval) {
            //this is scheduled printing
            bWritePrintinigFailureRecordToRegistry(true);
            vUpdateScheduledPrintingFailureRecordNotifyFlag(true);
        }
        else {
            //this is manual printing
            bWritePrintinigFailureRecordToRegistry(false);
            vUpdateManualPrintingFailureRecordNotifyFlag(true);
        }


        vUpdatePrintingStatus(intPrinterStatus_NotPrinting);

        //write log
        std::wstring wsDummy;
        if (bCheckEnabledAndInterval) {
            Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_Scheduled_Printing_FAIL, wsDummy);
        }
        else {
            Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_Manual_Printing_FAIL, wsDummy);
        }

        return -1;
    
    }

}