using System;
using System.Management;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace drvbckp
{
    class Program
    {
        static void Main(string[] args)
        {
            string bckpDir = null;
            string argLower;
            bool help = false;
            bool overWrite = false;
            bool goodArgs = true;
            bool msDrivers = false;
            bool debug = false;
            
            foreach (string arg in args)
            {
                argLower = arg.ToLower();
                string[] argSplit = argLower.Split('=');
                if (argSplit.Count() > 0)
                {
                    switch (argSplit[0])
                    {
                        case "-bckpdir":
                            bckpDir = argSplit[1];
                            break;
                        case "-overwrite":
                            overWrite = true;
                            break;
                        case "-help":
                            getHelp();
                            help = true;
                            break;
                        case "-msdrv":
                            msDrivers = true;
                            break;
                        case "-debug":
                            debug = true;
                            break;
                        default:
                            goodArgs = false;
                            break;
                    }
                }
            }

            if (goodArgs && !help)
            {
                bckpDir = setBckpDir(bckpDir);
                if (createBckpDir(bckpDir, overWrite))
                {
                    getDevInfo devInfo = new getDevInfo();
                    ArrayList deviceList = devInfo.devList(msDrivers);

                    getDrvFiles drvList = new getDrvFiles();
                    ArrayList driverList = drvList.drvList(deviceList, debug);

                    getCatFiles catList = new getCatFiles();
                    List<string> catalogList = catList.catList(driverList, debug);

                    doBckpDrv bckpDrv = new doBckpDrv();
                    bool doBckp = bckpDrv.bckpDrv(driverList, bckpDir, overWrite, debug);
                }
            }
            else if (!help)
            {
                getUsage();
            }
            if (debug) Console.Out.WriteLine("Press any key to exit...");
            if (debug) Console.ReadKey();
        }

        public static bool createBckpDir(string bckpPath, bool overWrite)
        {
            if (Directory.Exists(bckpPath) && overWrite)
            {
                return true;
            }
            else if (!Directory.Exists(bckpPath))
            {
                try
                {
                    Directory.CreateDirectory(bckpPath);
                    return true;
                }
                catch
                {
                    Console.Out.WriteLine("FATAL: Could not create backup directory");
                    return false;
                }                
            }
            else
            {
                Console.Out.WriteLine("FATAL: Could not create backup directory");
                return false;
            }

        }

        public static bool getHelp()
        {
            getUsage();
            Console.Out.WriteLine("/nThere are some expressions that you can use to format the output directory,");
            Console.Out.WriteLine("every expression should be surrounded with \"?\"'s \n");
            Console.Out.WriteLine("The available expressions are:");
            Console.Out.WriteLine("?model? = the WMI Win32_ComputerSystem Model, i.e. " + getModel());
            Console.Out.WriteLine("?name? = the local computer hostname i.e. " + Environment.MachineName);
            Console.Out.WriteLine("?mac? = the mac address of the first ethernet adapter i.e. " + getMac());
            return true;
        }

        public static bool getUsage()
        {
            Console.Out.WriteLine("Usage: drvbckp [-bckpdir=Drive:\\Path] [-overwrite] [-msdrv] [-help]");
            Console.Out.WriteLine("Example: drvbckp -bkcpdir=C:\\?model?\\Drivers -overwrite");
            return true;
        }

        public static string getModel()
        {
            string model = null;
            ManagementObjectSearcher query = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
            foreach (ManagementObject queryobj in query.Get()) model = queryobj["model"].ToString();
            return model;
        }

        public static string getMac()
        {
            string mac = null;
            ManagementObjectSearcher query2 = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter");
            foreach (ManagementObject queryobj in query2.Get())
            {
                try
                {
                    string ad = queryobj["AdapterType"].ToString();
                    string mf = queryobj["Manufacturer"].ToString();
                    if (queryobj["AdapterType"].ToString() == "Ethernet 802.3" && queryobj["Manufacturer"].ToString() != "Microsoft" && mac == null)
                    {
                        mac = queryobj["MacAddress"].ToString();
                    }
                }
                catch { }
            }
            mac = mac.Replace(":", "");
            return mac;
        }
        
        public static string setBckpDir(string bckpDir)
        {
            if (bckpDir == null) return getModel();
            string[] bckpDirSplit = null;
            string bckpPath = null;
            bckpDirSplit = bckpDir.Split('?');
            foreach (string folder in bckpDirSplit)
            {
                string path = null;
                switch (folder.ToLower())
                {
                    case "model":
                        path = getModel();
                        break;
                    case "mac":
                        path = getMac();
                        break;
                    case "name":
                        path = Environment.MachineName;
                        break;
                    default:
                        path = folder;
                        break;
                }
                bckpPath = bckpPath + path;
            }

            if (bckpPath == null) bckpPath = getModel();
            return bckpPath;
        }

    }

    public class device
    {
        public string description;
        public string version;
        public string infPath;
        public string infSection;
        public string id;
        public string provider;

        public device(string description, string version, string infPath, string infSection, string id, string provider)
        {
            this.description = description;
            this.version = version;
            this.infPath = infPath;
            this.infSection = infSection;
            this.id = id;
            this.provider = provider;
        }
    }
    public class driver : device
    {
        public List<file> files;
        public List<string> sourceDisks;
        public string catFile;
        public string catFileName;

        public driver(List<file> files, List<string> sourceDisks, string catFileName, string catFile, string description, string version, string infPath, string infSection, string id, string provider) : base("", "", "", "", "", "")
        {
            this.files = files;
            this.sourceDisks = sourceDisks;
            this.catFile = catFile;
            this.catFileName = catFileName;
            this.description = description;
            this.version = version;
            this.infPath = infPath;
            this.infSection = infSection;
            this.id = id;
            this.provider = provider;
        }
    }
    public class dirId
    {
        public int id;
        public string path;

        public dirId(int id, string path)
        {
            this.id = id;
            this.path = path;
        }
    }
    public class quote
    {
        public int start;
        public int end;
    }

    public class directive
    {
        public string name;
        public List<string> values;
    }
    public class file
    {
        public string name;
        public string srcname;
        public string source;
        public string targetpath;
        public string targetid;
    }
}
