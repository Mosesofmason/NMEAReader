using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NMEAReader
{
    class SerialPortInfo
    {
        public string strCom { get; set; }
        public int nSpeed { get; set; }
        public int nDatabit { get; set; }
        public float fStopbit { get; set; }
        public string strParity { get; set; }
        public string strFlowcontrol { get; set; }
    }
}
