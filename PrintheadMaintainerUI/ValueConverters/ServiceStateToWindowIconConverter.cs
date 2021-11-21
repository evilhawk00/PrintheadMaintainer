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
using PrintheadMaintainerUI.Enums;
using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PrintheadMaintainerUI.ValueConverters
{
    public class ServiceStateToWindowIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ServiceState val = (ServiceState)value;
            if (val == ServiceState.OK)
            {

                return BfGetBitmapFrameByURI("pack://application:,,,/PrintheadMaintainer;component/Resources/Icon_Green.ico");
            }
            else if (val == ServiceState.Warning)
            {

                return BfGetBitmapFrameByURI("pack://application:,,,/PrintheadMaintainer;component/Resources/Icon_Orange.ico");
            }
            else if (val == ServiceState.Error)
            {

                return BfGetBitmapFrameByURI("pack://application:,,,/PrintheadMaintainer;component/Resources/Icon_Red.ico");
            }
            else {
                //ServiceState.Transforming
                return BfGetBitmapFrameByURI("pack://application:,,,/PrintheadMaintainer;component/Resources/Icon_Blue.ico");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private BitmapFrame BfGetBitmapFrameByURI(string strUri)
        {
            IconBitmapDecoder ibd = new IconBitmapDecoder(
            new Uri(strUri, UriKind.RelativeOrAbsolute),
            BitmapCreateOptions.None,
            BitmapCacheOption.Default);
            return ibd.Frames[0];
        }

    }
}
