using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Extensions;

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly
{
    namespace Utils
    {
        public class RotatingPatrollerBoundPreTransmissionMovement : PreTransmissionMovement
        {
            public RotatingPatrollerBoundPreTransmissionMovement() { }
            public RotatingPatrollerBoundPreTransmissionMovement(float PB, int DB)
            {
                this.PbFactor = PB;
                this.Db = DB;
            }


            public float PbFactor
            /// Pb should be is an int in [1,\frac{n}{2} - 2d_b - 1] - the numerator for the average segments bounds advance each round
            /// PbFactor is in (0,1], and tells Pb
            {
                get;
                protected set;
            }

            public int Db // maximal distance a patroller can have from its nearest bound
            {
                get;
                protected set;
            }
            
            public override bool Equals(object obj)
            {
                RotatingPatrollerBoundPreTransmissionMovement o = (RotatingPatrollerBoundPreTransmissionMovement)obj;
                return PbFactor == o.PbFactor && Db == o.Db;
            }
            public override int GetHashCode()
            {
                return PbFactor.GetHashCode() ^ Db.GetHashCode();
            }
            public override Dictionary<string, string> serialize()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["Pb"] = PbFactor.ToString();
                res["Db"] = Db.ToString();
                return res;
            }

            public int BoundAdvancingLimitationNumerator(IntrusionGameParams gamePrm)
            {
                int d = BoundAdvancingLimitationDenominator(gamePrm);
                // if we can move more than one segment on average(i.e. numerator>denominator), 
                // then t_i is probably so big that there is no limitation
                return Math.Min(d,
                    (int)(PbFactor * (gamePrm.SensitiveAreaPointsCount() / 2 - 2 * Db - 1)));
            }
            public int BoundAdvancingLimitationDenominator(IntrusionGameParams gamePrm)
            {
                return gamePrm.SensitiveAreaPointsCount() - 2 * gamePrm.t_i;
            }
            public bool checkLegalMovementMethod(IntrusionGameParams gamePrm)
            {
                return 0 < BoundAdvancingLimitationNumerator(gamePrm);
            }
            protected override List<PreTransmissionMovement> generateConcreteMovements(int itemsCount, IntrusionGameParams prm)
            {
                HashSet<PreTransmissionMovement> res = new HashSet<PreTransmissionMovement>();
                float sqrtItems = (int)Math.Sqrt(itemsCount);

                int maxPb = prm.SensitiveAreaPointsCount() / 4 - 1;

                for (float i = 0; i < sqrtItems; ++i)
                    for(float j = 0; j < sqrtItems; ++j)
                    {
                        int pb = (int)(1 + maxPb * (j / (sqrtItems - 1)));
                        var m = new RotatingPatrollerBoundPreTransmissionMovement(i / (sqrtItems - 1), pb);
                        if(m.checkLegalMovementMethod(prm))
                            res.Add(m);
                    }

                return res.ToList();
            }
        }

    }
}