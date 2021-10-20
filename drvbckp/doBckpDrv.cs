using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace drvbckp
{
    class doBckpDrv
    {
        List<dirId> dirIdList = new List<dirId>();

        public doBckpDrv()
        {
            //Build the dreaded directory id list, Microsoft, why not give us a PROPER function for this?
            dirIdList.Add(new dirId(10, Environment.GetEnvironmentVariable("SystemRoot"))); //windir
            dirIdList.Add(new dirId(11, getDirId(10) + "\\system32")); //sys32dir
            dirIdList.Add(new dirId(12, getDirId(11) + "\\drivers")); //drivers dir
            dirIdList.Add(new dirId(17, getDirId(10) + "\\inf")); //inf files dir
            dirIdList.Add(new dirId(18, getDirId(10) + "\\help")); //hlp files dir
            dirIdList.Add(new dirId(20, getDirId(10) + "\\Fonts")); //font files dir
            dirIdList.Add(new dirId(21, getDirId(10))); //FIXME! "viewers directory" dunno where it should be
            dirIdList.Add(new dirId(23, getDirId(11) + "\\spool\\drivers\\color")); //ICM Color directory
            dirIdList.Add(new dirId(24, Environment.GetEnvironmentVariable("SystemDrive"))); //sysdrive
            dirIdList.Add(new dirId(25, getDirId(10))); //FIXME! "shared directory" dunno where it should be
            dirIdList.Add(new dirId(30, getDirId(24))); //FIXME! Should be the ARC boot disk root, whatever that is
            dirIdList.Add(new dirId(50, getDirId(10) + "\\system")); //sysdir
            dirIdList.Add(new dirId(51, getDirId(11) + "\\spool")); //spooldir
            dirIdList.Add(new dirId(52, getDirId(51) + "\\drivers")); //spool drivers dir
            dirIdList.Add(new dirId(53, Environment.GetEnvironmentVariable("UserProfile"))); //User profile dir
            dirIdList.Add(new dirId(54, getDirId(10))); //FIXME! Should be where "ntldr.exe" and "osloader.exe" resides
            dirIdList.Add(new dirId(16406, getShellDir(0x0016))); //All Users\Start Menu
            dirIdList.Add(new dirId(16407, getShellDir(0x0017))); //All Users\Start Menu\Programs
            dirIdList.Add(new dirId(16408, getShellDir(0x0018))); //All Users\Start Menu\Programs\Startup
            dirIdList.Add(new dirId(16409, getShellDir(0x0019))); //All Users\Desktop
            dirIdList.Add(new dirId(16415, null)); //FIXME! All Users\Favorites
            dirIdList.Add(new dirId(16419, getShellDir(0x0023))); //All Users\Application Data
            dirIdList.Add(new dirId(16422, Environment.GetEnvironmentVariable("ProgramFiles"))); //Programfilesdir
            dirIdList.Add(new dirId(16425, getDirId(11))); // %SystemRoot%\system32 (valid for Microsoft Win32 user-mode applications that are running under Windows on Windows (WOW64))
            dirIdList.Add(new dirId(16426, getDirId(16422))); // (valid for Win32 user-mode applications that are running under WOW64)
            dirIdList.Add(new dirId(16427, Environment.GetEnvironmentVariable("CommonProgramFiles"))); // Program Files\Common
            dirIdList.Add(new dirId(16428, getDirId(16427))); //Program Files\Common (valid for Win32 user-mode applications that are running under WOW64)
            dirIdList.Add(new dirId(16429, getShellDir(0x002D))); //All Users\Templates
            dirIdList.Add(new dirId(16430, getShellDir(0x002E))); //All Users\Documents 
            dirIdList.Add(new dirId(66000, getDir66000())); //Represents the directory path returned by the GetPrinterDriverDirectory function.	Driver files and dependent files.
            dirIdList.Add(new dirId(66001, getDir66001())); //Represents the directory path returned by the GetPrintProcessorDirectory function.	Print processor files.
            dirIdList.Add(new dirId(66002, getDirId(11))); //Represents the directory path to additional files to be copied to \System32 of the local system. See the paragraph following this table.	Print monitor files.
            dirIdList.Add(new dirId(66003, null)); //FIXME! Represents the directory path returned by the GetColorDirectory function.	ICM color profile files.
            dirIdList.Add(new dirId(66004, getDirId(10))); //FIXME! Represents the directory path to which printer type-specific ASP files are copied.	ASP files and associated files.
        }
        
        public string getDirId(Int32 idNo)
        {
            dirId directory = dirIdList.Find(id => id.id == idNo);
            if (directory != null && directory.path != null)
            {
                return directory.path.ToString();
            }
            else
            {
                return Environment.GetEnvironmentVariable("SystemRoot");
            }
        }

        [DllImport("winspool.drv")]
        static extern bool GetPrinterDriverDirectory(StringBuilder pName,StringBuilder pEnv,int Level,[Out] StringBuilder outPath,int bufferSize,ref int Bytes);
        
        [DllImport("winspool.drv")]
        static extern bool GetPrintProcessorDirectory(StringBuilder pName,StringBuilder pEnv,int Level,[Out] StringBuilder outPath,int bufferSize,ref int Bytes);
        
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        public string getDir66000()
        {
            string returnDir;
            StringBuilder DirPath = new StringBuilder(1024);
            int i = 0;
            GetPrinterDriverDirectory(null, null, 1, DirPath, 1024, ref i);
            returnDir = DirPath.ToString();
            return returnDir;
        }
        public string getDir66001()
        {
            string returnDir;
            StringBuilder DirPath = new StringBuilder(1024);
            int i = 0;
            GetPrintProcessorDirectory(null, null, 1, DirPath, 1024, ref i);
            returnDir = DirPath.ToString();
            return returnDir;
        }


        public string getShellDir(int csidl)
        {
            string returnDir;
            StringBuilder path = new StringBuilder(1024);
            SHGetSpecialFolderPath(IntPtr.Zero, path, csidl, false);
            returnDir = path.ToString();
            return returnDir;
        }

        public bool bckpDrv(ArrayList driverList, string bckpDir, bool overWrite, bool debug)
        {
            if (Directory.Exists(bckpDir))
            {
                foreach (driver driver in driverList)
                {
                    if (debug) Console.Out.WriteLine("DEBUG: Copying files for device: " + driver.description);
                    //Create the folder for the driver
                    string driverDir = bckpDir + "\\" + driver.description;
                    if (!createFolder(driverDir)) return false;
                    
                    //Copy the inf file to the directory
                    copyFile(getDirId(17) + "\\" + driver.infPath, driverDir + "\\" + driver.infPath, driver.infPath, overWrite);
                    
                    //Create the sourcedisks folders (if any)
                    foreach (string sourceDisk in driver.sourceDisks)
                    {
                        if (!createFolder(driverDir + "\\" + sourceDisk)) return false;
                    }

                    foreach (file file in driver.files)
                    {
                        if (file.source != null && file.source.Length > 0)
                        {
                            if (!createFolder(driverDir + "\\" + file.source)) return false;
                        }
                    }

                    //Copy the driver files to the directory
                    foreach (file file in driver.files)
                    {
                        string filePath = "";
                        string targetPath = "";
                        if (file.targetid != null)
                        {
                            filePath = getDirId(Convert.ToInt32(file.targetid));
                        }
                        if (file.targetpath != null)
                        {
                            filePath += "\\" + file.targetpath;
                        }
                        filePath += "\\" + file.name;
                        targetPath = driverDir + "\\" + file.source + "\\" + file.srcname;
                        if (!copyFile(filePath, targetPath, file.name, overWrite)) return false;
                    }
                    //Copy the catalog files to the directory
                    if (driver.catFile.Length > 0)
                    {
                        //Check if the driver manufacturer happened to "leave out" the cat file name...
                        if (driver.catFileName == null || driver.catFileName.Length == 0)
                        {
                            driver.catFileName = driver.catFile.Substring(driver.catFile.LastIndexOf('\\') + 1);
                        }
                        if (!copyFile(driver.catFile, driverDir + "\\" + driver.catFileName, driver.catFileName, overWrite)) return false;
                    }
                }

            }
            return true;
        }

        public bool createFolder (string folder)
        {
            folder = folder.Replace(":","");
            string[] folderSplit = folder.Split('\\');
            foreach (string foldername in folderSplit)
            {
                if (!Directory.Exists(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                    }
                    catch
                    {
                        Console.Out.WriteLine("FATAL: Failed to create folder, disk full?");
                        return false;
                    }
                }
            }
            return true;
        }
        
        public bool copyFile(string sourceFile, string destinationFile, string fileName, bool overWrite)
        {
            try
            {
                if (File.Exists(sourceFile))
                {
                    if (!File.Exists(destinationFile))
                    {
                        File.Copy(sourceFile, destinationFile);
                        Console.Out.WriteLine("File: " + fileName + " copied..");
                        return true;
                    }
                    else Console.Out.WriteLine("File: " + fileName + " not copied.. (the file is already in place)");
                    return true;
                }
                else
                {
                    Console.Out.Write("File: " + fileName + " not found, searching... ");
                    string searchFile = null;
                    searchFile = findFile(getDirId(10), fileName);
                    if (searchFile == null){
                        Console.Out.Write("Still searching... ");
                        searchFile = findFile(getDirId(24), fileName);
                    }
                    if (searchFile != null)
                    {
                        Console.Out.WriteLine("Found!");
                        copyFile(searchFile, destinationFile, fileName, overWrite);
                    }
                    else Console.Out.WriteLine("\nFile: " + fileName + " not copied.. (source file not found)");
                    return true;
                } 
            }
            catch
            {
                Console.Out.WriteLine("FATAL: " + fileName + " not copied.. (file copy error, disk full?)");
                return false;
            }
        }
        public string findFile(string dir, string file)
        {
            string foundFile = null;
            try
            {
                foreach (string f in Directory.GetFiles(dir, file))
                {
                    return dir + "\\" + file;
                }
                foreach (string d in Directory.GetDirectories(dir + "\\"))
                {
                    try
                    {
                        foreach (string f in Directory.GetFiles(d, file))
                        {
                            return d.ToString() + "\\" + file;
                        }
                        string srchFile = findFile(d, file);
                        if (srchFile != null) foundFile = srchFile;
                    }
                    catch { }
                }
            }
            catch (IOException e)
            {
                Console.Out.WriteLine(e);
                return null;
            }
            return foundFile;
        }
    }
}
