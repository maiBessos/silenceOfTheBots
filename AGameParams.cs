using GoE.Utils;
using System;
using System.Collections.Generic;
using GoE.Utils.Extensions;
using System.IO;

namespace GoE.GameLogic
{
    public interface IGameParams
    {
        void fromValueMap(Dictionary<string, string> vals);
        Dictionary<string, string> toValueMap();
        void serialize(string filename);
        void deserialize(string serializedFile, Dictionary<string, string> overriddenValues);

        string generateFileShowDialog();
    }
    /// <summary>
    /// truly generalizing Game params for different game types is hard,
    /// because the reused functions are all static.
    /// In order to use "abstract static" methods, we need to use C# attributes, and it's a little ugly
    /// </summary>
    public abstract class AGameParams<T> : IGameParams where T : AGameParams<T>, new()
    {
        [ThreadStatic]
        private static T threadParams;

        /// <summary>
        /// replaces this thread's previous params, with a clear instance
        /// </summary>
        /// <returns></returns>
        public static T getClearParams()
        {
             if (threadParams == null)
                threadParams = new T();
             threadParams.initClearParams();
            return threadParams;
        }
        public static T getParamsFromFile(string serializedFile, Dictionary<string, string> overriddenValues)
        {
            if (threadParams == null)
                threadParams = new T();
            threadParams.deserialize(serializedFile, overriddenValues);
            return threadParams;
        }
      
        public virtual string generateFileShowDialog()
        {
            return null; // TODO: some game params don't implement this method
        }
        public virtual void deserialize(string serializedFile, Dictionary<string, string> overriddenValues)
        {
            try
            {
                Dictionary<string, string> vals = new Dictionary<string, string>();

                if (serializedFile != "")
                    vals.AddRange(FileUtils.readValueMap(serializedFile));

                if (overriddenValues != null)
                    vals.AddRange(overriddenValues);

                fromValueMap(vals);
            }
            catch (Exception ex)
            {
                throw new Exception("Cant deserialize a " + typeof(T).Name + " instance. " + ex.Message);
            }
        }
        public virtual void serialize(string filename)
        {
            FileUtils.writeValueMap(toValueMap(), filename);
        }

        /// <summary>
        /// resets the GameParam instance
        /// </summary>
        protected abstract void initClearParams();
        public abstract void fromValueMap(Dictionary<string, string> vals);
        public abstract Dictionary<string, string> toValueMap();
    }
}