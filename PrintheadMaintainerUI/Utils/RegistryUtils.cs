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
using Microsoft.Win32;

namespace PrintheadMaintainerUI.Utils
{
    public static class RegistryUtils
    {

        public static RegistryKey RkOpenRegistyHandle(bool bWritable) {

            try
            {
                
                return Registry.LocalMachine.OpenSubKey("SOFTWARE\\evilhawk00\\Printhead Maintainer", bWritable);
            }
            catch (Exception) {

                return null;
            }
        }

        public static int IntReadUnsignedInteger(RegistryKey rkRegistryHandle, string strValueName) {
            //returns unsigned int if success
            //returns -1 if value not exist
            //returns -2 if returned value is not unsigned int
            //returns -3 if exception happens

            try
            {
                object oResult = rkRegistryHandle.GetValue(strValueName);
                if (oResult == null)
                {
                    //result is null
                    return -1;
                }
                else {
                    //check if result is unsigned integer
                    try
                    {
                        //result is unsiged integer
                        int intResult = Convert.ToInt32(oResult);
                        return intResult;
                    }
                    catch (Exception)
                    {
                        //result is not unsigned integer
                        return -2;
                    }

                }

            }
            catch (Exception) {
                //exception occured
                return -3;
            }
        }
        public static string StrRegistryReadSingleLineString(RegistryKey rkRegistryHandle, string strValueName) {
            
            try
            {
                object oResult = rkRegistryHandle.GetValue(strValueName);
                if (oResult == null)
                {
                    return null;
                }
                else
                {
                    try
                    {
                        string strResult = oResult.ToString();
                        return strResult;
                    }
                    catch (Exception) {
                        return null;
                    }
                
                }

            }
            catch (Exception) {

                return null;
            }
             
        }

    }



}
