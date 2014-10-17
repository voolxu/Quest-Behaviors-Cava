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
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.ScentOfBattle
{
    [CustomBehaviorFileName(@"Cava\27811-AllyTwilightHighlands-ScentOfBattle")]
    public class ScentOfBattle : CustomForcedBehavior
    {
        ~ScentOfBattle()
        {
            Dispose(false);
        }

        public ScentOfBattle(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 27811;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
                MobIds = new uint[] { 50635, 50638, 50643, 50636 };
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
        public uint[] MobIds { get; private set; }
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }


        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;


        // Private properties
        private LocalPlayer Me
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

        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId),
                    new Action(delegate
                    {                                                       
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        public WoWUnit Normal
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 46968 && u.IsAlive && !u.IsMoving).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        public WoWUnit Pinned
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 46969 && u.IsAlive && u.HasAura(87490)).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        public WoWUnit Pin
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(p => p.IsAlive && p.Entry == 46975 && p.Location.Distance(Pinned.Location) <= (20));

            }
        }

        private Composite TryCast(int spellId, ProvideBoolDelegate requirements = null)
        {
            requirements = requirements ?? (context => true);

            return new Decorator(context => SpellManager.CanCast(spellId) && requirements(context),
                new Action(context =>
                {
                    QBCLog.DeveloperInfo("MiniCombatRoutine used {0}", Utility.GetSpellNameFromId(spellId));
                    SpellManager.Cast(spellId);
                }));
        }

        public Composite DoDps
        {
            get
            {
                return
                    new PrioritySelector(
                        new Decorator(ret => Me.Combat && Me.Mounted,
                            new Sequence(
                                new Action(ret =>WoWMovement.MoveStop()),
                                new Action(ret =>Lua.DoString("Dismount()")),
                                new Decorator(ret => Me.Class == WoWClass.Druid,
                                    new Action(ret => Lua.DoString("RunMacroText('/cancelaura Flight Form')"))),
                                new Action(ret => new Mount.ActionLandAndDismount())
                        )),
                        //new Decorator(ret => RoutineManager.Current.CombatBehavior != null, RoutineManager.Current.CombatBehavior),
                        //new Action(c => RoutineManager.Current.Combat()),
                        new Decorator(ret => Me.CurrentTarget != null && Me.CurrentTarget.IsDead,
                            new Sequence(
                                new Action(ret => Blacklist.Add(Me.CurrentTarget, BlacklistFlags.Combat, TimeSpan.FromSeconds(180000))),
                                new Action(ret => Me.ClearTarget())
                        )),
                        new Decorator(ret => Me.CurrentTarget != null && Me.CurrentTarget.Location.Distance(Me.Location) > 4,
                            new Action(c => Navigator.MoveTo(Me.CurrentTarget.Location))),
                        new Decorator(ret => !Me.IsAutoAttacking,
                            new Action(c => Lua.DoString("StartAttack()"))),
                        new Decorator(context => !WoWMovement.ActiveMover.IsSafelyFacing(Me.CurrentTarget, 30),
                            new ActionFail(context => Me.SetFacing(Me.CurrentTarget.Location))),
                        new Decorator(ret => Me.CurrentTarget.Location.Distance(Me.Location) <= 4,
                            new Switch<WoWClass>(context => Me.Class,
                                new SwitchArgument<WoWClass>(WoWClass.DeathKnight,
                                    new PrioritySelector(
                                        TryCast(49998)      // Death Strike: http://wowhead.com/spell=49998
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Druid,
                                    new PrioritySelector(
                                        TryCast(5176, context => !Me.HasAura(768)),      // Wrath: http://wowhead.com/spell=5176
                                        TryCast(768, context => !Me.HasAura(768)),       // Cat Form: http://wowhead.com/spell=768
                                        TryCast(1822),      // Rake: http://wowhead.com/spell=1822
                                        TryCast(22568),     // Ferocious Bite: http://wowhead.com/spell=22568
                                        TryCast(33917)      // Mangle: http://wowhead.com/spell=33917
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Hunter,
                                    new PrioritySelector(
                                        TryCast(3044),      // Arcane Shot: http://wowhead.com/spell=3044
                                        TryCast(56641)      // Steady Shot: http://wowhead.com/spell=56641
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Mage,
                                    new PrioritySelector(
                                        TryCast(44614),     // Frostfire Bolt: http://wowhead.com/spell=44614
                                        TryCast(126201),    // Frostbolt: http://wowhead.com/spell=126201
                                        TryCast(2136)       // Fire Blast: http://wowhead.com/spell=2136
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Monk,
                                    new PrioritySelector(
                                        TryCast(100780),    // Jab: http://wowhead.com/spell=100780
                                        TryCast(100787)     // Tiger Palm: http://wowhead.com/spell=100787
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Paladin,
                                    new PrioritySelector(
                                        TryCast(35395),     // Crusader Strike: http://wowhead.com/spell=35395
                                        TryCast(20271)      // Judgment: http://wowhead.com/spell=20271
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Priest,
                                    new PrioritySelector(
                                        TryCast(589, context => !Me.CurrentTarget.HasAura(589)),      // Shadow Word: Pain: http://wowhead.com/spell=589
                                        TryCast(15407),     // Mind Flay: http://wowhead.com/spell=15407
                                        TryCast(585)        // Smite: http://wowhead.com/spell=585
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Rogue,
                                    new PrioritySelector(
                                        TryCast(2098),      // Eviscerate: http://wowhead.com/spell=2098
                                        TryCast(1752)       // Sinster Strike: http://wowhead.com/spell=1752
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Shaman,
                                    new PrioritySelector(
                                        TryCast(17364),     // Stormstrike: http://wowhead.com/spell=17364
                                        TryCast(403),       // Lightning Bolt: http://wowhead.com/spell=403
                                        TryCast(73899)      // Primal Strike: http://wowhead.com/spell=73899
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Warlock,
                                    new PrioritySelector(
                                        TryCast(686)        // Shadow Bolt: http://wowhead.com/spell=686
                                )),
                                new SwitchArgument<WoWClass>(WoWClass.Warrior,
                                    new PrioritySelector(
                                        TryCast(78),        // Heroic Strike: http://wowhead.com/spell=78
                                        TryCast(34428),     // Victory Rush: http://wowhead.com/spell=34428
                                        TryCast(23922),     // Shield Slam: http://wowhead.com/spell=23922
                                        TryCast(20243)      // Devastate: http://wowhead.com/spell=20243
                        ))))
                );
            }
        }

        public Composite GetInRange
        {
            get
            {
                return new Decorator(r => Normal.Distance > 1, new Action(r=>Flightor.MoveTo(Normal.Location)));
            }
        }
        public Composite GetInRangep
        {
            get
            {
                return new Decorator(r => Pinned.Distance > 1, new Action(r => Flightor.MoveTo(Pinned.Location)));
            }
        }

        public Composite Interact
        {
            get
            {
                return new Decorator(r => Normal.Distance <= 1, new Action(delegate
                    {
                        Normal.Interact();
                        Lua.DoString("SelectGossipOption(1);");
                }));
            }
        }



        public Composite NormalGryphon
        {
            get 
            {
                return new Decorator(r => Normal != null && !Me.Combat, new PrioritySelector(GetInRange, Interact));
            }
        }


        public Composite KillPegs
        {
            get
            {
                return new Decorator(r => Pin != null && !Me.Combat, new Action(delegate
                    {
                        Pin.Target();
                        Pin.Interact();
                }));
            }
        }


        public Composite PinnedGryphon
        {
            get
            {
                return new Decorator(r => Pinned != null && !Me.Combat, new PrioritySelector(GetInRangep, KillPegs));
            }
        }


        public Composite Combat
        {
            get
            {
                return new Decorator(r => Me.Combat,DoDps);
            }
        }


        protected Composite CreateBehavior_QuestbotMain()
        {
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
                new PrioritySelector(
                    DoneYet,
                    Combat,
                    NormalGryphon,
                    PinnedGryphon,
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
                return (_isBehaviorDone // normal completion
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
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());
                this.UpdateGoalText(QuestId);
            }
        }

        #endregion
    }
}