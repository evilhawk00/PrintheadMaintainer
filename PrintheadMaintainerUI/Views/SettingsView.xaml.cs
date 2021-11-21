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
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrintheadMaintainerUI.Views
{
    /// <summary>
    /// SettingsView.xaml 的互動邏輯
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }


        private void TextChanged_RemoveSpace(object sender, EventArgs e)
        {
            //remove whitespaces
            (sender as TextBox).Text = Regex.Replace((sender as TextBox).Text, @"\s+", "");
            
        }

        private void PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            
            e.Handled = !e.Text.All(char.IsDigit);
        }


        

    }
}
