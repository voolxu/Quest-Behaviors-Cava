
#region Summary and Documentation
#endregion


#region Examples
#endregion

using System.Diagnostics;
using Styx.CommonBot.Routines;
using Styx.Pathing;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.SeedsofDiscord
{
    [CustomBehaviorFileName(@"Cava\25308-MountHyjal-SeedsofDiscord")]
    public class SeedsofDiscord : CustomForcedBehavior
    {
        ~SeedsofDiscord()
        {
            Dispose(false);
        }

        public SeedsofDiscord(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 25308;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
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
        private bool _isDisposed;
        private Composite _root;

        // Private properties
        private static LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
        }
        WoWPoint _outHouseLoc = new WoWPoint(4831.595, -4221.065, 894.0665);
        WoWPoint _runToLoc = new WoWPoint(4807.574, -4182.97, 897.5315);
        private List<WoWUnit> MobKarrgonn { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 40489 && ret.IsAlive && !ret.IsMoving)).OrderBy(ret => ret.Distance).ToList(); } }
        private List<WoWUnit> MobAzennios { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 40491 && ret.IsAlive && ret.IsNeutral)).OrderBy(ret => ret.Distance).ToList(); } }
        private static WoWItem ItemOgreDisguise { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 55137)); } }
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();



        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    TreeHooks.Instance.RemoveHook("Combat_Main", CreateBehavior_MainCombat());
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                // ReSharper disable once CSharpWarnings::CS0618
                base.Dispose();
            }

            _isDisposed = true;
        }


        #region Overrides of CustomForcedBehavior

        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId) || _doingQuestTimer.ElapsedMilliseconds >= 180000 || Me.Combat,
                    new Action(delegate
                    {
                        if (Me.HasAura(75724))
                        {
                            Lua.DoString("CancelUnitBuff('player',GetSpellInfo(75724))");
                        }
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        protected Composite CreateBehavior_MainCombat()
        {
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
                new PrioritySelector(
                    DoneYet,
                    //Get Aura
                    new Decorator(ret => !Me.HasAura(75724),
                        new Sequence(
                            new DecoratorContinue(ret => _outHouseLoc.Distance(Me.Location) > 2,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(_outHouseLoc)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => _outHouseLoc.Distance(Me.Location)  <= 2,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new Decorator(ret => Me.IsShapeshifted(),
                                        new Action(ret => Lua.DoString("CancelShapeshiftForm()"))),
                                    new Action(ret => ItemOgreDisguise.Use()),
                                    new Sleep(1000)
                    )))),
                    new Decorator(ret => Me.HasAura(75724),
                        new Sequence(
                            new DecoratorContinue(ret => _runToLoc.Distance(Me.Location) > 25,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(_runToLoc)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => _runToLoc.Distance(Me.Location) <= 25 && MobKarrgonn != null && MobKarrgonn[0].Location.Distance(Me.Location) > 5,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(MobKarrgonn[0].Location)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => MobKarrgonn != null && MobKarrgonn[0].Location.Distance(Me.Location) <= 5,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new Action(ret => MobKarrgonn[0].Interact()),
                                    new Action(ret => Lua.DoString("SelectGossipOption(1)")),
                                    new Sleep(5000)
                            )),
                            new DecoratorContinue(ret => MobAzennios != null && MobAzennios[0].Location.Distance(Me.Location) > 5,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(MobAzennios[0].Location)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => MobAzennios != null && MobAzennios[0].Location.Distance(Me.Location) <= 5,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new Action(ret => MobAzennios[0].Interact()),
                                    new Sleep(1000)
                            ))
                    )),
                    new ActionAlwaysSucceed())));
        }

        // ReSharper disable once CSharpWarnings::CS0672
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }

        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_MainCombat());
                this.UpdateGoalText(QuestId);
            }
        }
        #endregion
    }
}