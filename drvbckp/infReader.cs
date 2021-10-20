using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;

namespace drvbckp
{
    public class infReader
    {
        public List<directive> getSection(string infFile, Regex targetSection, bool str)
        {
            List<directive> strings = new List<directive>();
            if (!str) strings = getSection(infFile, new Regex(@"\[Strings"), true);
            List<directive> section = new List<directive>();
            StreamReader infReader;
            Regex otherSection = new Regex(@"^\[");
            try
            {
                infReader = File.OpenText(infFile);
            }
            catch {
                return null; 
            }
            string readLine;
            readLine = infReader.ReadLine();
            bool readingSect = false;
            while (readLine != null)
            {
                if (readLine != null && targetSection.IsMatch(readLine))
                {
                    readingSect = true;
                    readLine = infReader.ReadLine();
                }
                else if (readLine != null && otherSection.IsMatch(readLine)) readingSect = false;
                if (readingSect && readLine != null && readLine.Length > 0)
                {
                    directive directive = new directive();
                    List<quote> quotes = getQuotes(ref readLine);
                    readLine = removeComments(quotes, readLine);

                    //See if were dealing with a split line, and if so, pick up the other line(s)
                    readLine = getSplitLine(ref quotes, readLine, infReader);

                    //Replace strings
                    if (!str)
                    {
                        readLine = replaceStrings(ref quotes, strings, readLine);
                    }

                    //Get the directive name
                    int endOfName = getDirectiveName(quotes, ref directive, readLine);

                    //Get the values
                    bool values = getValues(quotes, ref directive, readLine, endOfName);
                    if (directive.values == null) directive.values = new List<string>();
                    if (directive.name != null) section.Add(directive);
                }
                readLine = infReader.ReadLine();
            }
            return section;
        }

        public string replaceStrings(ref List<quote> quotes, List<directive> strings, string readLine)
        {
            int indexOf = readLine.IndexOf('%');
            while (indexOf > -1)
            {
                if (!isInsideQuotes(quotes, indexOf))
                {
                    int indexOfEnd = readLine.IndexOf('%', indexOf + 1);
                    if (indexOfEnd == -1) break;
                    else if (!isInsideQuotes(quotes, indexOfEnd))
                    {
                        string strKey = readLine.Substring(indexOf + 1, indexOfEnd - indexOf - 1);
                        foreach (directive str in strings)
                        {
                            if (str.name.ToLower() == strKey.ToLower())
                            {
                                //Get the length difference to adjust our quotes
                                int lenDiff = indexOfEnd - indexOf - str.values[0].Length;
                                List<quote> tmpQuotes = new List<quote>();
                                foreach (quote quote in quotes)
                                {
                                    if (quote.start >= indexOfEnd)
                                    {
                                        quote tmpQuote = new quote();
                                        tmpQuote.start = quote.start + lenDiff;
                                        tmpQuote.end = quote.end + lenDiff;
                                        tmpQuotes.Add(tmpQuote);
                                    }
                                    else tmpQuotes.Add(quote);
                                }
                                quotes = tmpQuotes;
                                readLine = readLine.Substring(0, indexOf) + str.values[0] + readLine.Substring(indexOfEnd + 1);
                                break;
                            }
                        }
                    }
                }
                indexOf = readLine.IndexOf('%', indexOf + 1);
            }
            return readLine;
        }

        public bool isInsideQuotes(List<quote> quotes, int index)
        {
            foreach (quote quote in quotes)
            {
                if (index >= quote.start && index <= quote.end) return true;
            }
            return false;
        }

        public int getDirectiveName(List<quote> quotes, ref directive directive, string readLine)
        {
            int indexOfEquals = readLine.IndexOf('=');
            while (indexOfEquals > -1)
            {
                if (!isInsideQuotes(quotes, indexOfEquals))
                {
                    directive.name = readLine.Remove(indexOfEquals).Trim();
                    return indexOfEquals + 1;
                }
                indexOfEquals = readLine.IndexOf('=', indexOfEquals + 1);
            }
            directive.name = "";
            return 0;
        }

