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
#include <atomic>

static std::atomic<int> aintPrintingStatus = 0;

static std::atomic<bool> abNotifyClientScheduledPrintingFailureRecordUpdated = false;
static std::atomic<bool> abNotifyClientManualPrintingFailureRecordUpdated = false;
static std::atomic<bool> abNotifyClientPrintingSucceededRecently = false;

int intGetPrintingStatus() {


	return aintPrintingStatus;

}

void vUpdatePrintingStatus(int intStatusCode) {

	aintPrintingStatus = intStatusCode;
}

//for scheduled printing
bool bGetScheduledPrintingFailureRecordNotifyFlag() {

	return abNotifyClientScheduledPrintingFailureRecordUpdated;
}

void vUpdateScheduledPrintingFailureRecordNotifyFlag(bool bValue) {

	abNotifyClientScheduledPrintingFailureRecordUpdated = bValue;
}

//for manual printing
bool bGetManualPrintingFailureRecordNotifyFlag() {

	return abNotifyClientManualPrintingFailureRecordUpdated;
}

void vUpdateManualPrintingFailureRecordNotifyFlag(bool bValue) {

	abNotifyClientManualPrintingFailureRecordUpdated = bValue;
}

//for Printing Succeeded Recently
bool bGetPrintingSucceededRecentlyNotifyFlag() {

	return abNotifyClientPrintingSucceededRecently;
}

void vUpdatePrintingSucceededRecentlyNotifyFlag(bool bValue) {

	abNotifyClientPrintingSucceededRecently = bValue;
}