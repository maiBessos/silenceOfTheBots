using System.Collections.Generic;
using GoE.AppConstants.GameProcess;
using System;
using GoE.UI;
using System.Threading.Tasks;
using System.Threading;
using GoE.Utils.Extensions;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace GoE.Policies
{
    public class GameResult : Dictionary<string, string>
    {
        /// <summary>
        /// base contains actual data of the getters and additional fields, according to the specific policies
        /// </summary>
        public GameResult(float UtilityPerEvader = float.PositiveInfinity, int CapturedEvaders = 0)
        {
            this.utilityPerEvader = UtilityPerEvader;
            this.capturedEvaders = CapturedEvaders;
        }

        public float utilityPerEvader
        {
            get
            {
                return float.Parse(this[OutputFields.PRESENTABLE_UTILITY]);
            }
            set
            {
                this[OutputFields.PRESENTABLE_UTILITY] = value.ToString();
            }
        }

        /// <summary>
        /// The evaders may decide to stop the game since they don't have enough evaders to continue
        /// the normal strategy. in this case, we might want to evaluate only leaked data *per captured evader*
        /// </summary>
        public float capturedEvaders
        {
            get
            {
                return float.Parse(this[OutputFields.CAPTURED_EVES]);
            }
            set
            {
                this[OutputFields.CAPTURED_EVES] = value.ToString();
            }
        }
    }
    public class ProcessGameResult : GameResult
    {
        // we need explicitly parameterless ctor so it can be used as generic type
        public ProcessGameResult()
            : this(float.PositiveInfinity, 0)
        {

        }

        public ProcessGameResult(float UtilityPerEvader,
                                 int capturedEvaders,
                                 int RoundCount = -1,
                                 int ProcessLengthMS = -1)
            : base(UtilityPerEvader, capturedEvaders)
        {
            this.roundCount = RoundCount;
            this.processLengthMS = ProcessLengthMS;
        }

        public int roundCount
        {
            get
            {
                return Int32.Parse(this[OutputFields.ROUNDS_COUNT]);
            }
            set
            {
                this[OutputFields.ROUNDS_COUNT] = value.ToString();
            }
        }
        public int processLengthMS
        {
            get
            {
                return Int32.Parse(this[OutputFields.PROCESS_TIME_MS]);
            }
            set
            {
                this[OutputFields.PROCESS_TIME_MS] = value.ToString();
            }
        }
    }

    
}