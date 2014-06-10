// Behavior originally contributed by Cava
// Part of this code obtained from HB QB's and UseHearthstone.cs originaly contributed by Azaril
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.

#region Summary and Documentation
// QUICK DOX:
// GoSavedInn use hearthstone checking the BindPoints.
//
// BEHAVIOR ATTRIBUTES:
//
// QuestId: (Optional) - associates a quest with this behavior. 
// QuestCompleteRequirement [Default:NotComplete]:
// QuestInLogRequirement [Default:InLog]:
//	If the quest is complete or not in the quest log, this Behavior will not be executed.
//	A full discussion of how the Quest* attributes operate is described in
//      http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//
// BindPointAreaId: (Required ) - Specifies the Number of BindPoint where we want to go, if the toon
// isnt saved at that inn the behavior is ignored.
//
// ForceTheUse: (Optional) [Default: false] - Specifies if behavior will always wait for cooldwon to use Heartstone. 
//
// ShowBindPoint: (Optional) [Default: false] - For Debug: if enabled to true, behavior will just output message with BindPoint number.
//
// WaitTime [optional; Default: 0ms]
//	Defines an extra number of milliseconds to wait after use the item, toon always have 12secs waittime
//
// (X,Y,Z) [Optional]; [Default: Toon Local] - If spicified QB will move to that location before using item.
//
// THINGS TO KNOW:
//
// The idea of this Behavior is use Heartstone to be summoned to last saved inn.
// Behavior check if the saved inn have the same bindpoint specified when its called.
// there are some options to use, read attributes to check then.
// If enable ForceTheUse and HS is in cooldown, Toon will wait till can use HS with AFK routine (Jumping each 2 minutes)
//
// EXAMPLES:
// <CustomBehavior File="Cava\GoSavedInn" BindPointAreaId="123" />
// <CustomBehavior File="Cava\GoSavedInn" ShowBindPoint="True" BindPointAreaId="123" />
// <CustomBehavior File="Cava\GoSavedInn" ForceTheUse="True" BindPointAreaId="789" X="123.2" Y="456.5" Z="19.9" />
#endregion

#region Usings
using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Threading;

using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
//using Styx.CommonBot.Routines;
//using Styx.Helpers;
using Styx.Pathing;
//using Styx.Plugins;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
//using CommonBehaviors.Actions;

using Action = Styx.TreeSharp.Action;
#endregion

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.GoSavedInn
{
    [CustomBehaviorFileName(@"Cava\GoSavedInn")]
    public class GoSavedInn : CustomForcedBehavior
    {
        public GoSavedInn(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
  
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
  
                QuestId = GetAttributeAsNullable("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
                BindPointAreaId = GetAttributeAsNullable("BindPointAreaId", true, ConstrainAs.ItemId, null) ?? 0;
                ForceTheUse = GetAttributeAsNullable<bool>("ForceTheUse", false, null, null) ?? false;
                ShowBindPoint = GetAttributeAsNullable<bool>("ShowBindPoint", false, null, null) ?? false;
                WaitTime = GetAttributeAsNullable("WaitTime", false, ConstrainAs.Milliseconds, null) ?? 12000;
                Location = GetAttributeAsNullable("", false, ConstrainAs.WoWPointNonEmpty, null) ?? WoWPoint.Empty;
                if (!ShowBindPoint && BindPointAreaId <= 0)
                // Semantic coherency / covariant dependency checks --
                {
                    LogMessage("error", "Attribute 'BindPointAreaId' is required, but was not provided.");
                    IsAttributeProblem = true;
                }
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }

        // Attributes provided by caller
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; set; }
        public QuestInLogRequirement QuestRequirementInLog { get; set; }
        public int BindPointAreaId { get; private set; }
        public bool ForceTheUse { get; private set; }
        public bool ShowBindPoint { get; private set; }
        public int WaitTime { get; private set; }
        public WoWPoint Location { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;

        // Private properties
  
        protected LocalPlayer Me { get { return StyxWoW.Me; } }
        private WoWItem ItemId { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 6948)); } }
        private readonly WaitTimer _afkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id$:"); } }
        public override string SubversionRevision { get { return ("$Rev$:"); } }


        ~GoSavedInn()
        {
            Dispose(false);
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
                    // empty, for now
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
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
            new PrioritySelector(

                // output message with BindPoint number and exit
                new Decorator(ret => ShowBindPoint,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Current bind location - Name: " + GetBindLocation() + " - ID: " + Me.HearthstoneAreaId),
                        new Action(ret => _isBehaviorDone = true)
                )),

                // no HS = exit
                new Decorator(ret => ItemId == null,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Don't have Heartstone in bags skipping Behavior"),
                        new Action(ret => _isBehaviorDone = true)
                )),

                // rong BindPointAreaId = exit
                new Decorator(ret => Me.HearthstoneAreaId != BindPointAreaId,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Hearth is not set to defined Bidpointnumber, skipping behavior"),
                        new Action(ret => _isBehaviorDone = true)
                )),

                // if HS in cooldown exit, but will continue till use HS if ForceTheUse==true
                new Decorator(ret => ItemId.Cooldown != 0 && !ForceTheUse,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Hearthstone in cooldown skipping"),
                        new Action(ret => _isBehaviorDone = true)
                )),

                // if we have Location then move
                new Decorator(ret => Location != WoWPoint.Empty && Location.Distance(Me.Location) > 2,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Moving to location"),
                        new Action(ret => Navigator.MoveTo(Location))
                )),

                // if we are moving then stop ( cant cast HS while moving)
                new Decorator(ret => Me.IsMoving,
                    new Sequence(
                        new Action(ret => Navigator.PlayerMover.MoveStop()),
                        new Action(ret => StyxWoW.SleepForLagDuration())
                )),

                // if HS in cooldown but have ForceTheUse==true keep waiting with AFK jumps
                new Decorator(ret => ItemId.Cooldown != 0 && ForceTheUse,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Hearthstone is in cooldown, waiting till can use it"),
                        new Action(ret => AntiAfk())
                )),

                // lets use HS after use exit
                new Decorator(ret => ItemId.Cooldown == 0,
                    new Sequence(
                        new Action(ret => TreeRoot.StatusText = "Using Hearthstone"),
                        new Action(ret => Mount.Dismount()), 
                        new Action(ret => ItemId.UseContainerItem()),
                        new Action(ret => StyxWoW.SleepForLagDuration()),
                        new DecoratorContinue( ret => !Me.Combat,
                        new Action(ret => Thread.Sleep(12000 + WaitTime))),
                        new DecoratorContinue( ret => ItemId.Cooldown != 0,
                            new Action(ret => _isBehaviorDone = true))
                 ))
            ));
        }

        string GetBindLocation()
        {
            return Lua.GetReturnVal<string>("return GetBindLocation();", 0);
        }

        private void AntiAfk()
        {
            if (_afkTimer.IsFinished)
            {
                WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend, TimeSpan.FromMilliseconds(100));
                _afkTimer.Reset();
            }
        }

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

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = this.GetType().Name + ": " + ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
            }
        }
        #endregion
    }
}
