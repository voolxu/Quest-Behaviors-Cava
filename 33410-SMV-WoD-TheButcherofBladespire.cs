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
namespace Honorbuddy.Quest_Behaviors.Cava.TheButcherofBladespire
{
    [CustomBehaviorFileName(@"Cava\33410-SMV-WoD-TheButcherofBladespire")]
    // ReSharper disable once UnusedMember.Global
    public class TheButcherofBladespire : CustomForcedBehavior
    {
        public TheButcherofBladespire(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 33410;
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
        private readonly WoWPoint BossLoc = new WoWPoint(6664.831, 5774.746, 318.8174);
        public WoWUnit DoroggtheRuthless { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 74254 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit Whirling { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 80417 && u.IsAlive && DoroggtheRuthless != null &&
            u.Location.Distance(DoroggtheRuthless.Location) <= 13 && u.Location.Distance(Me.Location) <= 13).OrderBy(u => u.Distance).FirstOrDefault(); } }
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
                if (DoroggtheRuthless == null)
                {
                    QBCLog.Info("Moving to Boss Location");
                    Navigator.MoveTo(BossLoc);
				    return false;
			    }

                // move to Whirling side
                if (Whirling != null)
			    {
                    QBCLog.Info("Moving to Whirling side");
                    Navigator.MoveTo(Whirling.Location.RayCast(Whirling.Rotation + WoWMathHelper.DegreesToRadians(90), 10f));
				    return true;
			    }
                // kill boss
                if (Whirling == null && DoroggtheRuthless != null)
                {
                    if (DoroggtheRuthless != null && Me.CurrentTarget != DoroggtheRuthless)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(DoroggtheRuthless, PoiType.Kill);
                        DoroggtheRuthless.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(DoroggtheRuthless, PoiType.Kill);
                        DoroggtheRuthless.Target();
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