using PVLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PV_Controller
{
    internal class ControllerRef
    {
        public string Location = string.Empty;
        public int port;
        ChannelList ChannelList = new();
        public ControllerRef(string name, int prt, ChannelList list)
        {
            Location = name;
            ChannelList = list;
            port = prt;
        }
    }
}
