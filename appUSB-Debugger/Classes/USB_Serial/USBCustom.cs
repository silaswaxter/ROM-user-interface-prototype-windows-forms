﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.IO.Ports;
using USBClassLibrary;

namespace appUSB_Debugger
{
    /// <summary>
    /// USBClass Library is used for the attach and remove events.  SerialPorts/ComPorts are found
    /// through code that utilizes "Microsoft.Win32" and "using System.Text.RegularExpressions;".
    /// The USBClass Library does NOT distuinguish/classify devices by VID or PID when invoking attach
    /// and remove events.  For this reason, filtering is done via the previously mentioned references in
    /// the invoked event methods.
    /// </summary>
    public class USBCustom
    {
        //private uint VID = 0x1A86, PID = 0x7523;
        private string VID_string = "1A86", PID_string = "7523";

        public void SetupUSBForEvents(USBClass LibraryUSB, Form1 form1)
        {
            var ListOfUSBDeviceProperties = new List<USBClass.DeviceProperties>();
            LibraryUSB.USBDeviceAttached += new USBClass.USBDeviceEventHandler(USBDeviceAttached);
            LibraryUSB.USBDeviceRemoved += new USBClass.USBDeviceEventHandler(USBDeviceRemoved);
            LibraryUSB.RegisterForDeviceChange(true, form1.Handle);
        }

        /// <summary>
        /// Finds current comPorts with correct VID and PID.
        /// </summary>
        /// <param name="comPorts">List is cleared. Serial ports found added.</param>
        public void SearchComPorts(ref List<string> comPorts)
        {
            List<string> names = ComPortNames();
            if (names.Count > 0)
            {
                comPorts.Clear();
                foreach (String s in SerialPort.GetPortNames())
                {
                    if (names.Contains(s))
                    {
                        //System.Diagnostics.Debug.WriteLine(s);
                        comPorts.Add(s);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No COM ports found");
            }
        }

            /// <summary>
            /// Compile an array of COM port names associated with given VID and PID
            /// </summary>
            /// <param name="VID"></param>
            /// <param name="PID"></param>
            /// <returns></returns>
            List<string> ComPortNames()
            {
                String pattern = String.Format("^VID_{0}.PID_{1}", VID_string, PID_string);
                Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
                List<string> comports = new List<string>();
                RegistryKey rk1 = Registry.LocalMachine;
                RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
                foreach (String s3 in rk2.GetSubKeyNames())
                {
                    RegistryKey rk3 = rk2.OpenSubKey(s3);
                    foreach (String s in rk3.GetSubKeyNames())
                    {
                        if (_rx.Match(s).Success)
                        {
                            RegistryKey rk4 = rk3.OpenSubKey(s);
                            foreach (String s2 in rk4.GetSubKeyNames())
                            {
                                RegistryKey rk5 = rk4.OpenSubKey(s2);
                                RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                                comports.Add((string)rk6.GetValue("PortName"));
                            }
                        }
                    }
                }
                return comports;
            }

        private void USBDeviceAttached(object sender, USBClass.USBDeviceEventArgs e)
        {
            //create list with current comPorts
            var currentComPorts = new List<string>();
            SearchComPorts(ref currentComPorts);

            //create list with elements in currentComPorts, but NOT in containersComPorts
            var comPortAttached = currentComPorts.Except(Serial.CurrentComPortsInContainers());

            foreach (string comPort in comPortAttached)
            {
                System.Diagnostics.Debug.WriteLine("DEVICE ATTACHED:   " + comPort);
                Serial.AddDevice(comPort);
                Pairing.Pair(Serial.deviceBeingAdded.serialPort);
            }
        }

        private void USBDeviceRemoved(object sender, USBClass.USBDeviceEventArgs e)
        {
            //create list with current comPorts
            var currentComPorts = new List<string>();
            SearchComPorts(ref currentComPorts);

            //create list with elements in containersComPorts, but NOT in currentComPorts
            var portRemoved = Serial.CurrentComPortsInContainers().Except(currentComPorts);

            foreach (string comPort in portRemoved)
            {
                System.Diagnostics.Debug.WriteLine("DEVICE Removed:   " + comPort);
                Serial.RemoveDevice(comPort);
            }
        }
    }
}