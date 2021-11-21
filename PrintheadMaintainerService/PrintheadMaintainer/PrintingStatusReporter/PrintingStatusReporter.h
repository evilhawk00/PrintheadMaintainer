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

#define intPrinterStatus_Printing 1
#define intPrinterStatus_NotPrinting 0
#define intPrinterStatus_WaitingCountdownBeforePrint 2


int intGetPrintingStatus();
void vUpdatePrintingStatus(int intStatusCode);

//for scheduled printing
bool bGetScheduledPrintingFailureRecordNotifyFlag();
void vUpdateScheduledPrintingFailureRecordNotifyFlag(bool bValue);

//for manual printing
bool bGetManualPrintingFailureRecordNotifyFlag();
void vUpdateManualPrintingFailureRecordNotifyFlag(bool bValue);

//for Printing Succeeded Recently
bool bGetPrintingSucceededRecentlyNotifyFlag();
void vUpdatePrintingSucceededRecentlyNotifyFlag(bool bValue);