        public bool getValues(List<quote> quotes, ref directive directive, string readLine, int endOfName)
        {
            if (directive.name != null)
            {
                directive.values = new List<string>();
                int indexOfComma = readLine.IndexOf(',', endOfName);
                int lastEnd = endOfName;
                while (indexOfComma > -1)
                {
                    if (!isInsideQuotes(quotes, indexOfComma))
                    {
                        string value = readLine.Substring(lastEnd, indexOfComma - lastEnd).Trim();
                        directive.values.Add(value);
                    }
                    lastEnd = indexOfComma + 1;
                    indexOfComma = readLine.IndexOf(',', lastEnd);
                }
                directive.values.Add(readLine.Substring(lastEnd).Trim());
                return true;
            }
            return false;
        }

        public string getSplitLine(ref List<quote> quotes, string readLine, StreamReader infReader)
        {
            int indexOfBackslash = readLine.IndexOf('\\');
            while (indexOfBackslash > -1)
            {
                while (!isInsideQuotes(quotes, indexOfBackslash) && indexOfBackslash + 1 == readLine.Length)
                {
                    string readSplitLine = infReader.ReadLine();
                    List<quote> quotesSplitLine = getQuotes(ref readSplitLine);
                    foreach (quote quote in quotesSplitLine)
                    {
                        quote tempQuote = new quote();
                        tempQuote.start = quote.start + readLine.Length;
                        tempQuote.end = quote.end + readLine.Length;
                        quotes.Add(tempQuote);
                    }
                    readLine = readLine.Remove(indexOfBackslash) + removeComments(quotesSplitLine, readSplitLine);

                }
                if (indexOfBackslash + 1 < readLine.Length) indexOfBackslash = readLine.IndexOf('\\', indexOfBackslash + 1);
                else indexOfBackslash = -1;
            }
            return readLine;
        }

        public List<quote> getQuotes(ref string readLine)
        {
            int lastQuote = 0;
            List<quote> quotes = new List<quote>();
            while (readLine.IndexOf("\"\"", lastQuote) > -1)
            {
                quote getQuote = new quote();
                getQuote.start = readLine.IndexOf("\"\"", lastQuote);
                if (getQuote.start > -1)
                {
                    getQuote.end = readLine.IndexOf("\"\"", getQuote.start + 2);
                    if (getQuote.end > -1)
                    {
                        lastQuote = getQuote.end;
                        readLine =
                            readLine.Substring(0, getQuote.start) +
                            readLine.Substring(getQuote.start + 2, getQuote.end - getQuote.start - 2) +
                            readLine.Substring(getQuote.end + 2);
                        getQuote.end -= 3;
                        quotes.Add(getQuote);
                    }
                    else lastQuote = getQuote.start + 2;
                }
                else lastQuote = getQuote.start + 2;
            }
            lastQuote = 0;
            while (readLine.IndexOf('"', lastQuote) > -1)
            {
                quote getQuote = new quote();
                getQuote.start = readLine.IndexOf('"', lastQuote);
                if (getQuote.start > -1 && !isInsideQuotes(quotes, getQuote.start))
                {
                    getQuote.end = readLine.IndexOf('"', getQuote.start + 1);
                    if (getQuote.end > -1)
                    {
                        readLine =
                            readLine.Substring(0, getQuote.start) +
                            readLine.Substring(getQuote.start + 1, getQuote.end - getQuote.start - 1) +
                            readLine.Substring(getQuote.end + 1);
                        getQuote.end -= 2;
                        lastQuote = getQuote.end;
                        quotes.Add(getQuote);
                        int quotesIndex = 0;
                        while (quotes.Count > quotesIndex)
                        {
                            if (quotes[quotesIndex].start > getQuote.end)
                            {
                                quotes[quotesIndex].end -= 2;
                                quotes[quotesIndex].start -= 2;
                            }
                            quotesIndex++;
                        }
                    }
                    else lastQuote = getQuote.start + 1;
                }
                else lastQuote = getQuote.start + 1;
            }
            return quotes;
        }

        public string removeComments(List<quote> quotes, string readLine)
        {
            int indexOfComment = readLine.IndexOf(';');
            while (indexOfComment > -1)
            {
                if (!isInsideQuotes(quotes, indexOfComment))
                {
                    readLine = readLine.Remove(indexOfComment).Trim();
                    break;
                }
                indexOfComment = readLine.IndexOf(';', indexOfComment + 1);
            }
            return readLine;

        }
    }
}
