using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using InfParser;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace drvbckp
{
    public class getDrvFiles
    {
        string infDir;

        private string[] getCopyFilesSections(infSection installSection)
        {
            List<string> outputList = new List<string>();
            foreach (infLine instDirective in installSection.lines)
            {
                if (instDirective.key.ToLower() == "copyfiles")
                {
                    outputList.AddRange(instDirective.values);
                }
            }
            return outputList.ToArray();
        }
          
        private List<file> getFilesTargets(infSection destinationDirs, string[] fileSections, infFile infFile)
        {
            List<file> fileList = new List<file>();
            infLine defaultDestDir = destinationDirs.getLine("DefaultDestDir");
            foreach (string fileSection in fileSections)
            {
                infSection section = new infSection();
                if (fileSection.IndexOf('@') == 0)
                {
                    infLine singleFile = new infLine();
                    singleFile.values = new List<string>();
                    singleFile.values.Add(fileSection.Substring(1));
                    section.lines.Add(singleFile);
                }
                else
                {
                    section = infFile.getSection(fileSection);
                }
                infLine destinationDir = new infLine();
                if ((destinationDir = destinationDirs.getLine(fileSection)) == null) destinationDir = defaultDestDir;
                foreach (infLine file in section.lines)
                {
                    if (file.values.Count > 0 && file.values[0].Length > 0)
                    {
                        file newFile = new file();
                        newFile.name = file.values[0];
                        if (file.values.Count > 1 && file.values[1].Length > 0)
                        {
                            newFile.srcname = file.values[1];
                        }
                        else newFile.srcname = file.values[0];

                        if (destinationDir.values != null)
                        {
                            if (destinationDir.values.Count > 0) newFile.targetid = destinationDir.values[0];
                            if (destinationDir.values.Count > 1) newFile.targetpath = destinationDir.values[1];
                        }
                        else if (defaultDestDir.values != null && defaultDestDir.values.Count > 0)
                        {
                            newFile.targetid = defaultDestDir.values[0];
                        }
                        fileList.Add(newFile);
                    }
                }
            }
            return fileList;
        }

        private List<file> getFilesSources(List<file> fileList, infSection sourceDisksFiles, infSection sourceDisks)
        {
            foreach (file file in fileList)
            {
                foreach (infLine sourceDisksFile in sourceDisksFiles.lines)
                {
                    if (file.name.ToLower() == sourceDisksFile.key.ToLower()&& sourceDisksFile.values.Count > 0)
                    {
                        file.source = sourceDisksFile.values[0];
                    }
                }
            }
            return fileList;
        }

        //Get the catalog filename from the versionsection
        private string getCatFile(infSection versionSection)
        {
            infLine catLine = new infLine();
            if ((catLine = versionSection.getLine("CatalogFile")) != null && catLine.values.Count > 0) return catLine.values[0];
            else return null;
        }

        private infSection matchSourceDisksFiles(infSection sourceDisksFiles, infSection sourceDisks)
        {
            infSection output = new infSection();
            //Check for duplicate sourceDisks
            infSection dupeList = new infSection();
            foreach (infLine sourceDisk in sourceDisks.lines)
            {
                bool isDupe = false;
                foreach (infLine dupe in dupeList.lines)
                {
                    if (sourceDisk.key == dupe.key) isDupe = true;
                }
                if (!isDupe) dupeList.lines.Add(sourceDisk);
            }
            sourceDisks = dupeList;

            foreach (infLine sourceDisksFile in sourceDisksFiles.lines)
            {
                infLine file = new infLine();
                file.key = sourceDisksFile.key;
                file.values = new List<string>();
                foreach (infLine sourceDisk in sourceDisks.lines)
                {
                    if (sourceDisksFile.values.Count > 0 && sourceDisk.key.ToLower() == sourceDisksFile.values[0].ToLower())
                    {
                        if (sourceDisk.values.Count > 3)
                        {
                            if (sourceDisksFile.values.Count > 1)
                            {
                                file.values.Add(sourceDisk.values[3] + "\\" + sourceDisksFile.values[1]);
                            }
                            else
                            {
                                file.values.Add(sourceDisk.values[3]);
                            }
                        }
                        else if (sourceDisksFile.values.Count > 1)
                        {
                            file.values.Add(sourceDisksFile.values[1]);
                        }
                    }
                }
                output.lines.Add(file);
            }
            return output;
        }

        private List<string> getSourceDisksPaths(infSection sourceDisks)
        {
            List<string> paths = new List<string>();
            foreach (infLine sourceDisk in sourceDisks.lines)
            {
                if (sourceDisk.values.Count > 3)
                {
                    paths.Add(sourceDisk.values[3]);
                }
            }
            return paths;
        }

        private infSection chooseOSVersion(string sectionName, infFile file)
        {
            string Architecture = null;
            OperatingSystem os = System.Environment.OSVersion;
            string majorVersion = os.Version.Major.ToString();
            string minorVersion = os.Version.Minor.ToString();
            bool NT = false;
            RegistryKey deviceKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Environment");
            try
            {
                Architecture = deviceKey.GetValue("PROCESSOR_ARCHITECTURE").ToString();
                if (deviceKey.GetValue("PROCESSOR_ARCHITECTURE").ToString() == "WINDOWS_NT") NT = true;
            }
            catch { }
            infSection retSection;
            Regex search = new Regex("^" + Regex.Escape(sectionName) + @"(|.nt\]|.ntx86\]|.ntia64\]|.ntamd64\])", RegexOptions.IgnoreCase);
            if (NT) 
            {
                retSection = file.searchSection(search);
            }
            else
            {

            }
            return retSection;
        }

        public getDrvFiles()
        {
            infDir = Environment.GetEnvironmentVariable("SystemRoot") + "\\inf"; //inf file dir            
        }

        public ArrayList drvList(ArrayList deviceList, bool debug)
        {
            ArrayList driverList = new ArrayList();
            
            /* Get the system architecture and OS */
            string Architecture = null;
            bool NT = false;
            RegistryKey deviceKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Environment");
            try
            {
                Architecture = deviceKey.GetValue("PROCESSOR_ARCHITECTURE").ToString();
                if (deviceKey.GetValue("PROCESSOR_ARCHITECTURE").ToString() == "WINDOWS_NT") NT = true;
            }
            catch
            {
            }
            
            //Build a list with all the driver files based on our device list
            foreach (device dev in deviceList)
            {
                string infFile = infDir + "\\" + dev.infPath;
                Parser parser = new Parser();
                if (debug) Console.Out.WriteLine("DEBUG: Parsing inf file: " + infFile + " for device: " + dev.description);
                infFile file = parser.readInf(infFile);
                if (file == null)
                {
                    Console.Out.WriteLine("Driver for: " + dev.description.Substring(0, 18) + "... could not be extracted, inf file unavailable!");
                }
                else
                {
                    infSection installSection = file.getSection(dev.infSection); //FIXME
                    infSection versionSection = file.getSection("Version");
                    infSection destinationDirs = file.getSection("DestinationDirs");
                    
                    /* Get the sourcedisks section(s) */
                    infSection sourceDisks = file.getSection("SourceDisksNames");
                    infSection sourceDisksArch = file.getSection("SourceDisksNames." + Architecture);
                    if (sourceDisks == null) sourceDisks = sourceDisksArch;
                    else if (sourceDisksArch != null) sourceDisks.lines.AddRange(sourceDisksArch.lines);
                    if (sourceDisks == null) goto error;

                    /* Get the sourcedisksfiles section(s) */
                    infSection sourceDisksFiles = file.getSection("SourceDisksFiles");
                    infSection sourceDisksFilesArch = file.getSection("SourceDisksFiles." + Architecture);
                    if (sourceDisksFiles == null) sourceDisksFiles = sourceDisksFilesArch;
                    else if (sourceDisksFilesArch != null) sourceDisksFiles.lines.AddRange(sourceDisksFilesArch.lines);
                    if (sourceDisksFiles == null) goto error;

                    sourceDisksFiles = matchSourceDisksFiles(sourceDisksFiles, sourceDisks);
                    string[] fileSections = getCopyFilesSections(installSection);
                    List<file> fileList = getFilesTargets(destinationDirs, fileSections, file);
                    fileList = getFilesSources(fileList, sourceDisksFiles, sourceDisks);
                    string catalogFile = getCatFile(versionSection);
                    List<string> sourceDisksPaths = getSourceDisksPaths(sourceDisks);
                    driver deviceDriver = new driver(fileList, sourceDisksPaths, catalogFile, "", dev.description, dev.version, dev.infPath, dev.infSection, dev.id, dev.provider);
                    driverList.Add(deviceDriver);
                error:
                    System.Console.WriteLine("Error parsing file");
                }
            }
            return driverList;
        }
    }
}
