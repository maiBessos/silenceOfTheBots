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
    public static class FileUtils
    {
        /// <summary>
        /// replaces backslashes with slashes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string makeSlash(string path)
        {
            return path.Replace('\\', '/');
        }
        private static string addPrefixSlashIfMissing(string val)
        {
            if (val.Count() == 0 || val[0] != '/')
                return '/' + val;
            return val;
        }
        private static string fixDoubleSlashes(string val)
        {
            string fixedVal = val;
            do
            {
                val = fixedVal;
                fixedVal = val.Replace("//", "/");
            }
            while (fixedVal != val);
            return fixedVal;
        }
        private static string getPath(string prefix, string postfix)
        {
            return fixDoubleSlashes(makeSlash(prefix + addPrefixSlashIfMissing(postfix)));
        }
        private static string removeParenthesis(string val)
        {
            if (val[0] == '"')
                val = val.Substring(1);
            if (val[val.Count() - 1] == '"')
                val = val.Substring(0, val.Count() - 2);
            return val;
        }

        /// <summary>
        /// since linux and windows have slightly different conventions when it comes to relative paths, handling ""
        /// etc. , this tries to find a file that exists (File.Exist) by referring 
        /// the given path after "" are removed (if found), and by trying to concatenate exe's working directory
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns> empty string if nothing works
        /// </returns>
        private static string findSatisfyingString(string filepath, Func<string,bool> testSat)
        {
            filepath = removeParenthesis(filepath);

            if (testSat(filepath))
                return filepath;

            if (testSat(getPath(Directory.GetCurrentDirectory(), filepath)))
                return getPath(Directory.GetCurrentDirectory(), filepath);

            if (testSat(getPath(Application.StartupPath, filepath)))
                return getPath(Application.StartupPath, filepath);

            if (testSat(getPath("./", filepath)))
                return getPath("./", filepath);

            return "";
        }
        
        /// <summary>
        /// attempts to fix the path
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>
        /// fixed string for which File.Exists works, or empty string
        /// </returns>
        public static string TryFindingFile(string filepath)
        {
            return findSatisfyingString(filepath, File.Exists);
        }
        /// <summary>
        /// attempts to fix the path
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>
        /// fixed string for which Directory.Exists works, or empty string
        /// </returns>
        public static string TryFindingFolder(string folderPath)
        {
            string tmp = findSatisfyingString(folderPath, Directory.Exists);
            if (tmp != "")
                return tmp;

            char lastChar = folderPath[folderPath.Count() - 1];
            if(lastChar == '\\' || lastChar == '/') // in case folder wasn't found due to the slash prefix
                return findSatisfyingString(folderPath.Substring(0,folderPath.Count()-2), Directory.Exists);

            return "";
        }

        /// <summary>
        /// given absolute/relative filename of a file that still doesn't exists, this separates folder name
        /// and finds an existing legal folder path that matches it, then fixes the entire path so the file could
        /// be written in the new path
        /// </summary>
        /// <param name="fileout"></param>
        /// <returns></returns>
        public static string tryFindingWritableFile(string fileout)
        {
            fileout = fixDoubleSlashes(makeSlash(fileout));
            
            string folder, file;
            FileUtils.getFolderOfFile(fileout, out folder, out file);

            if (!string.IsNullOrWhiteSpace(folder) && folder.Count() > 0)
                folder = FileUtils.TryFindingFolder(folder);
            else
                folder = ".";
            return FileUtils.concatenateFullFilePath(folder, file);
        }
        public static void writeTable(List<Dictionary<string,string>> vals, string fileout)
        {
            File.WriteAllLines(tryFindingWritableFile(fileout), ParsingUtils.ToTable(vals).ToArray());
        }

        public static void appendTable(List<Dictionary<string, string>> vals, string fileout)
        {
            File.AppendAllLines(tryFindingWritableFile(fileout), ParsingUtils.ToTable(vals).ToArray());
        }

        public static string concatenateFullFilePath(string folder, string file)
        {
            return fixDoubleSlashes(makeSlash(folder + addPrefixSlashIfMissing(file)));
        }
        /// <summary>
        /// returns 'folderPath = fullpath' if it's already a folder path.
        /// otherwise, just removes the suffix, until the first '/' or '\' (filename is set to the removed suffix)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static void getFolderOfFile(string fullPath, out string folderPath, out string filename)
        {
            if (TryFindingFolder(fullPath) != "")
            {
                folderPath = fullPath;
                filename = "";
                return;
            }
            //string tmp = Path.GetDirectoryName(filename);
            //if (tmp != null && tmp != "")
            //    return tmp;
            //tmp = new FileInfo(path).Directory.FullName
            int i1 = fullPath.LastIndexOf('\\');
            int i2 = fullPath.LastIndexOf('/');
            if (i1 > i2)
            {
                folderPath = fullPath.Substring(0, i1);
                filename = fullPath.Substring(i1 + 1);
                return;
            }
            else if (i2 > i1)
            {
                folderPath = fullPath.Substring(0, i2);
                filename = fullPath.Substring(i2 + 1);
                return;
            }

            // if both i1 and i2 are -1 , this means we got a relative filename
            folderPath = Directory.GetCurrentDirectory();
            filename = fullPath;
            return;
        }

        public static List<Dictionary<string, string>> readTable(string filein)
        {
            string[] tableLines = File.ReadAllLines(FileUtils.TryFindingFile(filein));
            return ParsingUtils.readTable(tableLines);
        }
        
        public static Dictionary<string,string> readValueMap(string filein)
        {
            string[] data = File.ReadAllLines(FileUtils.TryFindingFile(filein));
            data = ParsingUtils.clearComments(data);
            return ParsingUtils.parseValueMap(data);
        }

        // replaces given key values, but doesn't affect the others
        public static void updateValueMap(Dictionary<string,string> vals, string fileOut)
        {
            Dictionary<string, string> prevVals;
            
            try
            {
                prevVals = readValueMap(fileOut);
            }
            catch (Exception) 
            {
                /* we assume file was not created yet*/
                prevVals = new Dictionary<string, string>();
            }

            Extensions.Dictionaries.AddRange(prevVals, vals);
            writeValueMap(prevVals, fileOut);
        }
        public static bool tryReadValueMapKey(string key, string fileIn, out string value)
        {
            Dictionary<string, string> prevVals = readValueMap(fileIn);
            return prevVals.TryGetValue(key, out value);

        }
        /// <summary>
        /// writes data in readable format, and may later be loaded using readValueMap()
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="fileOut"></param>
        /// <exception cref="">
        /// throws same exceptions as File.WriteAllLines()
        /// </exception>
        public static void writeValueMap(Dictionary<string,string> vals, string fileOut)
        {
            List<string> fileLines = new List<string>();
            foreach (var v in vals)
                fileLines.Add(v.Key + "=" + v.Value);
            File.WriteAllLines(tryFindingWritableFile(fileOut), fileLines.ToArray());
        }

        /// <summary>
        /// makes sure the folder exists, and creates one otherwise
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns>
        /// true if doler now exists, false otherwise
        /// </returns>
        public static bool prepareFolder(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                try
                {
                    Directory.CreateDirectory(folderName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("can't create folder for " + folderName + " files:" + ex.Message);
                    return false;
                }
            }
            return true;
        }
    
       /// <summary>
       /// given a text file filename, this attempts opening it using os's default method
       /// </summary>
       /// <param name="filename"></param>
        public static void openOutputFile(string filename)
        {
            try
            {
                Process.Start(filename);
            }
            catch (Exception)
            {
                MessageBox.Show("results (hopefully) written into " + filename);
            }
        }
    }
}
