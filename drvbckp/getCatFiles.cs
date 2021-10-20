using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace drvbckp
{
    class getCatFiles
    {
        string catRoot = null;
        public getCatFiles()
        {
            catRoot = Environment.GetEnvironmentVariable("SystemRoot") + "\\system32\\CatRoot"; //catroot
        }
        public List<string> catList(ArrayList driverList, bool debug)
        {
            Console.Out.Write("Searching for .cat files... ");
            List<string> catFileList = new List<string>();
            List<string> catFiles = new List<string>();
            try
            {
                catFiles = findFile(catRoot + "\\");
            }
            catch
            {
                Console.Out.WriteLine("Error when searching for cat files, cat files wont be included!");
                return catFileList;
            }
            Console.Out.Write(catFiles.Count() + " found \nIndexing .cat files, please wait...\n" );
            Regex hwidEx = new Regex("[H].[W].[I].[D]");
            Regex hwidChars = new Regex(@"[^A-Za-z0-9&_\\\.]");
            Regex hwidNum = new Regex("HWID[0-9]");
            foreach (string catFile in catFiles)
            {
                if (debug) Console.Out.WriteLine("DEBUG: Parsing catalog file: " + catFile);
                List<string> hwidInCat = new List<string>();
                StreamReader openFile = File.OpenText(catFile);
                string readLine = openFile.ReadLine();
                while (readLine != null)
                {                    
                    if (hwidEx.IsMatch(readLine))
                    {
                        readLine = hwidChars.Replace(readLine, "");
                        readLine = hwidNum.Replace(readLine, "");
                        hwidInCat.Add(readLine);
                        catFileList.Add(readLine);
                        
                    }
                    readLine = openFile.ReadLine();
                }
                foreach (driver driver in driverList)
                {
                    foreach (string hwid in hwidInCat)
                    {
                        string hwidLower = hwid.ToLower();
                        string drvHwidLower = driver.id.ToLower();
                        drvHwidLower = hwidChars.Replace(drvHwidLower, "");
                        if (hwidLower.IndexOf(drvHwidLower) != -1)
                        {
                            driver.catFile = catFile;
                        }
                    }
                }
            }
            return catFileList;
        }


        List<string> filesFound = new List<string>();
        public List<string> findFile(string dir)
        {
            
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    foreach (string f in Directory.GetFiles(d, "*.cat"))
                    {
                        filesFound.Add(f);
                    }
                    findFile(d);
                }
            }
            catch { }
            return filesFound;
        }
    }
}
