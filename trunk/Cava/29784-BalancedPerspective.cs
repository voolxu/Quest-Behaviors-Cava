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
namespace Honorbuddy.Quest_Behaviors.Cava.BalancedPerspective
{
    [CustomBehaviorFileName(@"Cava\29784-BalancedPerspective")]
    public class BalancedPerspective : CustomForcedBehavior
    {
        ~BalancedPerspective()
        {
            Dispose(false);
        }

        public BalancedPerspective(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 29784;
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
        private int _stage = 1;
        readonly WoWPoint _point1 = new WoWPoint(1128.444, 4154.432, 190.6452);//inicio
        readonly WoWPoint _point2 = new WoWPoint(1130.029, 4150.454, 190.9646);//junto inicio
        readonly WoWPoint _point3 = new WoWPoint(1130.309, 4148.83, 191.4986); //inicio barrote 1
        readonly WoWPoint _point4 = new WoWPoint(1138.769, 4119.33, 196.4878); //barrote 2
        readonly WoWPoint _point5 = new WoWPoint(1123.548, 4100.45, 196.5883); //barrote 3
        readonly WoWPoint _point6 = new WoWPoint(1147.549, 4099.289, 195.9034);//perto barrote 4
        readonly WoWPoint _point7 = new WoWPoint(1151.134, 4099.06, 196.4183); //barrote 4


        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => _stage == 8 || _doingQuestTimer.ElapsedMilliseconds >= 180000,
                    new Action(delegate
                    {
                        StyxWoW.Sleep(20000);
                        TreeRoot.StatusText = "Finished!";
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
                    new Decorator(r => Me.Z < 190 && _stage > 3, new Action(r => _stage = 1)),
                    new Decorator(r => Me.Location.Distance(_point1) < 2 && _stage == 1, new Action(r => _stage = 2)),
                    new Decorator(r => Me.Location.Distance(_point2) < 2 && _stage == 2, new Action(r => _stage = 3)),
                    new Decorator(r => Me.Location.Distance(_point3) < 2 && _stage == 3, new Action(r => _stage = 4)),
                    new Decorator(r => Me.Location.Distance(_point4) < 2 && _stage == 4, new Action(r => _stage = 5)),
                    new Decorator(r => Me.Location.Distance(_point5) < 2 && _stage == 5, new Action(r => _stage = 6)),
                    new Decorator(r => Me.Location.Distance(_point6) < 2 && _stage == 6, new Action(r => _stage = 7)),
                    new Decorator(r => Me.Location.Distance(_point7) < 2 && _stage == 7, new Action(r => _stage = 8))
                    );
            }
        }

        public Composite StepOne
        {
            get
            {
                return new Decorator(r => _stage == 1, new PrioritySelector(
                    //new Action(r => Navigator.MoveTo(_point1))));
                    new Action(delegate
                    {
                        Navigator.MoveTo(_point1);
                        QBCLog.Info("Moving to Point 1");
                    })));
            }
        }

        public Composite StepTwo
        {
            get
            {
                return new Decorator(r => _stage == 2, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point2))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point2);
                        QBCLog.Info("Moving to Point 2");
                    })));
            }
        }

        public Composite StepThree
        {
            get
            {
                return new Decorator(r => _stage == 3, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point3))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point3);
                        QBCLog.Info("Moving to Point 3");
                    })));
            }
        }

        public Composite StepFour
        {
            get
            {
                return new Decorator(r => _stage == 4, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point4))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point4);
                        QBCLog.Info("Moving to Point 4");
                    })));
            }
        }

        public Composite StepFive
        {
            get
            {
                return new Decorator(r => _stage == 5, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point5))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point5);
                        QBCLog.Info("Moving to Point 5");
                    })));
            }
        }

        public Composite StepSix
        {
            get
            {
                return new Decorator(r => _stage == 6, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point6))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point6);
                        QBCLog.Info("Moving to Point 6");
                    })));
            }
        }

        public Composite StepSeven
        {
            get
            {
                return new Decorator(r => _stage == 7, new PrioritySelector(
                    //new Action(r => WoWMovement.ClickToMove(_point7))));
                    new Action(delegate
                    {
                        WoWMovement.ClickToMove(_point7);
                        QBCLog.Info("Moving to Point 7");
                    })));
            }
        }
        public Composite StepEight
        {
            get
            {
                return new Decorator(r => _stage == 8, new PrioritySelector(
                    new Action(delegate
                    {
                        _isBehaviorDone = true;
                        QBCLog.Info("Finishing Behavior");
                    })));
            }
        }

 
        protected Composite CreateBehavior_QuestbotMain()
        {
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(
                DoneYet,
                SetProgress,
                StepOne,
                StepTwo,
                StepThree,
                StepFour,
                StepFive,
                StepSix,
                StepSeven,
                StepEight,
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
                return (_isBehaviorDone);
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