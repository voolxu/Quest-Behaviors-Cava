using Buddy.Coroutines;

#region Usings

using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.TheRealPrey
{
    [CustomBehaviorFileName(@"Cava\34348-SMV-WoD-TheRealPrey")]
    // ReSharper disable once UnusedMember.Global
    public class TheRealPrey : CustomForcedBehavior
    {
        public TheRealPrey(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 34348;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private Composite _root;

        // Private properties
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private readonly WoWPoint BossLoc = new WoWPoint(7083.627, 4340.759, 56.55431);
        public WoWUnit GroshtheMighty { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 78230 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
 
     
        
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();

        #region Overrides of CustomForcedBehavior

        protected Composite CreateBehavior_MainCombat()
        {
			return new ActionRunCoroutine(ctx => MainCoroutine());
        }

	    async Task<bool> MainCoroutine()
	    {
			if (!IsDone)
		    {
                // Move To Place
                if (GroshtheMighty == null)
                {
                    QBCLog.Info("Moving to Boss Location");
                    Navigator.MoveTo(BossLoc);
				    return false;
			    }

				// move to Behind Boss
                if (GroshtheMighty != null && GroshtheMighty.IsCasting && (GroshtheMighty.CastingSpellId == 165908 || GroshtheMighty.CastingSpellId == 165985))
			    {
                    QBCLog.Info("Moving Behind boss");
                    Navigator.MoveTo(GroshtheMighty.Location.RayCast(GroshtheMighty.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
				    return true;
			    }
                // moving to apply vial on boss
		        if (GroshtheMighty != null && !GroshtheMighty.HasAura(157954) && GroshtheMighty.Location.Distance(Me.Location) > 30)
		        {
		            QBCLog.Info("Moving near Boss to apply vial");
		            Navigator.MoveTo(GroshtheMighty.Location);
		            return true;
		        }
                // apply vial on boss
                if (GroshtheMighty != null && !GroshtheMighty.HasAura(157954) && GroshtheMighty.Location.Distance(Me.Location) <= 30)
                {
                    QBCLog.Info("Using Vial on Boss");
                    Lua.DoString("RunMacroText('/click ExtraActionButton1','0')");
                    await Coroutine.Sleep(2000);
                    SpellManager.ClickRemoteLocation(GroshtheMighty.Location);
                    return true;
                }


		        // kill boss
                if (GroshtheMighty != null && !GroshtheMighty.IsCasting)
                {
                    if (GroshtheMighty != null && Me.CurrentTarget != GroshtheMighty)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(GroshtheMighty, PoiType.Kill);
                        GroshtheMighty.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(GroshtheMighty, PoiType.Kill);
                        GroshtheMighty.Target();
                    }
                    return false;
                }
                return true;
		    }
		    return false;
	    }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete)
                        || _doingQuestTimer.ElapsedMilliseconds >= 180000);
            }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();

            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_MainCombat());

                this.UpdateGoalText(QuestId);
            }
        }

	    public override void OnFinished()
	    {
            TreeHooks.Instance.RemoveHook("Combat_Main", CreateBehavior_MainCombat());
			TreeRoot.GoalText = string.Empty;
			TreeRoot.StatusText = string.Empty;
		    base.OnFinished();
	    }

	    #endregion
    }
}