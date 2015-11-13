using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;

namespace NMEAReader
{
    /// <summary>
    /// Interaction logic for PortSettings.xaml
    /// </summary>
    public partial class PortSettings : Window
    {
        public PortSettings ()
        {
            InitializeComponent ();
        }

        private void okBtn_Click (object sender, RoutedEventArgs e)
        {
            saveSettings ();
            Close ();
        }

        private void saveSettings ()
        {
            FileStream fs = null;
            string settingFilename = @"Settings.ini";
            string pathString = Directory.GetCurrentDirectory () + settingFilename;

            SerialPortInfo spi = new SerialPortInfo ();
            spi = readSetting ();

            if (File.Exists (pathString))
            {
                File.Delete (pathString);
            }
            try
            {
                using (fs = new FileStream (pathString, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    using (var fw = new StreamWriter (fs))
                    {
                        fw.WriteLine ("[Main Settings]");
                        fw.WriteLine ("COM = " + spi.strCom);
                        fw.WriteLine ("SPEED = " + spi.nSpeed);
                        fw.WriteLine ("DATABIT = " + spi.nDatabit);
                        fw.WriteLine ("STOPBIT = " + spi.fStopbit);
                        fw.WriteLine ("PARITY = " + spi.strParity);
                        fw.WriteLine ("FLOWCONTROL = " + spi.strFlowcontrol);
                        fw.Flush (); // Added
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show ("ERROR THROWN");
            }

        }

        private SerialPortInfo readSetting ()
        {
            SerialPortInfo spc = new SerialPortInfo ();
            if (comSpeed.Text != "" && comDatabit.Text != "" && comStopbit.Text != "")
            {
                string strCom = comList.Text;
                int nSpeed = int.Parse (comSpeed.Text);
                int nDatabit = int.Parse (comDatabit.Text);
                float fStopbit = float.Parse (comStopbit.Text);
                string strParity = comParity.Text;
                string strFlowcontrol = comFlowControl.Text;

                spc.strCom = strCom;
                spc.nSpeed = nSpeed;
                spc.nDatabit = nDatabit;
                spc.fStopbit = fStopbit;
                spc.strParity = strParity;
                spc.strFlowcontrol = strFlowcontrol;
            }
            return spc;
        }

        private void cancelBtn_Click (object sender, RoutedEventArgs e)
        {
            Close ();
        }

        private bool checkIsDigital (TextCompositionEventArgs e)
        {
            char c = Convert.ToChar (e.Text);
            if (Char.IsNumber (c))
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private static bool IsTextAllowed (string text)
        {
            Regex regex = new Regex ("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch (text);
        }

        private static bool IsDotAllowed (string text)
        {
            Regex regex = new Regex ("[^0-9.]+"); //regex that matches disallowed text
            return !regex.IsMatch (text);
        }

        private void comSpeed_PreviewTextInput (object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed (e.Text);
        }

        private void comDatabit_PreviewTextInput (object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed (e.Text);
        }

        private void comStopbit_PreviewTextInput (object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDotAllowed (e.Text);
        }
    }
}
