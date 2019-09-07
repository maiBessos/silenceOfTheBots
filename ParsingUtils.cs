using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Utils
{
    public static class ParsingUtils
    {
        /// <summary>
        /// parses 'lines' into key-value dictionary.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns>
        /// NULL if there are duplicate keys
        /// key-value pairs otherwise
        /// </returns>
        public static Dictionary<string,string> parseRawParams(string[] lines)
        {
            Dictionary<string, string> processParams = new Dictionary<string, string>();
            List<string> paramLines = new List<string>();
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();

            try
            {
                pairs = getKeyValuePairs(clearComments(lines));
            }
            catch (Exception) { }

            foreach (var p in pairs)
            {
                if (processParams.ContainsKey(p.Item1))
                {
                    processParams = null;
                    return null;
                }
                processParams[p.Item1] = p.Item2;
            }
            return processParams;
        }
        public static string AbbreviateName(string val, int maxLetters = 2, bool onlyAfterDot = true)
        {
            int from = val.LastIndexOf('.');
            if (from > 0)
                val = val.Substring(from);

            string res = "";
            res += val[0];
            for (int i = 1; i < val.Count(); ++i)
                if (char.IsUpper(val[i]))
                    res += val[i];

            if (res.Count() > maxLetters)
                return res.Substring(res.Count() - maxLetters);
            return res;
        }
        /// <summary>
        /// finds CSV values. filters out non numeric values
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<float> extractValues(string str)
        {
            List<string> vals = ParsingUtils.separateCSV(str);
            List<float> valsF = new List<float>();
            foreach (var v in vals)
            {
                string cs = ParsingUtils.filterOutNonNumericValues(v);
                if (cs.Length > 0)
                    valsF.Add(float.Parse(cs));
            }
            return valsF;
        }
        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        /// <summary>
        /// assumes keys are distinct. adds entries for each keys-vals pair in coresponding index
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="keys"></param>
        /// <param name="vals"></param>
        /// <returns></returns>
        public static Dictionary<string, string> makeValueMap<TKey,TValue>(List<TKey> keys, List<TValue> vals)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            for(int i = 0; i < keys.Count; ++i)
            {
                
                res[keys[i].ToString()] = vals[i].ToString();
            }
            return res;
        }
        public static List<Dictionary<string, string>> readTable(string[] tableLines)
        {
            tableLines = ParsingUtils.clearComments(tableLines);

            List<string> headerLine = ParsingUtils.separateCSV(tableLines[0]);
            List<Dictionary<string, string>> res = new List<Dictionary<string, string>>();
            for (int l = 1; l < tableLines.Count(); ++l)
            {
                Dictionary<string, string> lineVals = new Dictionary<string, string>();
                List<string> vals = ParsingUtils.separateCSV(tableLines[l]);
                for (int vi = 0; vi < vals.Count; ++vi)
                {
                    lineVals[headerLine[vi].Trim()] = vals[vi].Trim();
                }
                res.Add(lineVals);
            }
            return res;
        }
        static List<string> LineSeparators
        {
            get { return new List<string>(new string[] { Environment.NewLine, "\n" }); }
        }
        static char KeyValueSeparator
        {
            get { return '='; }
        }
        /// <summary>
        /// attempts reading the value in 'valMap' under the key 'key'. If present, val will be assigned with it.
        /// Otherwise, it will be assigned with defaultVal
        /// </summary>
        /// <param name="valMap"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns>
        /// true iff key was in valMap
        /// </returns>
        public static string readValueOrDefault(Dictionary<string, string> valMap, string key, string defaultVal)
        {
            if (valMap != null && valMap.ContainsKey(key))
                return valMap[key];
            return defaultVal;
        }

        /// <summary>
        /// removes any char that is not numeric (dots between digits remain)
        /// </summary>
        /// <param name="vals"></param>
        /// <returns></returns>
        public static string filterOutNonNumericValues(string vals)
        {
            string res = "";
            bool isPrevDigit = false, isNextDigit;
            for (int i = 0; i < vals.Length; ++i)
            {
                isNextDigit = (i < vals.Length - 1) && char.IsDigit(vals[i+1]);
                if(vals[i] =='-' && isNextDigit)
                {
                    res += vals[i];
                    isPrevDigit = false;
                    continue;
                }
                if (isNextDigit && isPrevDigit && vals[i] == '.')
                {
                    res += vals[i];
                    isPrevDigit = false;
                    continue;
                }
                if (char.IsDigit(vals[i]))
                {
                    res += vals[i];
                    isPrevDigit = true;
                    continue;
                }
                isPrevDigit = false;
            }
            return res;
        }
        /// <summary>
        /// removes empty strings (or only newlines), and strings that start with the char '#', and returns the rest
        /// </summary>
        /// <returns></returns>
        public static string[] clearComments(string[] lines)
        {
            List<string> res = new List<string>();
            foreach (string s in lines)
            {
                if (isComment(s))
                    continue; // skip the line
                res.Add(s);
            }
            return res.ToArray();
        }
        public static bool isComment(string line)
        {
            line = clearLineSeparators(line);
            return (line.Length == 0 || line[0] == '#');
        }
        /// <summary>
        /// spearates lines into key-value pairs, where the key is the string that comes before the first '=' char, and the rest is value
        /// </summary>
        /// <param name="clearLines">
        /// lines with no comments (as returned from clearComments() )
        /// <\param name="clearLines">
        /// <returns></returns>
        public static Dictionary<string, string> parseValueMap(string[] clearLines)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            var pairs = getKeyValuePairs(clearLines);
            foreach (var p in pairs)
                res[p.Item1] = p.Item2;
            return res;
        }

        /// <summary>
        /// if line includes a line separator (\r\n, \n etc.), the result string will exlude them
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string clearLineSeparators(string line)
        {
            string res = line;
            foreach (string s in LineSeparators)
                res = res.Replace(s, "");
            return res;
        }
        /// <summary>
        /// similar to parseValueMap(), but doesn't assume each key is unique
        /// </summary>
        /// <param name="clearLines"></param>
        public static List<Tuple<string, string>> getKeyValuePairs(string[] clearLines)
        {
            List<Tuple<string, string>> keyValuePairs = new List<Tuple<string, string>>();
            foreach (string s in clearLines)
            {
                var words = s.Split(new char[] { KeyValueSeparator });
                string keyw = words[0];
                keyValuePairs.Add(Tuple.Create(keyw, s.Substring(words[0].Length + 1)));
            }
            return keyValuePairs;
        }

        public static string serializeKeyValue(string key, string value)
        {
            return key + KeyValueSeparator + value; 
        }

        public static string makeLine(IEnumerable<string> vals, string separator)
        {
            string res = "";
            foreach (var v in vals)
                res += v+ separator;

            if (separator.Length > 0)
                res = res.Remove(res.Length - separator.Length);
            return res;
        }
        public static string makeLine<T>(IEnumerable<T> vals, string separator)
        {
            string res = "";
            foreach (var v in vals)
                res += v.ToString() + separator;

            if(separator.Length > 0)
                res.Remove(res.Length - separator.Length);
            return res;
        }
        /// <summary>
        /// generates a readable and parsable(using separateCSV() ) string
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="minimalValueLength">
        /// if value is shorter than this length, white space is added
        /// </param>
        /// <param name="addTabSuffix">
        /// if true, after each value (and after the ',') a tab char is added
        /// </param>
        /// <returns></returns>
        public static string makeCSV<T>(IEnumerable<T> vals, int minimalValueLength, bool addTabSuffix) where T : IComparable
        {
            string res = "";
            int sI = 0;
            foreach (var ts in vals)
            {
                string s = ts.ToString();
                string addition = s;
                if (s.Contains(','))
                    addition = '"' + s + '"';
                res += addition;

                sI++;
                if (sI < vals.Count())
                {
                    if (addition.Length + 1 < minimalValueLength) // +1 because we'll also add ',' 
                        res += new string(' ', minimalValueLength - addition.Length - 1);

                    res += ',';

                    if (addTabSuffix)
                        res += '\t';

                }
            }
            return res;
        }

        /// <summary>
        /// finds a term within two 'listmarker's, and creates versions of listValue, each with a different value from the list
        /// </summary>
        /// <param name="listValue"></param>
        /// <param name="listmarker"></param>
        /// <returns></returns>
        public static List<string> splitList(string listValue, string listmarker)
        {
            List<string> res = new List<string>();

            int first = listValue.IndexOf(listmarker);
            if (first == -1)
            {
                res.Add(listValue);
                return res;
            }
            int second = listValue.IndexOf(listmarker, first + 1);

            List<string> vals = separateCSV(listValue.Substring(first + 1, second - first - 1));
            foreach (string v in vals)
                res.Add(listValue.Substring(0, first) + v + listValue.Substring(second + 1));
            return res;
        }

        /// <summary>
        /// separates string by ',' chars. If qoute marks are used (""), then "," within qoutation is not considered a separator
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static List<string> separateCSV(string val)
        {
            List<string> terms = val.Split(new char[] { '"' }).ToList();
            if (terms.Count == 1) // no " found
                return val.Split(new char[] { ',' }).ToList();

            List<string> res = new List<string>();
            List<string> splitTerms = terms[0].Split(new char[] { ',' }).ToList();
            res.AddRange(splitTerms);
            for (int i = 0; i < terms.Count - 1;)
            {
                // every other term is in quotation marks, so we ignore commas it contains
                if (i + 2 < terms.Count)
                {
                    // if we have qoutation, then the last value added to res also needs concatenation fo the string in qoutation,
                    // and the string that comes after that and before next "," separator
                    res[res.Count - 1] = res.Last() + terms[i + 1];

                    i += 2;
                    splitTerms = terms[i].Split(new char[] { ',' }).ToList();
                    res[res.Count - 1] += splitTerms[0];
                    splitTerms.RemoveAt(0); // very inefficient, but meh
                    res.AddRange(splitTerms);
                }
                else
                {
                    if (terms.Count < i + 1)
                        res[res.Count - 1] = res.Last() + terms[i + 1];
                    break;
                }
            }

            return res;
        }

        public static List<string> splitToLines(string multilined, StringSplitOptions opt = StringSplitOptions.RemoveEmptyEntries)
        {
            return new List<string>(multilined.Split(LineSeparators.ToArray(),opt));
        }
        /// <summary>
        /// used to serialize a multi-line text string  (into a single line). May be deserialized using deserializeMultiLineString()
        /// TODO: make sure this works for linux as well
        /// </summary>
        /// <param name="multilined"></param>
        /// <returns></returns>
        public static string serializeMultiLineString(string multilined)
        {
            StringBuilder value = new StringBuilder(multilined);
            string controlStr = "" + AppConstants.ArgEntry.CONTROL_CHAR;

            // control char may appear if other serialization operations were done. duplicate its instances, if any:
            value.Replace(controlStr, controlStr + controlStr); // similarly to how // is /

            // use control char+'n' to mark where newlines should be
            foreach (string newLineChar in LineSeparators )
                value.Replace(newLineChar, controlStr + 'n');

            return value.ToString();
        }

        /// <summary>
        /// creates a text with Environment.Newline for every original newline in the serialized text
        /// </summary>
        /// <param name="serializedMultilined"></param>
        /// <returns></returns>
        public static string deserializeMultiLineString(string serializedMultilined)
        {
            if (serializedMultilined.Length == 0)
                return serializedMultilined;

            string controlStr = "" + AppConstants.ArgEntry.CONTROL_CHAR;
            string res;
            List<string> parts = 
                new List<string>(serializedMultilined.Split(new string[] { controlStr + 'n' },StringSplitOptions.RemoveEmptyEntries));

            res = parts.First();
            for (int pi = 1; pi < parts.Count; ++pi)
            {
                if (parts[pi].Length == 0)
                    continue;
                
                res += Environment.NewLine;
                res += parts[pi];
            }

            
            res = res.Replace(controlStr + controlStr, controlStr); // serialization duplicates control chars that aren't newlines, so we now need to undo this

            //res = parts.First();
            //for (int pi = 1; pi < parts.Count; ++pi)
            //{
            //    if(parts[pi].Length == 0)
            //    {
            //        // illegal char appeared twice in a row - the char was actually part of the text
            //        res += AppConstants.ArgEntry.CONTROL_CHAR; 
            //    }
            //    else if(parts[pi][0] == 'n')
            //    {
            //        res += Environment.NewLine;
            //        res += parts[pi].Substring(1);
            //    }
            //    else
            //    {
            //        throw new Exception("mismatching control char");
            //    }

            //}

            return res;
        }
        /// <summary>
        /// NOTE: white space in values will be disregarded (and removed when calling readTable())
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="fileout"></param>
        public static List<string> ToTable(List<Dictionary<string, string>> vals)
        {
            List<string> tableLines = new List<string>();

            HashSet<string> commonHeadersHash = new HashSet<string>(); // headers that repeat in all lines
            HashSet<string> uncommonHeadersHash = new HashSet<string>();

            // add all headers
            foreach (var d in vals)
            {
                if (d == null)
                    continue;
                foreach (string ks in d.Keys)
                {
                    if (ks == null || ks == "")
                        continue;
                    commonHeadersHash.Add(ks);
                }
            }

            // remove all non common headers:
            foreach (var d in vals)
            {
                HashSet<string> dupeTable = new HashSet<string>(commonHeadersHash);
                foreach (string ks in dupeTable)
                {
                    if (!d.ContainsKey(ks))
                    {
                        commonHeadersHash.Remove(ks);
                        uncommonHeadersHash.Add(ks);
                    }
                }
            }

            // move to lists to make sure the order doesn't change:
            List<string> commonHeaders = new List<string>(commonHeadersHash);
            List<string> uncommonHeaders = new List<string>(uncommonHeadersHash);

            // we want to keep similar distance between columns and values
            int entryLength = 0;
            foreach (var d in vals)
            {
                foreach (var v in commonHeaders)
                {
                    entryLength = Math.Max(entryLength, v.Length);
                    entryLength = Math.Max(entryLength, d[v].Length);
                }

                foreach (var v in uncommonHeaders)
                {
                    entryLength = Math.Max(entryLength, v.Length);
                    if (d.ContainsKey(v))
                        entryLength = Math.Max(entryLength, d[v].Length);
                }
            }
            entryLength++; // insure there is always at least 1 space between entries

            // add column names:
            List<string> columnNames = new List<string>(commonHeaders);
            columnNames.AddRange(uncommonHeaders);
            tableLines.Add(ParsingUtils.makeCSV(columnNames, entryLength, true));

            foreach (var d in vals)
            {
                List<string> rowVals = new List<string>(); // values in d that are under common headers, and then the unique
                foreach (string s in commonHeaders)
                    rowVals.Add(d[s]);
                foreach (string s in uncommonHeaders)
                {
                    if (d.ContainsKey(s))
                        rowVals.Add(d[s]);
                    else
                        rowVals.Add("-1");
                }
                tableLines.Add(ParsingUtils.makeCSV(rowVals, entryLength, true));
            }
            return tableLines;
        }
    }
}
