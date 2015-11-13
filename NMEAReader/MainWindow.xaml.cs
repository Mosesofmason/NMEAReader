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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Collections;
using NMAEReader;

namespace NMEAReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serial = null;
        ArrayList markList = new ArrayList ();
        string lastString = "";

        public MainWindow ()
        {
            SetBrowserFeatureControl ();
            InitializeComponent ();
        }

        private void portSettingMenu_Click (object sender, RoutedEventArgs e)
        {
            PortSettings portDlg = new PortSettings ();
            portDlg.ShowDialog ();
        }

        private void portOpenMenu_Click (object sender, RoutedEventArgs e)
        {
            serial = new SerialPort ();
            SerialPortInfo spi = readSettingFile ();

            Parity parity = Parity.None;
            StopBits stopbits = StopBits.None;
            Handshake flowcontrol = Handshake.None;

            switch (spi.strParity.ToUpper ())
            {
                case "NONE":
                    parity = Parity.None;
                    break;
                case "ODD":
                    parity = Parity.Odd;
                    break;
                case "EVEN":
                    parity = Parity.Even;
                    break;
                case "MARK":
                    parity = Parity.Mark;
                    break;
                case "SPACE":
                    parity = Parity.Space;
                    break;
                default:
                    parity = Parity.None;
                    break;
            }

            switch (spi.fStopbit.ToString())
            {
                case "0":
                    stopbits = StopBits.None;
                    break;
                case "1":
                    stopbits = StopBits.One;
                    break;
                case "1.5":
                    stopbits = StopBits.OnePointFive;
                    break;
                case "2":
                    stopbits = StopBits.Two;
                    break;
                default:
                    stopbits = StopBits.None;
                    break;
            }

            switch (spi.strFlowcontrol.ToUpper())
            {
                case "NONE":
                    flowcontrol = Handshake.None;
                    break;
                case "XON/XOFF":
                    flowcontrol = Handshake.XOnXOff;
                    break;
                case "RTS/CTS":
                    flowcontrol = Handshake.RequestToSend;
                    break;
                case "DSR/DTR":
                    serial.DtrEnable = true;
                    break;
                default:
                    flowcontrol = Handshake.None;
                    break;
            }

            serial.PortName = spi.strCom; //Com Port Name                
            serial.BaudRate = spi.nSpeed; //COM Port Sp
            serial.Handshake = flowcontrol;
            serial.Parity = parity;
            serial.DataBits = spi.nDatabit;
            serial.StopBits = stopbits;
            serial.ReadTimeout = 1000;
            serial.WriteTimeout = 500;

            try
            {
                serial.Open ();
                serial.DataReceived += new SerialDataReceivedEventHandler (Recieve);
            }
            catch (Exception ee)
            {
                Debug.WriteLine (ee.StackTrace);
            }

        }

        private delegate void UpdateUiTextDelegate (string text);
        private void Recieve (object sender, SerialDataReceivedEventArgs e)
        {

            // Collecting the characters received to our 'buffer' (string).
            string recieved_data = serial.ReadExisting ();
            Dispatcher.Invoke (DispatcherPriority.Send, 
                new UpdateUiTextDelegate (WriteData), 
                recieved_data);
            if (!recieved_data.Contains (System.Environment.NewLine))
            {
                lastString += recieved_data;
            }
            else
            {
                string [] dataArray = recieved_data.Split (new char [] { '\n' }, 2);
                lastString += dataArray [0];
                ParseNMEALine (lastString);
                lastString = dataArray [1];
            }
        }

        private void WriteData (string text)
        {

            FlowDocument mcFlowDoc = new FlowDocument ();
            Paragraph para = new Paragraph ();

            // Assign the value of the plot to the RichTextBox.
            para.Inlines.Add (text);
            mcFlowDoc.Blocks.Add (para);
            //Commdata.Document = mcFlowDoc;
            rawText.Document = mcFlowDoc;
        }

        private SerialPortInfo readSettingFile ()
        {
            FileStream fs = null;
            string settingFilename = @"Settings.ini";
            string pathString = Directory.GetCurrentDirectory () + settingFilename;

            SerialPortInfo spi = new SerialPortInfo ();

            try
            {
                using (fs = new FileStream (pathString, FileMode.Open, FileAccess.ReadWrite))
                {
                    using (var fr = new StreamReader (fs))
                    {
                        while (fr.Peek () >= 0)
                        {
                            string strSetting = fr.ReadLine ();
                            string [] settingArray = strSetting.Trim ().Split ('=');
                            switch (settingArray[0].Trim().ToUpper())
                            {
                                case "COM":
                                    spi.strCom = settingArray [1].Trim();
                                    break;
                                case "SPEED":
                                    spi.nSpeed = Int16.Parse(settingArray [1].Trim ());
                                    break;
                                case "DATABIT":
                                    spi.nDatabit = Int16.Parse (settingArray [1].Trim ());
                                    break;
                                case "STOPBIT":
                                    spi.fStopbit = float.Parse (settingArray [1].Trim ());
                                    break;
                                case "PARITY":
                                    spi.strParity = settingArray [1].Trim ();
                                    break;
                                case "FLOWCONTROL":
                                    spi.strFlowcontrol = settingArray [1].Trim ();
                                    break;
                                default:
                                    break;
                            }
                        }                     
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show ("ERROR THROWN");
            }

            return spi;
        }

        [PermissionSet (SecurityAction.Demand, Name = "FullTrust")]
        [ComVisible (true)]
        private void openFileMenu_Click (object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog ();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog () == true)
            {
                this.wbControler.InvokeScript ("clearMap");

                // 1
                // Declare new List.
                List<string> lines = new List<string> ();

                // 2
                // Use using StreamReader for disposing.
                using (StreamReader r = new StreamReader (openFileDialog.FileName))
                {
                    // 3
                    // Use while != null pattern for loop
                    string line;
                    while ((line = r.ReadLine ()) != null)
                    {
                        ParseNMEALine (line);
                        // 4
                        // Insert logic here.
                        // ...
                        // "line" is a line in the file. Add it to our List.
                        lines.Add (line);
                    }
                    foreach (LatLongPosition mark in markList)
                    {
                        object rst = this.wbControler.InvokeScript ("addPoint", mark.latitude, mark.longitude);
                    }
//                  object rst = this.wbControler.InvokeScript ("addMark", -25.363882, 131.044922);
                    this.wbControler.InvokeScript ("setMap");
                }

                // 5
                // Print out all the lines.

                rawTextDisplay (lines);
            }
        }

        private void rawTextDisplay (List<string> lines)
        {
            rawText.Document.Blocks.Clear ();
            rawText.AppendText (System.Environment.NewLine);
            foreach (string s in lines)
            {
                Console.WriteLine (s);
                rawText.AppendText (s + System.Environment.NewLine);
            }
            rawText.ScrollToEnd ();
        }

        private void ParseNMEALine (string line)
        {
            string [] lineArray = line.Trim().Split ('*');
            if (lineArray [1] != null && lineArray [0].StartsWith ("$GP")) 
            {
                if (checksum (lineArray [0], lineArray [1]))
                {
                    string strNMEA = lineArray [0].Substring (3);
                    string [] NMEADataArray = strNMEA.Split (',');
                    // eg: GPGGA,092750.000,5321.6802,N,00630.3372,W,1,8,1.03,61.7,M,55.2,M,,
                    /*
                    AAM - Waypoint Arrival Alarm
                    ALM - Almanac data
                    APA - Auto Pilot A sentence
                    APB - Auto Pilot B sentence
                    BOD - Bearing Origin to Destination
                    BWC - Bearing using Great Circle route
                    DTM - Datum being used.
                    GGA - Fix information
                    GLL - Lat/Lon data
                    GRS - GPS Range Residuals
                    GSA - Overall Satellite data
                    GST - GPS Pseudorange Noise Statistics
                    GSV - Detailed Satellite data
                    MSK - send control for a beacon receiver
                    MSS - Beacon receiver status information.
                    RMA - recommended Loran data
                    RMB - recommended navigation data for gps
                    RMC - recommended minimum data for gps
                    RTE - route message
                    TRF - Transit Fix Data
                    STN - Multiple Data ID
                    VBW - dual Ground / Water Spped
                    VTG - Vector track an Speed over the Ground
                    WCV - Waypoint closure velocity (Velocity Made Good)
                    WPL - Waypoint Location information
                    XTC - cross track error
                    XTE - measured cross track error
                    ZTG - Zulu (UTC) time and time to go (to destination)
                    ZDA - Date and Time
                    */


                    switch (NMEADataArray [0])
                    {
                        case "AAM":
                            break;
                        case "ALM":
                            break;
                        case "APA":
                            break;
                        case "APB":
                            break;
                        case "BOD":
                            break;
                        case "BWC":
                            break;
                        case "DTM":
                            break;
                        case "GGA":
                            ParseGGAData (NMEADataArray);
                            break;
                        case "GLL":
                            break;
                        case "GRS":
                            break;
                        case "GSA":
                            break;
                        case "GST":
                            break;
                        case "GSV":
                            break;
                        case "MSK":
                            break;
                        case "MSS":
                            break;
                        case "RMA":
                            break;
                        case "RMB":
                            break;
                        case "RMC":
                            break;
                        case "RTE":
                            break;
                        case "TRF":
                            break;
                        case "STN":
                            break;
                        case "VBW":
                            break;
                        case "VTG":
                            break;
                        case "WCV":
                            break;
                        case "WPL":
                            break;
                        case "XTC":
                            break;
                        case "XTE":
                            break;
                        case "ZTG":
                            break;
                        case "ZDA":
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        [PermissionSet (SecurityAction.Demand, Name = "FullTrust")]
        [ComVisible (true)]
        private void ParseGGAData (string [] nMEADataArray)
        {
            string timeUTC = nMEADataArray [1];
            string latitude = nMEADataArray [2];
            string latitudeUnit = nMEADataArray [3];
            string longitude = nMEADataArray [4];
            string longitudeUnit = nMEADataArray [5];
            string fixQuality = nMEADataArray [6];      // Fix quality: 0 = invalid
                                                        // 1 = GPS fix (SPS)
                                                        // 2 = DGPS fix
                                                        // 3 = PPS fix
                                                        // 4 = Real Time Kinematic
                                                        // 5 = Float RTK
                                                        // 6 = estimated (dead reckoning) (2.3 feature)
                                                        // 7 = Manual input mode
                                                        // 8 = Simulation mode
            string nos = nMEADataArray [7];             // Number of satellites being tracked
            string hdp = nMEADataArray [9];             // Horizontal dilution of position
            string altitude = nMEADataArray [10];
            string altitudeUnit = nMEADataArray [11];
            string hog = nMEADataArray [12];            // Height of geoid (mean sea level) above WGS84 ellipsoid
            string lastDGPSTime = nMEADataArray [13];   // time in seconds since last DGPS update
            string idDGPS = nMEADataArray [14];         // DGPS station ID number

            if (int.Parse (fixQuality) < 8 || int.Parse (fixQuality) > 0)
            {
                double fraction = 0;
                double lat = float.Parse (latitude) / 100.0;
                fraction = float.Parse (latitude) % 100 / 60;
                lat += fraction;
                double lon = float.Parse (longitude) / 100.0;
                fraction = float.Parse (longitude) % 100 / 60;
                lon += fraction;

                if (latitudeUnit.Equals ("S"))
                {
                    lat *= -1;
                }
                if (longitudeUnit.Equals ("W"))
                {
                    lon *= -1;
                }

                LatLongPosition latlongPos = new LatLongPosition ();
                latlongPos.latitude = lat;
                latlongPos.longitude = lon;

                markList.Add (latlongPos);
            }    
        }

        private bool checksum (string line, string sum)
        {
            int result = 0;
            // hex to decimal
            int iSum = int.Parse (sum, System.Globalization.NumberStyles.HexNumber); 
            for (int i = 1; i < line.Length; i++)
            {
                char c = line.ElementAt (i);
                result ^= c;
            }
            if (result == iSum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void exitProgMenu_Click (object sender, RoutedEventArgs e)
        {
            this.Close ();
        }

        private void wbControler_Initialized (object sender, EventArgs e)
        {
            string curDir = Directory.GetCurrentDirectory ();
            wbControler.Navigate (String.Format ("file:///{0}/map.html", curDir));
        }

        private void SetBrowserFeatureControlKey (string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey (
                String.Concat (@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue (appName, (UInt32) value, RegistryValueKind.DWord);
            }
        }

        private void SetBrowserFeatureControl ()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // FeatureControl settings are per-process
            var fileName = System.IO.Path.GetFileName (Process.GetCurrentProcess ().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare (fileName, "devenv.exe", true) == 0 || String.Compare (fileName, "XDesProc.exe", true) == 0)
                return;

            SetBrowserFeatureControlKey ("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode ()); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
            SetBrowserFeatureControlKey ("FEATURE_AJAX_CONNECTIONEVENTS", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_DOMSTORAGE ", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_GPU_RENDERING ", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_IVIEWOBJECTDRAW_DMLT9_WITH_GDI  ", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_DISABLE_LEGACY_COMPRESSION", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_LOCALMACHINE_LOCKDOWN", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_BLOCK_LMZ_OBJECT", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_BLOCK_LMZ_SCRIPT", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_DISABLE_NAVIGATION_SOUNDS", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_SCRIPTURL_MITIGATION", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_SPELLCHECKING", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_STATUS_BAR_THROTTLING", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_TABBED_BROWSING", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_VALIDATE_NAVIGATE_URL", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_WEBOC_DOCUMENT_ZOOM", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_WEBOC_POPUPMANAGEMENT", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_WEBOC_MOVESIZECHILD", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_ADDON_MANAGEMENT", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_WEBSOCKET", fileName, 1);
            SetBrowserFeatureControlKey ("FEATURE_WINDOW_RESTRICTIONS ", fileName, 0);
            SetBrowserFeatureControlKey ("FEATURE_XMLHTTP", fileName, 1);
        }

        private UInt32 GetBrowserEmulationMode ()
        {
            int browserVersion = 7;
            using (var ieKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue ("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue ("Version");
                    if (null == version)
                        throw new ApplicationException ("Microsoft Internet Explorer is required!");
                }
                int.TryParse (version.ToString ().Split ('.') [0], out browserVersion);
            }

            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode. Default value for Internet Explorer 11.
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. Default value for applications hosting the WebBrowser Control.
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. Default value for Internet Explorer 8
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode. Default value for Internet Explorer 9.
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10. Webpages containing standards-based !DOCTYPE directives are displayed in IE10 mode. Default value for Internet Explorer 10.
                    break;
                default:
                    // use IE11 mode by default
                    break;
            }

            return mode;
        }

        private void aboutMenu_Click (object sender, RoutedEventArgs e)
        {
            AboutDlg abtDlg = new AboutDlg ();
            abtDlg.ShowDialog ();
        }
    }
}
