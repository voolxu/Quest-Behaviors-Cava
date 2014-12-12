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
namespace Honorbuddy.Quest_Behaviors.Cava.ToCaptureGuldan
{
    [CustomBehaviorFileName(@"Cava\34295-FR-WoD-ToCaptureGuldan")]
    // ReSharper disable once UnusedMember.Global
    public class ToCaptureGuldan : CustomForcedBehavior
    {
        public ToCaptureGuldan(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 34295;
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
        private readonly WoWPoint _bossLoc = new WoWPoint(7765.667, 6362.882, 10.29681);

        private static WoWUnit Guldan { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 74749 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }
        private static WoWUnit Giselda { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 75933 && u.IsAlive && u.Elite).OrderBy(u => u.Distance).FirstOrDefault(); } }



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
                if (Guldan == null && Giselda== null)
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
                    QBCLog.Info("Interacting with Gul'dan");
                    WoWMovement.MoveStop();
                    Guldan.Interact();
                    return false;
                }
                //killing Giselda
                if (Guldan != null && Guldan.HasAura(148951) && Giselda != null && Giselda.Attackable)
                {
                    if (Me.CurrentTarget != Giselda)
                    {
                        QBCLog.Info("Killing Giselda");
                        BotPoi.Current = new BotPoi(Giselda, PoiType.Kill);
                        Giselda.Target();
                    }
                    if (BotPoi.Current.Type != PoiType.Kill)
                    {
                        QBCLog.Info("Killing Giselda");
                        BotPoi.Current = new BotPoi(Giselda, PoiType.Kill);
                        Giselda.Target();
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