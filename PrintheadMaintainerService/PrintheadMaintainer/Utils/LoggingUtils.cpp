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

#include "FileSizeUtils.h"
#include "WriteFileUtils.h"
#include "EncodingUtils.h"

int intUtf8Log_CreateNewUTF8LogFileAndWriteWstring(const std::wstring &wsLogFilePath, const std::wstring &wsWriteContent) {

    HANDLE  hFile = NULL;
    hFile = CreateFile(wsLogFilePath.c_str(), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_FLAG_SEQUENTIAL_SCAN, NULL);

    if (hFile == INVALID_HANDLE_VALUE) {

        return -1;
    }

    unsigned char smarker[3];
    DWORD bytesWritten;

    smarker[0] = 0xEF;
    smarker[1] = 0xBB;
    smarker[2] = 0xBF;
    //write Windows UTF8 BOM
    WriteFile(hFile, smarker, 3, &bytesWritten, NULL);
    //Convert wstring to UTF8 string
    std::string strBuf = wstring_to_utf8_string(wsWriteContent);
    //Write UTF8 Prepend string
    WriteFile(hFile, strBuf.c_str(), strBuf.size(), &bytesWritten, NULL);

    CloseHandle(hFile);

    return 0;
}

int intUtf8Log_WriteUtf8BomAndWstring(HANDLE hFile, const std::wstring &wsWriteContent) {

    if (hFile == INVALID_HANDLE_VALUE) {

        return -1;
    }

    unsigned char smarker[3];
    DWORD bytesWritten;

    smarker[0] = 0xEF;
    smarker[1] = 0xBB;
    smarker[2] = 0xBF;
    //write Windows UTF8 BOM
    WriteFile(hFile, smarker, 3, &bytesWritten, NULL);
    //Convert wstring to UTF8 string
    std::string strBuf = wstring_to_utf8_string(wsWriteContent);
    //Write UTF8 Prepend string
    WriteFile(hFile, strBuf.c_str(), strBuf.size(), &bytesWritten, NULL);

    return 0;
}

int intUtf8Log_PrependNewLineAndTruncateOldLastLine(HANDLE hFile, const std::wstring& wsPrependContent, bool bTruncateOldLastLineOrNot, long lFilePointerEndOfSecondLastLine) {

    if (hFile == INVALID_HANDLE_VALUE) {
        return -1;
    }


    DWORD dwFileSize = 0;
    dwFileSize = GetFileSize(hFile, NULL);
    if (dwFileSize == INVALID_FILE_SIZE)
    {
        return -1; //unable to get filesize
    }

    char* bufFstream;

    bufFstream = (char*)malloc(sizeof(char) * (dwFileSize)); //create buffer with Filesize

    if (bufFstream == NULL) {

        return -1;//ERROR_MEMORY;
    }
    memset(bufFstream, 0, sizeof(char) * (dwFileSize));

    DWORD dwBomHeaderByteCount = 3;
    DWORD dwBytesToRead;
    if (bTruncateOldLastLineOrNot) {

        dwBytesToRead = lFilePointerEndOfSecondLastLine - dwBomHeaderByteCount;
    }
    else {
        dwBytesToRead = dwFileSize - dwBomHeaderByteCount;
    }

    DWORD nRead = 0;

    //skip BOM header, read from after BOM
    SetFilePointer(hFile, dwBomHeaderByteCount, NULL, FILE_BEGIN);

    bool bReadResult = ReadFile(hFile, bufFstream, dwBytesToRead, &nRead, NULL);
    if (!bReadResult) {

        free(bufFstream);
        return -1;
    }

    //Now truncate file
    int intResultTFE = intTruncateFileToEmpty(hFile);
    if (intResultTFE != 0) {
        //failed
        free(bufFstream);
        return -1;
    }

    unsigned char smarker[3];
    DWORD bytesWritten;

    smarker[0] = 0xEF;
    smarker[1] = 0xBB;
    smarker[2] = 0xBF;
    //write Windows UTF8 BOM
    bool bWriteResult1 = WriteFile(hFile, smarker, 3, &bytesWritten, NULL);
    if (!bWriteResult1) {

        free(bufFstream);
        return -1;
    }
    //Convert wstring to UTF8 string
    std::string strBuf = wstring_to_utf8_string(wsPrependContent);
    //Write UTF8 Prepend string
    bool bWriteResult2 = WriteFile(hFile, strBuf.c_str(), strBuf.size(), &bytesWritten, NULL);
    if (!bWriteResult2) {

        free(bufFstream);
        return -1;
    }
    //write content read from input file
    bool bWriteResult3 = WriteFile(hFile, bufFstream, dwBytesToRead, &bytesWritten, NULL);
    if (!bWriteResult3) {

        free(bufFstream);
        return -1;
    }

    free(bufFstream);
    return 0;
}

int intCountLinesInTextFile(HANDLE hFile, long& lOutputFilePointerEndOfFirstLine, long& lOutputFilePointerEndOfSecondLastLine) {
    //returns the file pointer of "end of first line" with lOutputPointerEndOfFirstLine
    //returns the file pointer of "end of second last line" with lOutputPointerEndOfSecondLastLine
    //returns the total lines count the text file has via the main return

    if (hFile == INVALID_HANDLE_VALUE) {
        return -1;
    }

    DWORD dwFileSize = 0;
    dwFileSize = GetFileSize(hFile, NULL);
    if (dwFileSize == INVALID_FILE_SIZE)
    {
        return -1; //unable to get filesize
    }

    char bufFstream;

    DWORD dwBytesToRead = 1;
    DWORD dwBytesRemain = dwFileSize;
    DWORD nRead = 0;

    int intLinesCount = 0;

    DWORD dwLastNewLineCharLocation = 0;

    while (dwBytesRemain > 0) {

        bool bReadResult = ReadFile(hFile, &bufFstream, dwBytesToRead, &nRead, NULL);

        if (!bReadResult) {


            return -1;
        }

        dwBytesRemain = dwBytesRemain - dwBytesToRead;


        //check if this is the end of file, no matter if the last char is \n or not, always need to +1 count 
        if (dwBytesRemain == 0) {
            //end of file reached
            intLinesCount++;
        }
        else {
            //not the end of file, check for \n
            if (bufFstream == '\n') {
                //a new line character found
                intLinesCount++;

                DWORD dwResult = dwGetFilePointer(hFile);
                if (dwResult == -1) {
                    //error
                    return -1;
                }

                //log this pointer
                dwLastNewLineCharLocation = dwResult;

                
                //check if its the first line
                if (intLinesCount == 1) {
                    lOutputFilePointerEndOfFirstLine = dwResult;
                }
                



            }
        }

    }

    lOutputFilePointerEndOfSecondLastLine = dwLastNewLineCharLocation;


    return intLinesCount;
}

