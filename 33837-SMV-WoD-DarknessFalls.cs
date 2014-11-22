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
namespace Honorbuddy.Quest_Behaviors.Cava.DarknessFalls
{
    [CustomBehaviorFileName(@"Cava\33837-SMV-WoD-DarknessFalls")]
    // ReSharper disable once UnusedMember.Global
    public class DarknessFalls : CustomForcedBehavior
    {
        public DarknessFalls(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 33837;
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
        private readonly WoWPoint BossLoc = new WoWPoint(599.1701, -1209.141, -21.42326);
        public WoWUnit Nerzhul { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 76172 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit RisenSpirit { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 76650 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit VoidHorror { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 76818 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }

        
     
        
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
                if (Nerzhul == null || !Me.Combat)
                {
                    QBCLog.Info("Moving to Boss Location");
                    Navigator.MoveTo(BossLoc);
				    return false;
			    }

				// move to Behind Boss
                if (Nerzhul != null && Nerzhul.IsCasting && Nerzhul.CastingSpellId==154922)
			    {
                    QBCLog.Info("Moving Behind boss");
                    Navigator.MoveTo(Nerzhul.Location.RayCast(Nerzhul.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
				    return true;
			    }
		        //kill Void Horror
                if (VoidHorror != null)
		        {
                    if (Me.CurrentTarget != VoidHorror)
                    {
                        if (Me.CurrentTarget == Nerzhul)
                            Blacklist.Add(Nerzhul, BlacklistFlags.Combat, TimeSpan.FromSeconds(60000), "Priority to Risen Spirit");
                        QBCLog.Info("Killing Void Horror");
                        BotPoi.Current = new BotPoi(VoidHorror, PoiType.Kill);
                        VoidHorror.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Void Horror");
                        BotPoi.Current = new BotPoi(VoidHorror, PoiType.Kill);
                        VoidHorror.Target();
                    }
                    return false;
		        }
                // kill adds
		        if (RisenSpirit != null)
		        {
                    if (Me.CurrentTarget != RisenSpirit)
                    {
                        if (Me.CurrentTarget == Nerzhul)
                            Blacklist.Add(Nerzhul, BlacklistFlags.Combat, TimeSpan.FromSeconds(60000), "Priority to Risen Spirit");
                        QBCLog.Info("Killing Risen Spirit");
                        BotPoi.Current = new BotPoi(RisenSpirit, PoiType.Kill);
                        RisenSpirit.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Risen Spirit");
                        BotPoi.Current = new BotPoi(RisenSpirit, PoiType.Kill);
                        RisenSpirit.Target();
                    }
                    return false;
		        }
                // kill boss
                if (VoidHorror == null && RisenSpirit == null && Nerzhul != null && !Nerzhul.IsCasting && !Nerzhul.HasAura(154805) && Nerzhul.Location.Distance(Me.Location) <= 4)
                {
                    Blacklist.Clear(blacklistEntry => blacklistEntry.Entry == 76172);
                    if (Nerzhul != null && Me.CurrentTarget != Nerzhul)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(Nerzhul, PoiType.Kill);
                        Nerzhul.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(Nerzhul, PoiType.Kill);
                        Nerzhul.Target();
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