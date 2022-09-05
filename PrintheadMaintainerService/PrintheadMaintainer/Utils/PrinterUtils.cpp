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

#include "RandomUtils.h"

std::wstring wsGenerateRandomDocumentName() {

    std::wstring wsBasicHeader = L"Printhead Maintainer Job_";
    std::wstring wsRandomString = wsGenerateRamdomHexString(8);

    return wsBasicHeader + wsRandomString;
}

int intSendPrintBmpJobToPrinter(const std::wstring &wsDocumentNameToDisplayInQueue, const std::wstring &wsBmpFilePath, const std::wstring &wsPrinterName) {

	DOCINFO	docinfo;

	docinfo.cbSize = sizeof(DOCINFO);
    docinfo.lpszDocName = wsDocumentNameToDisplayInQueue.c_str();
	docinfo.lpszOutput = NULL;
	docinfo.lpszDatatype = NULL;
	docinfo.fwType = 0;

    //convert wstring to LPWSTR
    wchar_t wBuffer[256] = { 0 };
    wmemcpy_s(wBuffer, 256, wsPrinterName.c_str(), wsPrinterName.size());
    LPWSTR lpPrinterName = wBuffer;

    //getting the printer DC
    HDC PrinterDC = CreateDC(L"WINSPOOL", lpPrinterName, NULL, NULL);

    //start doc and page
    //the print thing starts here
    StartDoc(PrinterDC, &docinfo);
    StartPage(PrinterDC);
    //here we can draw what we want using GDI functions
    //but never forget convert from pixels to twips

    //draw the windc to printerdc

    HBITMAP hThisBmp = NULL;
    HBITMAP hOldBmp = NULL;
    

    hThisBmp = (HBITMAP)LoadImage(NULL, wsBmpFilePath.c_str(), IMAGE_BITMAP, 0, 0, LR_LOADFROMFILE);

    if (hThisBmp == NULL) {
        return -1;
    }

    
    HDC hCopyDC = CreateCompatibleDC(NULL);
    if (hCopyDC == NULL) {
        return -1;
    }


    int intPrinterMaxWidth = 0;
    int intPrinterMaxHeight = 0;

    int intBitmapWidth = 0;
    int intBitmapHeight = 0;

    //Get printer phyical Printing limit
    intPrinterMaxWidth = GetDeviceCaps(PrinterDC, HORZRES);
    intPrinterMaxHeight = GetDeviceCaps(PrinterDC, VERTRES);

    //Get Bitmap size
    BITMAP bm;
    int intResult = GetObject(hThisBmp, sizeof(bm), &bm);
    if (intResult == 0) {
        //getobject failed
        return -1;
    }


    intBitmapWidth = bm.bmWidth;
    intBitmapHeight = bm.bmHeight;


    hOldBmp = (HBITMAP)SelectObject(hCopyDC, hThisBmp);
    StretchBlt(PrinterDC, 0, 0, intPrinterMaxWidth, intPrinterMaxHeight, hCopyDC, 0, 0, intBitmapWidth, intBitmapHeight, SRCCOPY);
    SelectObject(hCopyDC, hOldBmp);
    DeleteDC(hCopyDC);
    
    //end page and doc
    //the print thing ends here
    EndPage(PrinterDC);
    EndDoc(PrinterDC);

    //delete GDI resources
    DeleteDC(PrinterDC);

    DeleteObject(hThisBmp);

    return 0;

}

