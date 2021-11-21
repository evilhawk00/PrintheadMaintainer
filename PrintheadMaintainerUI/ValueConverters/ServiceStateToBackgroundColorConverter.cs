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
using System.Windows.Media;

namespace PrintheadMaintainerUI.ValueConverters
{
    public class ServiceStateToBackgroundColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ServiceState val = (ServiceState)value;
            if (val == ServiceState.OK)
            {
                return Brushes.Green; // ff008000


            }
            else if (val == ServiceState.Warning)
            {
                return new SolidColorBrush(Color.FromRgb(255, 128, 0));//#ff8000
            }
            else if (val == ServiceState.Error)
            {
                return new SolidColorBrush(Color.FromRgb(199, 25, 0));//#c71900
            }
            else {
                //ServiceState.Transforming
                return new SolidColorBrush(Color.FromRgb(12, 138, 201)); //#0c8ac9
            }
                
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

}
