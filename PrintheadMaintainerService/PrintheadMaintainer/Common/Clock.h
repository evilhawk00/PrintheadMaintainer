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
#include <string>

// Timestamps are stored and exchanged as FILETIME ticks in UTC (100 ns units since 1601).
constexpr uint64_t kTicksPerSecond = 10'000'000;
constexpr uint64_t kTicksPerDay = 24 * 60 * 60 * kTicksPerSecond;

uint64_t CurrentUtcTicks();

// Current local time as "YYYY/MM/DD HH:MM:SS".
std::wstring FormatCurrentLocalTime();
