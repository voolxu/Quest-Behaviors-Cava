// Behavior originally contributed by mastahg.
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Summary and Documentation
#endregion


#region Examples
#endregion

using System.Diagnostics;

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
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.BrokenDreams
{
    [CustomBehaviorFileName(@"Cava\29952-VotFW-BrokenDreams")]
    public class BrokenDreams : CustomForcedBehavior
    {
        ~BrokenDreams()
        {
            Dispose(false);
        }

        public BrokenDreams(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 29952;
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
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();

        // Private properties
        private static LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
        }


        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    TreeHooks.Instance.RemoveHook("Questbot_Main", CreateBehavior_QuestbotMain());
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
        public WoWUnit Alemental
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(r => r.Entry == 56684 && !r.IsFriendly);
            }
        }
        public WoWUnit StupidMonkey
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(r => r.Entry == 56691);
            }
        }
        private int _stage;
        readonly WoWPoint _point1 = new WoWPoint(-748.1182, 1327.557, 146.7159);
        readonly WoWPoint _point2 = new WoWPoint(-808.0125, 1278.992, 146.6653);
        readonly WoWPoint _point3 = new WoWPoint(-766.4666, 1281.953, 162.6727);
        readonly WoWPoint _point4 = new WoWPoint(-751.9271, 1334.837, 162.6368);
        readonly WoWPoint _point5 = new WoWPoint(-712.1048, 1303.719, 162.6363);

        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId) || !Me.HasAura(106555) || _doingQuestTimer.ElapsedMilliseconds >= 240000,
                    new Action(delegate
                    {
                        StyxWoW.Sleep(20000);
                        TreeRoot.StatusText = "Finished!";
                        Lua.DoString("VehicleExit()");
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        public Composite SetProgress
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(r => Alemental != null && Alemental.IsDead && _stage == 0, new Action(r=>_stage = 1)),
                    new Decorator(r => StupidMonkey != null && StupidMonkey.IsDead && _stage == 1, new Action(r => _stage = 2)),
                    new Decorator(r => Me.Location.Distance(_point3) < 5 && _stage == 2, new Action(r => _stage = 3)),
                    new Decorator(r => Me.Location.Distance(_point4) < 5 && _stage == 3, new Action(r => _stage = 4)),
                    new Decorator(r => Me.Location.Distance(_point5) < 5 && _stage == 4, new Action(r => _stage = 2))
                    );
            }
        }

        public Composite StepOne
        {
            get
            {
                return new Decorator(r => _stage == 0, new PrioritySelector(
                    new Decorator(r => Alemental == null,
                        new Action(r => Navigator.MoveTo(_point1))),
                    new Decorator(r => Alemental != null && (!Me.GotTarget || Me.CurrentTarget != Alemental),
                        new Action(r => Alemental.Target())),
                    new Decorator(r => Alemental != null && Me.CurrentTarget == Alemental && Me.CurrentTarget.Distance < 5,
                        new Action(delegate
                        {
                            Alemental.Face();
                            Alemental.Interact();
                            UsePetSkill("Stormstout Fu");
                        }
                ))));
            }
        }

        public Composite StepTwo
        {
            get
            {
                return new Decorator(r => _stage == 1, new PrioritySelector(

                    new Decorator(r => StupidMonkey == null,
                        new Action(r => Navigator.MoveTo(_point2))),
                    new Decorator(r => StupidMonkey != null && (!Me.GotTarget || Me.CurrentTarget != StupidMonkey),
                        new Action(r => StupidMonkey.Target())),
                    new Decorator(r => StupidMonkey != null && Me.CurrentTarget == StupidMonkey && Me.CurrentTarget.Distance < 5,
                        new Action(delegate
                        {
                            StupidMonkey.Face();
                            StupidMonkey.Interact();
                            UsePetSkill("Stormstout Fu");
                        }
                ))));
            }
        }

        public Composite StepThree
        {
            get
            {
                return new Decorator(r => _stage == 2, new PrioritySelector(
                    new Action(r => Navigator.MoveTo(_point3))));
            }
        }

        public Composite StepFour
        {
            get
            {
                return new Decorator(r => _stage == 3, new PrioritySelector(
                    new Action(r => Navigator.MoveTo(_point4))));
            }
        }

        public Composite StepFive
        {
            get
            {
                return new Decorator(r => _stage == 4, new PrioritySelector(
                    new Action(r => Navigator.MoveTo(_point5))));
            }
        }

        public Composite Checkhealth
        {
            get
            {
                return new Decorator(r => Me.HealthPercent < 75 && !Me.Combat, new PrioritySelector(
                    new Action(delegate
                    {
                        QBCLog.Info("Drinking");
                        Lua.DoString("CastPetAction({0})", 5);
                    })));
            }
        }

        public void UsePetSkill(string action)
        {

            var spell = StyxWoW.Me.PetSpells.FirstOrDefault(p => p.ToString() == action);
            if (spell == null)
                return;
            QBCLog.Info("[Pet] Casting {0}", action);
            Lua.DoString("CastPetAction({0})", spell.ActionBarIndex + 1);
        }

        /*public delegate float DynamicRangeRetriever(object context);
        public delegate WoWPoint LocationRetriever(object context);
        public delegate WoWUnit UnitSelectionDelegate(object context);

        */
        /*public Composite CreateEnsureMovementStoppedBehavior()
        {
            return new Decorator(ret => StyxWoW.Me.CharmedUnit.IsMoving, new Action(ret => { WoWMovement.ClickToMove(Me.CharmedUnit.Location); return RunStatus.Failure; }));
        }
        */
        /*public Composite CreateMoveToMeleeBehavior(bool stopInRange)
        {
            return CreateMoveToMeleeBehavior(ret => StyxWoW.Me.CurrentTarget.Location, stopInRange);
        }
        */
        /*public Composite CreateMoveToMeleeBehavior(LocationRetriever location, bool stopInRange)
        {
            return
                new Decorator(
                    ret => !StyxWoW.Me.IsCasting,
                    CreateMoveToLocationBehavior(location, stopInRange, ret => 8f));
        }
        */

        /*public Composite CreateFaceTargetBehavior(UnitSelectionDelegate toUnit)
        {
            return new Decorator(
                ret =>
                toUnit != null && toUnit(ret) != null &&
                !StyxWoW.Me.IsMoving && !toUnit(ret).IsMe &&
                !StyxWoW.Me.IsSafelyFacing(toUnit(ret), 70f),
                new Action(ret =>
                {
                    StyxWoW.Me.CurrentTarget.Face();
                    return RunStatus.Failure;
                }));
        }
        */
        /*public Composite CreateMoveToLocationBehavior(LocationRetriever location, bool stopInRange, DynamicRangeRetriever range)
        {
            // Do not fuck with this. It will ensure we stop in range if we're supposed to.
            // Otherwise it'll stick to the targets ass like flies on dog shit.
            // Specifying a range of, 2 or so, will ensure we're constantly running to the target. Specifying 0 will cause us to spin in circles around the target
            // or chase it down like mad. (PVP oriented behavior)
            return new PrioritySelector(
                new Decorator(ret => stopInRange && StyxWoW.Me.CharmedUnit.Location.Distance(location(ret)) < range(ret),
                    CreateEnsureMovementStoppedBehavior()),
                new Decorator(ret => StyxWoW.Me.CharmedUnit.Location.Distance(location(ret)) > range(ret),
                    new Action(ret =>
                    {
                        Navigator.MoveTo(location(ret));
                        return RunStatus.Failure;
                    }))
                );
        }
        */

         /*

        //<Vendor Name="Chen Stormstout" Entry="56649" Type="Repair" X="-805.5576" Y="1272.881" Z="146.6652" />
        //<Vendor Name="Wuk-Wuk" Entry="56691" Type="Repair" X="-806.7507" Y="1276.348" Z="146.6656" />
        //<Vendor Name="Uncle Gao" Entry="56680" Type="Repair" X="-751.9271" Y="1334.837" Z="162.6358" />
        //Stupid ass game doesnt detect that you walked close sometimes, so we need to do some stupid shit

        private int Bouncestage = 0;

        //<Vendor Name="Chen Stormstout" Entry="56649" Type="Repair" X="-769.0266" Y="1274.853" Z="162.7204" />
        readonly WoWPoint _bounce = new WoWPoint(-769.0266,1274.853,162.7204);
        */
        /*public Composite Move
        {
            get
            {
                return
                    new PrioritySelector(
                        new Decorator(r => _point3.Distance(Me.Location) > 10 && Bouncestage <= 0, new Action(r => Navigator.MoveTo(_point3))),
                        new Decorator(r => _point3.Distance(Me.Location) < 10 && Bouncestage == 0, new Action(r => _stage = 1))
                        );
            }
        }*/


        /*public Composite Bouncez
        {
            get
            {
                return
                    new PrioritySelector(
                        new Decorator(r => Me.Location.Distance(_bounce) > 3 && Bouncestage == 1, new Action(r => Navigator.MoveTo(_bounce))),
                        new Decorator(r => Me.Location.Distance(_bounce) <= 3 && Bouncestage == 1, new Action(r => _stage = -1))
                        );
            }
        }*/


        protected Composite CreateBehavior_QuestbotMain()
        {
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(
                DoneYet,
                SetProgress,
                StepOne,
                Checkhealth,
                StepTwo,
                Checkhealth,
                StepThree,
                StepFour,
                StepFive,
                new ActionAlwaysSucceed()
            )));
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
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());

                this.UpdateGoalText(QuestId);
            }
        }
        #endregion
    }
}