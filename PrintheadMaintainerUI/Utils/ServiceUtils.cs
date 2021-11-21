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
using System;
using System.ServiceProcess;

namespace PrintheadMaintainerUI.Utils
{
    public static class ServiceUtils
    {
        public enum ServiceStatus { Stopped, Running, StartPending, StopPending, Paused, ContinuePending, PausePending, UtilExceptionError}
        public static ServiceStatus SsGetServiceStatus(string strServiceName) {
            try
            {
                ServiceController sc = new ServiceController(strServiceName);

                switch (sc.Status)
                {
                    case ServiceControllerStatus.Stopped:
                        return ServiceStatus.Stopped;
                    case ServiceControllerStatus.Running:
                        return ServiceStatus.Running;
                    case ServiceControllerStatus.StartPending:
                        return ServiceStatus.StartPending;
                    case ServiceControllerStatus.StopPending:
                        return ServiceStatus.StopPending;
                    case ServiceControllerStatus.Paused:
                        return ServiceStatus.Paused;
                    case ServiceControllerStatus.ContinuePending:
                        return ServiceStatus.ContinuePending;
                    case ServiceControllerStatus.PausePending:
                        return ServiceStatus.PausePending;
                    default:
                        return ServiceStatus.UtilExceptionError;
                }
            }
            catch (Exception)
            {
                return ServiceStatus.UtilExceptionError;
            }


        }

    }
}
