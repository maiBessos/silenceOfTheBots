
using GoE.AppConstants.Policies;
using GoE.GameLogic;
using System;
using System.Collections.Generic;

/// <summary>
/// allows accessing PatrolAndPursuit's data through a APursuersLearner interface
/// </summary>
class PatrolAndPursuitLearner : APursuersLearner
{
    public override void init(PursuersLearnerInitData d)
    {
        if (d.p.GetType() != typeof(GoE.Policies.PatrolAndPursuit))
            throw new Exception("PatrolAndPursuitLearner can only learn PatrolAndPursuit policy");
        
        GoE.Policies.PatrolAndPursuit p = (GoE.Policies.PatrolAndPursuit)d.p;
        AreaPatrolCaptureProbability = p.AreaPatrolCaptureProbability;
        CircumferencePatrolCaptureProbability = p.CircumferencePatrolCaptureProbability;
        PursuitCaptureProbability = p.PursuitCaptureProbability;
    }

    /// <summary>
    /// may be queried after init()
    /// </summary>
    public double AreaPatrolCaptureProbability { get; private set; }

    /// <summary>
    /// may be queried after init()
    /// </summary>
    public double CircumferencePatrolCaptureProbability { get; private set; }

    /// <summary>
    /// may be queried after init()
    /// </summary>
    public double PursuitCaptureProbability { get; private set; }
    
    public void update(System.Collections.Generic.HashSet<System.Drawing.Point> O_p) {}
}