int intIfPrinterHasError(const std::wstring& wsPrinterName) {
    //return 0 if no error
    //return 1 if has error
    //return -1 if exception found

    HANDLE hPrinter;
    bool bOpenPrinterResult = OpenPrinter(const_cast<LPWSTR>(wsPrinterName.c_str()), &hPrinter, NULL);
    if (!bOpenPrinterResult)
    {
        //open printer failed
        return -1;
    }

    DWORD dwByteNeeded_PI = 0;
    DWORD dwByteUsed_PI = 0;

    PRINTER_INFO_2* pPrinterInfo = NULL;

    int intResultGP1 = GetPrinter(hPrinter, 2, NULL, 0, &dwByteNeeded_PI);
    if (intResultGP1 == 0) {

        if (GetLastError() != ERROR_INSUFFICIENT_BUFFER) {
            ClosePrinter(hPrinter);
            return -1;
        }

    }

    pPrinterInfo = (PRINTER_INFO_2*)malloc(dwByteNeeded_PI);
    if (pPrinterInfo == NULL) {
        //Failed to allocate memory
        ClosePrinter(hPrinter);
        return -1;
    }

    memset(pPrinterInfo, 0, dwByteNeeded_PI);

    int intResultGP2 = GetPrinter(hPrinter, 2, (LPBYTE)pPrinterInfo, dwByteNeeded_PI, &dwByteUsed_PI);
    if (intResultGP2 == 0) {
        //access printer failed
        free(pPrinterInfo);
        ClosePrinter(hPrinter);
        return -1;
    }

    if (pPrinterInfo->Status && (PRINTER_STATUS_ERROR |
        PRINTER_STATUS_PAPER_JAM |
        PRINTER_STATUS_PAPER_OUT |
        PRINTER_STATUS_PAPER_PROBLEM |
        PRINTER_STATUS_OUTPUT_BIN_FULL |
        PRINTER_STATUS_NOT_AVAILABLE |
        PRINTER_STATUS_NO_TONER |
        PRINTER_STATUS_OUT_OF_MEMORY |
        PRINTER_STATUS_OFFLINE |
        PRINTER_STATUS_DOOR_OPEN)) {

        //found error of printer
        free(pPrinterInfo);
        ClosePrinter(hPrinter);
        return 1;
    }

    //no errors were found in current printer status, now check printing jobs
    DWORD dwJobsInQueue = pPrinterInfo->cJobs;

    //Buffer printer info is no longer needed, free it
    free(pPrinterInfo);

    if (dwJobsInQueue <= 0) {
        //no jobs in queue, no printer status error , also no need to check job error, so ====> no error
        ClosePrinter(hPrinter);
        return 0;
    }

    //we have jobs in queue
    //get printer jobs
    DWORD dwByteNeededJI = 0;
    DWORD dwByteUsedJI = 0;
    DWORD dwJobsReturned = 0;
    JOB_INFO_2* pJobs = NULL;

    // Get job storage space. 
    bool bResultEJ1 = EnumJobs(hPrinter, 0, dwJobsInQueue, 2, NULL, 0, (LPDWORD)&dwByteNeededJI, (LPDWORD)&dwJobsReturned);
    if (!bResultEJ1)
    {
        if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        {
            ClosePrinter(hPrinter);
            return -1;
        }
    }

    pJobs = (JOB_INFO_2*)malloc(dwByteNeededJI);
    if (pJobs == NULL)
    {
        // Failure to allocate Job storage space.
        ClosePrinter(hPrinter);
        return -1;
    }

    memset(pJobs, 0, dwByteNeededJI);

    //Get the list of printing jobs
    bool bRessultEJ2 = EnumJobs(hPrinter, 0, dwJobsInQueue, 2, (LPBYTE)pJobs, dwByteNeededJI, (LPDWORD)&dwByteUsedJI, (LPDWORD)&dwJobsReturned);
    if (!bRessultEJ2)
    {
        free(pJobs);
        ClosePrinter(hPrinter);
        return -1;
    }

    //printer handle is no longer needed
    ClosePrinter(hPrinter);

    for (unsigned int i = 0; i < dwJobsReturned; i++)
    {
        if (pJobs[i].Status & JOB_STATUS_PRINTING)
        {
            if (pJobs[i].Status &
                (JOB_STATUS_ERROR |
                    JOB_STATUS_OFFLINE |
                    JOB_STATUS_PAPEROUT |
                    JOB_STATUS_BLOCKED_DEVQ))
            {
                //found error of current printing job
                free(pJobs);
                return 1;
            }
        }
    }

    //no error
    free(pJobs);
    return 0;

}

