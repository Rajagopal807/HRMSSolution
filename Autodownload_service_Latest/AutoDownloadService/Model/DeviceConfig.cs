using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Model
{
    public class DeviceConfig
    {
        public Zkdevice ZKDevice { get; set; }

        public class Zkdevice
        {
            public Device[] Devices { get; set; }
        }

        public class Device
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string IpAddress { get; set; }
            public int Port { get; set; }
            public int Password { get; set; }
            public bool Enabled { get; set; }
            public bool ClearDeviceAfterSync { get; set; }
        }

    }
}
