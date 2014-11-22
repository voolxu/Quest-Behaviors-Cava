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
    [CustomBehaviorFileName(@"Cava\33256-SMV-WoD-TheDefenseofKarabor")]
    // ReSharper disable once UnusedMember.Global
    public class TheDefenseofKarabor : CustomForcedBehavior
    {
        public TheDefenseofKarabor(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 33256;
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
        private readonly WoWPoint BossLoc = new WoWPoint(851.7448, -2880.493, 100.4525);
		private WoWUnit _targetPoiUnit;
        public WoWUnit ArnokktheBurner { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 75358 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }


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
                if (!CheckForcenemies() && ArnokktheBurner == null)
                {
                    QBCLog.Info("Moving to Boss Location");
                    Navigator.MoveTo(BossLoc);
                    return false;
                }

                // move to Behind Boss
                //fiery blast 151732
                if (ArnokktheBurner != null && ArnokktheBurner.IsCasting)
                {
                    if (ArnokktheBurner.CastingSpellId == 151731 || ArnokktheBurner.CastingSpellId == 151732 || ArnokktheBurner.CastingSpellId == 149378 || ArnokktheBurner.CastingSpellId == 151766)
                    {
                        QBCLog.Info("Moving Behind boss");
                        Navigator.MoveTo(
                        ArnokktheBurner.Location.RayCast(
                        ArnokktheBurner.Rotation + WoWMathHelper.DegreesToRadians(150), 8f));
                        return true;
                    }
                    return false;
                }
            
                //kill adds
                if (CheckForcenemies())
                {
                    if (Me.CurrentTarget == ArnokktheBurner)
                        Blacklist.Add(ArnokktheBurner, BlacklistFlags.Combat, TimeSpan.FromSeconds(60000), "Priority to Adds");
                    ChooseBestTarget();
                    return false;
                }

                // kill boss
                if (!CheckForcenemies() && ArnokktheBurner != null)
                {
                    Blacklist.Clear(blacklistEntry => blacklistEntry.Entry == 75358);
                    if (ArnokktheBurner != null && Me.CurrentTarget != ArnokktheBurner)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(ArnokktheBurner, PoiType.Kill);
                        ArnokktheBurner.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Boss");
                        BotPoi.Current = new BotPoi(ArnokktheBurner, PoiType.Kill);
                        ArnokktheBurner.Target();
                    }
                    return false;
                }
                return true;
            }
            return false;
        }

		private void ChooseBestTarget()
		{
            _targetPoiUnit = ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                .Where(u => u.Entry != 75358 && u.IsValid && u.IsHostile && u.IsAlive && u.Aggro && u.Location.Distance(Me.Location) <= 40).OrderBy(u => u.Distance * (u.Elite ? 100 : 1)).FirstOrDefault();
			if (_targetPoiUnit != null && Me.CurrentTarget != _targetPoiUnit)
			{
                QBCLog.Info("Selecting new target: {0}", _targetPoiUnit.SafeName);
				BotPoi.Current = new BotPoi(_targetPoiUnit, PoiType.Kill);
				_targetPoiUnit.Target();
			}
		}

        private bool CheckForcenemies()
        {
            if (ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => u.Entry != 75358 && u.IsValid && u.IsHostile && u.IsAlive && u.Aggro && u.Location.Distance(Me.Location) <= 40))
            {
                return true;
            }
            return false;
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || Bots.DungeonBuddy.Helpers.ScriptHelpers.CurrentScenarioInfo.CurrentStage.StageNumber == 2
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