int intIfJobStillInQueueWithOptionalDelete(const std::wstring& wsDocumentNameDisplayedInQueue, const std::wstring& wsPrinterName, bool bDeleteIfExist) {
    //return 0 if not in queue
    //return 1 if still in queue
    //return -1 if exception found
    //if bDeleteIfExist is set to true, it will delete the specific job if found

    HANDLE hPrinter;
    bool bOpenPrinterResult = OpenPrinter(const_cast<LPWSTR>(wsPrinterName.c_str()), &hPrinter, NULL);
    if (!bOpenPrinterResult)
    {
        //open printer failed
        return -1;
    }

    DWORD dwByteNeeded_PI = 0;
    DWORD dwByteUsed_PI = 0;

    PRINTER_INFO_2* pPrinterInfo = NULL;

    int intResultGP1 = GetPrinter(hPrinter, 2, NULL, 0, &dwByteNeeded_PI);
    if (intResultGP1 == 0) {

        if (GetLastError() != ERROR_INSUFFICIENT_BUFFER) {
            ClosePrinter(hPrinter);
            return -1;
        }

    }

    pPrinterInfo = (PRINTER_INFO_2*)malloc(dwByteNeeded_PI);
    if (pPrinterInfo == NULL) {
        //Failed to allocate memory
        ClosePrinter(hPrinter);
        return -1;
    }

    memset(pPrinterInfo, 0, dwByteNeeded_PI);

    int intResultGP2 = GetPrinter(hPrinter, 2, (LPBYTE)pPrinterInfo, dwByteNeeded_PI, &dwByteUsed_PI);
    if (intResultGP2 == 0) {
        //access printer failed
        free(pPrinterInfo);
        ClosePrinter(hPrinter);
        return -1;
    }

    DWORD dwJobsInQueue = pPrinterInfo->cJobs;

    //Buffer printer info is no longer needed, free it
    free(pPrinterInfo);

    if (dwJobsInQueue <= 0) {
        //no jobs in queue
        ClosePrinter(hPrinter);
        return 0;
    }

    //we have jobs in queue

    //get printer jobs
    DWORD dwByteNeededJI = 0;
    DWORD dwByteUsedJI = 0;
    DWORD dwJobsReturned = 0;
    JOB_INFO_2* pJobs = NULL;

    // Get job storage space. 
    bool bResultEJ1 = EnumJobs(hPrinter, 0, dwJobsInQueue, 2, NULL, 0, (LPDWORD)&dwByteNeededJI, (LPDWORD)&dwJobsReturned);
    if (!bResultEJ1)
    {
        if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        {

            ClosePrinter(hPrinter);
            return -1;
        }
    }

    pJobs = (JOB_INFO_2*)malloc(dwByteNeededJI);
    if (pJobs == NULL)
    {
        // Failure to allocate Job storage space.

        ClosePrinter(hPrinter);
        return -1;
    }

    memset(pJobs, 0, dwByteNeededJI);

    //Get the list of printing jobs
    bool bRessultEJ2 = EnumJobs(hPrinter, 0, dwJobsInQueue, 2, (LPBYTE)pJobs, dwByteNeededJI, (LPDWORD)&dwByteUsedJI, (LPDWORD)&dwJobsReturned);
    if (!bRessultEJ2)
    {

        free(pJobs);
        ClosePrinter(hPrinter);
        return -1;
    }

    for (unsigned int i = 0; i < dwJobsInQueue; i++)
    {
        if (pJobs[i].pDocument == wsDocumentNameDisplayedInQueue)
        {
            //found existence of the printing job

            //check if need to delete
            if (bDeleteIfExist) {
                //delete job, remember here still requires printer handle, dont close handle before here
                //here we don't get delete result, because it is not needed in our case
                SetJob(hPrinter, pJobs[i].JobId, 0, NULL, JOB_CONTROL_DELETE);
            }
            free(pJobs);
            ClosePrinter(hPrinter);
            return 1;
        }
    }

    //not found
    free(pJobs);
    ClosePrinter(hPrinter);
    return 0;
}

