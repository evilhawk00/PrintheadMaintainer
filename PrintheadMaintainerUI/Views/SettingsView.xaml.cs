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
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrintheadMaintainerUI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        // Only digits can be typed into the interval box; the view model checks the range.
        private void PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(ch => ch >= '0' && ch <= '9');
        }
    }
}
