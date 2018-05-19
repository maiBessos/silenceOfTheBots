using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GoE.Utils;
using GoE.AppConstants;
using GoE.Utils.Extensions;
using System.Threading;
using System.Windows.Forms;

namespace GoE
{
    public static class AppSettings
    {
        private static Action<string> _writeLogLine = null;
        private static bool _isVerboseMode = true;

        private static bool IsVerboseMode { get { return _isVerboseMode; } }

        public static void handleGameException(Exception ex)
        {
            // make sure there are no collisions in report with other processes or past reports
            // if running on CREATE, consider adding output folder
            int counter = 0;
            string filename;

            string allex = Exceptions.getFlatDesc(ex);

            try { File.WriteAllText("CRASH_FS.txt", allex); }
            catch (Exception) { }

            try
            {
                do
                {
                    ++counter;

                    string pid = "pid";
                    string tid = "tid";

                    try{tid = Thread.CurrentThread.ManagedThreadId.ToString();}catch (Exception) { }
                    try { pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString(); } catch (Exception) { }

                    filename = "CRASH_DETAILS_" +
                        tid + "_" +
                        pid + "_" +
                        counter.ToString() + ".txt";
                }
                while (File.Exists(filename));
            
                File.WriteAllText(filename, allex);
            }
            catch (Exception) { }
            
            try
            {
                MessageBox.Show(allex);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// sets the action WriteLogLine()
        /// </summary>
        /// <param name="writeLogLine"></param>
        /// <param name="isVerboseMode">
        /// if false, application runs in verbose move
        /// </param>
        public static void setLogWriteFunc(Action<string> writeLogLine, bool isVerboseMode)
        {
            _isVerboseMode = isVerboseMode;
            _writeLogLine = writeLogLine;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logLine"></param>
        /// <param name="onVerboseOnly">
        /// if true, will only be written when app runs in verbose mode
        /// </param>
        public static void WriteLogLine(string logLine, bool onVerboseOnly = false)
        {
            if(_writeLogLine != null && (!onVerboseOnly || _isVerboseMode))
                _writeLogLine(logLine);
        }

        /// <summary>
        /// saves values in local file, in same folder as application's executable.
        /// NOTE: no exceptions are thrown in case of failure
        /// </summary>
        public static void Save(string key, string value)
        {
            try
            {
                Dictionary<string, string> vals = new Dictionary<string, string>();
                vals[key] = value;
                Utils.FileUtils.updateValueMap(vals, AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// similar to Save(), but saves a line that may have newline chars in it, 
        /// and gets later loaded via LoadMultilineString()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SaveMultilineString(string key, string value)
        {
            Save(key, ParsingUtils.serializeMultiLineString(value));
        }

        /// <summary>
        /// loads a value previously saved by SaveValue() (empty string if not found)
        /// </summary>
        public static string Load(string key)
        {
            // try loading a previously used parameter set
            try
            {
                Dictionary<string, string> vals = 
                    Utils.FileUtils.readValueMap(AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE);
                return vals[key];
            }
            catch (Exception) { }
            return "";
        }

        /// <summary>
        /// /// loads a value previously saved by SaveMultilineString() (empty string if not found)
        /// </summary>
        public static string LoadMultilineString(string key)
        {
            return ParsingUtils.deserializeMultiLineString(Load(key));
        }

        /// <summary>
        /// saves a record to a local file with 'vals', and keys under AppArgumentKeys.DB_Keys
        /// i.e. DB_Keys specifies the keys of the values we attach to each saved record
        /// FIXME: automatic setting of DB_Keys is not implemented. currently using all available in keys policyinput
        /// </summary>
        /// <param name=""></param>
        public static void SaveToDB(Dictionary<string,string> policyInput, List<Dictionary<string, string>> vals)
        {
            try
            {
                List<Dictionary<string, string>> newDBVals = new List<Dictionary<string, string>>();
                Dictionary<string, string> dbKeysRecord = new Dictionary<string, string>();

                //List<string> dbKeys = ParsingUtils.separateCSV(AppArgumentKeys.DB_KEYS.tryRead(policyInput));
                //// for each needed db key, add a column with values coresponding policyInput
                //foreach (var dbk in dbKeys)
                //    dbKeysRecord[dbk] = policyInput[dbk];
                dbKeysRecord = new Dictionary<string, string>(policyInput);

                for (int i = 0; i < vals.Count; ++i)
                {
                    try
                    {
                        Dictionary<string, string> newRecord = new Dictionary<string, string>(dbKeysRecord);
                        newRecord.AddRange(vals[i]);
                        newDBVals.Add(newRecord);
                    }
                    catch (Exception exrec)
                    {
                        #region write error log
                        string partialRecord = "";

                        try
                        {
                            foreach (var v in vals[i])
                            {
                                string vkey = "";
                                string vval = "";
                                if (v.Key != null)
                                    vkey = v.Key;
                                if (v.Value != null)
                                    vval = v.Value;
                                try
                                {
                                    partialRecord += vkey + "=" + vval + ",";
                                }
                                catch (Exception) { }
                            }
                        }
                        catch (Exception) { }

                        try
                        {
                            AppSettings.WriteLogLine("failed saving to db the record:" + partialRecord + "|||"+ Exceptions.getFlatDesc(exrec));
                        }
                        catch (Exception) { }
                        #endregion
                    }
                }

                string dbPath =
                    AppArgumentKeys.DB_KEYS.tryRead(policyInput) + PathLocations.DB_FILE_PREFIX;
                FileUtils.appendTable(newDBVals, dbPath);
            }
            catch(Exception exdb) // try over entire function
            {
                try
                {
                    AppSettings.WriteLogLine("failed saving to db:" + Exceptions.getFlatDesc(exdb));
                }
                catch (Exception) { }
            }
            
        }
        

    }
}