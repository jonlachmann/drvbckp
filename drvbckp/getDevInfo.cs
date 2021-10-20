using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace drvbckp
{
    public class getDevInfo
    {
        public getDevInfo()
        {
        }

        public ArrayList devList(bool msDrivers)
        {
            //Build a list of all the installed drivers on the system
            ArrayList deviceList = new ArrayList();
            string regLocation = "System\\CurrentControlSet\\Control\\Class";
            RegistryKey classesKey = Registry.LocalMachine.OpenSubKey(regLocation);
            string[] controlSetClasses = classesKey.GetSubKeyNames();
            foreach (string controlSetClass in controlSetClasses)
            {
                RegistryKey devicesKey = Registry.LocalMachine.OpenSubKey(regLocation + "\\" + controlSetClass);
                string[] devices = devicesKey.GetSubKeyNames();
                foreach (string device in devices)
                {
                    string devDriverDesc = "";
                    string devDriverVersion = "";
                    string devInfPath = "";
                    string devInfSection = "";
                    string devId = "";
                    string devProviderName = "";
                    try
                    {
                        //Visit the registry in the hunt for drivers in the controlset
                        RegistryKey deviceKey = Registry.LocalMachine.OpenSubKey(regLocation + "\\" + controlSetClass + "\\" + device);
                        try
                        {
                            devDriverDesc = deviceKey.GetValue("DriverDesc").ToString();
                            devDriverDesc = devDriverDesc.Replace("/", " ");
                            devDriverVersion = deviceKey.GetValue("DriverVersion").ToString();
                            devInfPath = deviceKey.GetValue("InfPath").ToString();
                            devInfSection = deviceKey.GetValue("InfSection").ToString();
                            devId = deviceKey.GetValue("MatchingDeviceId").ToString();
                            devProviderName = deviceKey.GetValue("ProviderName").ToString();
                        }

                        catch
                        {
                        }

                        //Exclude MS drivers as they are included with the OS, add others to our list
                        if (!msDrivers)
                        {
                            if (devInfPath.Length > 0 && devInfSection.Length > 0 && devProviderName != "Microsoft" && devProviderName.Length > 0)
                            {
                                Console.Out.WriteLine("Found device: " + devDriverDesc);
                                device devDriver = new device(devDriverDesc, devDriverVersion, devInfPath, devInfSection, devId, devProviderName);
                                deviceList.Add(devDriver);
                            }
                        }
                        else if (devInfPath.Length > 0 && devInfSection.Length > 0 && devProviderName.Length > 0)
                        {
                            Console.Out.WriteLine("Found device: " + devDriverDesc);
                            device devDriver = new device(devDriverDesc, devDriverVersion, devInfPath, devInfSection, devId, devProviderName);
                            deviceList.Add(devDriver);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return deviceList;
        }
    }
}
