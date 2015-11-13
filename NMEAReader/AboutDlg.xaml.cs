﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NMAEReader
{
    /// <summary>
    /// Interaction logic for about.xaml
    /// </summary>
    public partial class AboutDlg : Window
    {
        public AboutDlg ()
        {
            InitializeComponent ();
        }

        private void button_Click (object sender, RoutedEventArgs e)
        {
            this.Close ();
        }

        private void Hyperlink_RequestNavigate (object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start (new ProcessStartInfo (e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
