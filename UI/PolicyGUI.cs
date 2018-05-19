using GoE.GameLogic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.UI
{
    public enum MarkType
    {
        Pursuer,
        Evader
    }   
    public class InputRequest
    {
        public InputRequest()
        {
            ComboChoices = new Dictionary<IAgent, List<Tuple<string, List<object>, object>>>();
            MovementOptions = new Dictionary<IAgent, Tuple<Location, List<Location>>>();
        }

        
        /// <summary>
        /// marks the option visually, and allows the use to choose
        /// a destination for the evader
        /// </summary>
        /// <param name="mover"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void addMovementOption(IAgent mover, Location from, List<Location> to)
        {
            MovementOptions[mover] = Tuple.Create(from, to);
        }

        /// <returns>
        /// key that later associated an answer to the choice
        /// </returns>
        public int addComboChoice<T>(IAgent chooser, string choiceDescription, IEnumerable<T> choiceOptions, T defaultVal)
        {
            if (!ComboChoices.Keys.Contains(chooser))
                ComboChoices[chooser] = new List<Tuple<string, List<object>, object>>();

            List<object> choiceOptionsObjects = new List<object>();
            foreach (T t in choiceOptions)
                choiceOptionsObjects.Add((object)t);
            ComboChoices[chooser].Add(Tuple.Create(choiceDescription, choiceOptionsObjects, (object)defaultVal));
            return ComboChoices[chooser].Count - 1;
        }

        /// <summary>
        /// ComboChoices[a][i].item1 tells description of question #i regarding agent a, and
        /// item2 tells the actual choices (assumed to implement ToString() )
        /// item3 tells the default value
        /// </summary>
        public Dictionary<IAgent, List<Tuple<string, List<object>, object>>> ComboChoices { get; private set; }
        
        /// <summary>
        /// MovementOptions[a] tells us an agent can move from MovementOptions[a].Item1 to any point in MovementOptions[a].Item2
        /// If the node is currently unset, the option of not changing the node (keeping it unset) exists
        /// </summary>
        public Dictionary<IAgent, Tuple<Location, List<Location>>> MovementOptions { get; private set; }
    }



    public interface IPolicyGUIInputProvider
    {
        // relevant only for GUI mode. If invoked, this tells the GUI to stop skipping rounds, so the same policy that 
        // called debugStopSkippingRound won't be invoked again before the user continues manually
        void debugStopSkippingRound();
        
        ///// <summary>
        ///// asks user for input for fields(listed in text[])
        ///// </summary>
        ///// <param name="text"></param>
        ///// <param name="caption"></param>
        ///// <param name="defaultValues"></param>
        ///// <returns>
        ///// a string per field in text[] (indices of field text and field answer corespond)
        ///// </returns>
        //List<string> ShowDialog(string[] text, string caption, string[] defaultValues);

        /// <summary>
        /// tells the GUI to mark a specific points on the graph, under a certain label
        /// </summary>
        void markLocations(Dictionary<string, List<PointF>> locations);

        /// <summary>
        /// waits until the user provides input according to InputRequest's addMovementOption() and addComboChoice calls
        /// </summary>
        void setInputRequest(InputRequest req);

        /// <returns>
        /// returns default, or one of the options, as specified in previous input Provided in addComboChoice() call (which returned the value of 'choiceKey')
        /// TODO: separate this into another class
        /// </returns>
        object getChoice(IAgent chooser, int choiceKey);

        /// <returns>
        /// returns one of the points, as specified in previous inputProvided.addMovementOption() call
        /// TODO: separate this into another class
        /// </returns>
        List<Location> getMovement(IAgent mover);

        /// <summary>
        /// writes a ling into log (accumulated used for analysing results of policy, when game is done)
        /// </summary>
        void addLogValue(string key, string value);

        /// <summary>
        /// flushes lines previously added in logWriteLine() calls
        /// </summary>
        void flushLog();

        /// <summary>
        /// unlike addLogValue(), this adds details only for the latest round (relevant only if hasBoardGUI() is true)
        /// </summary>
        /// <param name="logLines"></param>
        void addCurrentRoundLog(List<string> logLines);

        bool hasBoardGUI();

    }


    /// <summary>
    /// used for non-gui invocations of the policy.
    /// The implementation will use defaults to answer all of the policy's queries,
    /// and ShowDialog() will be displayed only once if default are null, and the answer will be reused
    /// </summary>
    public class InitOnlyPolicyInput : IPolicyGUIInputProvider
    {
        //private bool throwExceptionIfInputValueMissing;

        private Dictionary<IAgent, List<object>> defaultComboValues = new Dictionary<IAgent, List<object>>();
        private Dictionary<IAgent, List<Location>> defaultMovement = new Dictionary<IAgent, List<Location>>();

        //public Dictionary<string, string> inputForNULLDefaultVals { get; private set; }


        public InitOnlyPolicyInput()
            //bool ThrowExceptionIfInputValueMissing,
            //Dictionary<string, string> defaultValuesForMandatoryInput)
        {
            //this.throwExceptionIfInputValueMissing = ThrowExceptionIfInputValueMissing;
            //inputForNULLDefaultVals = defaultValuesForMandatoryInput;
            LogValues = new Dictionary<string, string>();

        }

        public Dictionary<string, string> LogValues { get; private set; }

        public void debugStopSkippingRound() { }


        public void addLogValue(string key, string value)
        {
            LogValues[key] = value;
        }

        public void flushLog()
        {
            LogValues.Clear();
        }

        //public void setDefaultValue(string key, string val)
        //{
        //    inputForNULLDefaultVals[key] = val;
        //}
        //object multiShowDialogLock = new object();
        //virtual public List<string> ShowDialog(string[] text, string caption, string[] defaultValues)
        //{
        //    if (defaultValues != null)
        //        return new List<string>(defaultValues);

        //    if (inputForNULLDefaultVals.ContainsKey(text[0]))
        //    {
        //        List<string> res = new List<string>();
        //        foreach (string t in text)
        //        {
        //            if (inputForNULLDefaultVals.ContainsKey(t))
        //                res.Add(inputForNULLDefaultVals[t]);
        //            else
        //                res.Add("");
        //        }
        //        return res;
        //    }

        //    if (!throwExceptionIfInputValueMissing)
        //    {
        //        List<string> newVals = InputBox.ShowDialog(text, caption);
        //        for (int i = 0; i < text.Count(); ++i)
        //            inputForNULLDefaultVals[text[i]] = newVals[i];

        //        return newVals;
        //    }
        //    throw new Exception("One of the input values was missing: " + Utils.ParsingUtils.makeCSV(text, 1, false));
        //}

        public void markLocations(Dictionary<string, List<PointF>> locations) { }

        public void setInputRequest(InputRequest req)
        {
            foreach (var i in req.ComboChoices)
            {
                defaultComboValues[i.Key].Clear();
                foreach (var v in i.Value)
                    defaultComboValues[i.Key].Add(v.Item3);
            }
            foreach (var i in req.MovementOptions)
            {
                defaultMovement[i.Key] = new List<Location>();
                defaultMovement[i.Key].Add(i.Value.Item2.First());
            }
        }

        public object getChoice(IAgent chooser, int choiceKey)
        {
            return defaultComboValues[chooser][choiceKey];
        }

        public List<Location> getMovement(IAgent mover)
        {
            return defaultMovement[mover];
        }


        public bool hasBoardGUI() { return false; }

        public void addCurrentRoundLog(List<string> logLines) { }
    }


}
