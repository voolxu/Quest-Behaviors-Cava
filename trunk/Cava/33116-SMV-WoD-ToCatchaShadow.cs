#region Usings

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
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.ToCatchaShadow
{
    [CustomBehaviorFileName(@"Cava\33116-SMV-WoD-ToCatchaShadow")]
    // ReSharper disable once UnusedMember.Global
    public class ToCatchaShadow : CustomForcedBehavior
    {
        public ToCatchaShadow(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 33116;
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
        // ReSharper disable once UnassignedField.Compiler
        private bool _isBehaviorDone;
        // ReSharper disable once UnusedField.Compiler
        private Composite _root;

        // Private properties
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private readonly WoWPoint _bossLoc = new WoWPoint(1308.831, 1066.35 ,147.6662);

        private static WoWUnit Guldan { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 73857 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
        private static WoWUnit Razuun { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 72793 && u.IsAlive && u.Elite).OrderBy(u => u.Distance).FirstOrDefault(); } }
        private static WoWItem ItemGuldanSoulTrap { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 109246)); } }

        #region Overrides of CustomForcedBehavior

        protected Composite CreateBehavior_MainCombat()
        {
            return new ActionRunCoroutine(ctx => MainCoroutine());
        }

        // ReSharper disable once CSharpWarnings::CS1998
        async Task<bool> MainCoroutine()
        {
            if (!IsDone)
            {
                // Move To Place
                if (Guldan == null && Razuun == null)
                {
                    QBCLog.Info("Moving to Boss Location");
                    Navigator.MoveTo(_bossLoc);
                    return false;
                }
                //start event
                if (Guldan != null && Guldan.HasAura(173746) && Guldan.Location.Distance(Me.Location) > 4)
                {
                    QBCLog.Info("Moving Near Gul'dan");
                    Navigator.MoveTo(Guldan.Location);
                    return false;
                }
                if (Guldan != null && Guldan.HasAura(173746) && Guldan.Location.Distance(Me.Location) <= 4)
                {
                    QBCLog.Info("Using Item Guldan Soul Trap at Gul'dan");
                    WoWMovement.MoveStop();
                    Guldan.Interact();
                    ItemGuldanSoulTrap.Use();
                    return false;
                }
                //killing Razuun
                if (Guldan != null && Guldan.HasAura(148951) && Razuun != null && Razuun.Attackable)
                {
                    if (Me.CurrentTarget != Razuun)
                    {
                        QBCLog.Info("Killing Razuun");
                        BotPoi.Current = new BotPoi(Razuun, PoiType.Kill);
                        Razuun.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Razuun");
                        BotPoi.Current = new BotPoi(Razuun, PoiType.Kill);
                        Razuun.Target();
                    }
                    return false;
                }
                //Ignoring Protected Boss
                if (Guldan != null && Guldan.HasAura(148951) && !Guldan.IsBlacklistedForCombat())
                {
                    Blacklist.Add(Guldan, BlacklistFlags.Combat, TimeSpan.FromSeconds(60000));
                    Me.ClearTarget();
                    return false;
                }
                return false;
            }
            return false;
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || Bots.DungeonBuddy.Helpers.ScriptHelpers.CurrentScenarioInfo.CurrentStage.StageNumber == 2
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

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