/*
*
* Copyright (C) 2026  YAN-LIN, CHEN
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

#include <cstdint>

#include "ServiceSettings.h"

// Persists the settings and the print history in
// HKLM\SOFTWARE\evilhawk00\Printhead Maintainer (always the 64-bit registry view).
// The installer creates the key and grants the service account write access to it.
// The service is the only component that reads or writes these values.
//
// All functions are thread-safe; each call reads or writes its group of values as a whole,
// so a reader never sees half of an update.
namespace SettingsStore
{
    // Missing or invalid values fall back to their defaults.
    ServiceSettings LoadSettings();
    bool SaveSettings(const ServiceSettings& settings);

    PrintHistory LoadHistory();
    bool RecordSuccess(uint64_t timeUtc);
    bool RecordFailure(PrintKind kind, uint64_t timeUtc, FailureReason reason);
}
