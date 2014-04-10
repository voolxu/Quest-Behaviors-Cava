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

//using System.Diagnostics;
using Styx.CommonBot.Routines;
using Styx.Pathing;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

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
namespace Honorbuddy.Quest_Behaviors.Cava.TheTerrorsofIsorath
{
    [CustomBehaviorFileName(@"Cava\27379-TheTerrorsofIsorath")]
    public class TheTerrorsofIsorath : CustomForcedBehavior
    {
        public TheTerrorsofIsorath(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 27379;
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
        public int QuestId { get; set; }
        private bool _isBehaviorDone;
        private bool _closebehavior;

        private Composite _root;

        public override bool IsDone
        {
            get
            {
                return _isBehaviorDone;
            }
        }
        private static LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
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

        public WoWUnit TentacleofIsorath3
        {
            get
            {
                return 
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 48794 && u.IsAlive)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }

        public WoWUnit TentacleofIsorath1
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 48739 && u.IsAlive)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }

        public WoWUnit TentacleofIsorath4
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 48796 && u.IsAlive)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }

        public WoWUnit TentacleofIsorath2
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 48790 && u.IsAlive)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }
        
        
        private readonly WoWPoint _safeLoc1 = new WoWPoint(-2739.093, -5001, -127.1468);
        private readonly WoWPoint _safeLoc2 = new WoWPoint(-2644.046, -5046.208, -126.7226);
        private readonly WoWPoint _safeLoc3 = new WoWPoint(-2591.309, -4974.548, -126.6994);
        private readonly WoWPoint _safeLoc4 = new WoWPoint(-2673.123, -4893.819, -127.1566);
        private int _bestloc = 1;
        private bool _needhealing;
        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => _closebehavior,
                    new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }
        private Composite MoveBestLoc()
        {
            return new Decorator(ret => Me.HealthPercent  <25 || Me.HasAura(90805) || _needhealing,
                new Action(ctx =>
                {
                    _needhealing = true;
                    _bestloc = 1;
                    if ((Me.Location.Distance(_safeLoc2) < Me.Location.Distance(_safeLoc1)) && (Me.Location.Distance(_safeLoc2) < Me.Location.Distance(_safeLoc3)) && (Me.Location.Distance(_safeLoc2) < Me.Location.Distance(_safeLoc4)))
                    {_bestloc = 2;}
                    if ((Me.Location.Distance(_safeLoc3) < Me.Location.Distance(_safeLoc1)) && (Me.Location.Distance(_safeLoc3) < Me.Location.Distance(_safeLoc2)) && (Me.Location.Distance(_safeLoc3) < Me.Location.Distance(_safeLoc4)))
                    {_bestloc = 3;}
                    if ((Me.Location.Distance(_safeLoc4) < Me.Location.Distance(_safeLoc1)) && (Me.Location.Distance(_safeLoc4) < Me.Location.Distance(_safeLoc2)) && (Me.Location.Distance(_safeLoc4) < Me.Location.Distance(_safeLoc3)))
                    { _bestloc = 4; }
                    if (_bestloc == 1 && Me.Location.Distance(_safeLoc1) > 5)
                        Navigator.MoveTo(_safeLoc1);
                    if (_bestloc == 2 && Me.Location.Distance(_safeLoc2) > 5)
                        Navigator.MoveTo(_safeLoc2);
                    if (_bestloc == 3 && Me.Location.Distance(_safeLoc3) > 5)
                        Navigator.MoveTo(_safeLoc3);
                    if (_bestloc == 4 && Me.Location.Distance(_safeLoc4) > 5)
                        Navigator.MoveTo(_safeLoc4);
                }));
        }
        private int power
        {
            get { return Lua.GetReturnVal<int>("return UnitPower(\"player\", ALTERNATE_POWER_INDEX)", 0); }
        }

/*        private static Composite TryCast(int spellId, ProvideBoolDelegate requirements = null)
        {
            requirements = requirements ?? (context => true);

            return new Decorator(context => SpellManager.CanCast(spellId) && requirements(context),
                new Action(context =>
                {
                    QBCLog.DeveloperInfo("MiniCombatRoutine used {0}", Utility.GetSpellNameFromId(spellId));
                    SpellManager.Cast(spellId);
                }));
        }
*/
        public Composite DoDps
        {
            get
            {
                return
                    new PrioritySelector(
                        new Decorator(ret => Me.CurrentTarget != null && Me.CurrentTarget.IsDead,
                            new Sequence(
                                new Action(ret => Blacklist.Add(Me.CurrentTarget, BlacklistFlags.Combat, TimeSpan.FromSeconds(180000))),
                                new Action(ret => Me.ClearTarget())
                        )),
                        new Decorator(ret => Me.CurrentTarget != null && Me.CurrentTarget.Location.Distance(Me.Location) > 5 && !_needhealing,
                            new Action(c => Navigator.MoveTo(Me.CurrentTarget.Location))),
                        new Decorator(ret => !Me.IsAutoAttacking && !_needhealing,
                            new Action(c => Lua.DoString("StartAttack()"))),
                        new Decorator(context => !WoWMovement.ActiveMover.IsSafelyFacing(Me.CurrentTarget, 30) && !_needhealing,
                            new ActionFail(context => Me.SetFacing(Me.CurrentTarget.Location))),
                        new Decorator(ret => Me.CurrentTarget.Location.Distance(Me.Location) <= 5 && !_needhealing && RoutineManager.Current.CombatBehavior != null,RoutineManager.Current.CombatBehavior),
                        new Action(c => RoutineManager.Current.Combat())

                            /*new Switch<WoWClass>(context => Me.Class,
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
                        ))))*/
                );
            }
        }

        public Composite Part1()
        {
            return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 1) && !_needhealing && !Me.Combat,
                new PrioritySelector(
                    new Decorator(r => (Me.Combat && Me.CurrentTarget != null && Me.CurrentTarget == TentacleofIsorath1 && TentacleofIsorath1.Distance > 5) || (TentacleofIsorath1 != null && TentacleofIsorath1.Distance > 5),
                        new Action(r => Navigator.MoveTo(TentacleofIsorath1.Location))),
                    new Decorator(r => TentacleofIsorath1 != null && TentacleofIsorath1.Distance < 5 && Me.CurrentTarget != null && Me.CurrentTarget != TentacleofIsorath1,
                        new Action(r => Me.ClearTarget())),
                    new Decorator(r => TentacleofIsorath1 != null && TentacleofIsorath1.Distance < 5 && Me.CurrentTarget == null,
                        new Action(delegate
                        {
                            WoWMovement.MoveStop();
                            if (Me.Mounted)
                            {Lua.DoString("Dismount()");}
                            TentacleofIsorath1.Target();
                            TentacleofIsorath1.Interact();
                        }))
            ));
        }
        public Composite Part2()
        {
            return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 2) && !_needhealing && !Me.Combat,
                new PrioritySelector(
                    new Decorator(r => (Me.Combat && Me.CurrentTarget != null && Me.CurrentTarget == TentacleofIsorath2 && TentacleofIsorath2.Distance > 5) || (TentacleofIsorath2 != null && TentacleofIsorath2.Distance > 5),
                        new Action(r => Navigator.MoveTo(TentacleofIsorath2.Location))),
                    new Decorator(r => TentacleofIsorath2 != null && TentacleofIsorath2.Distance < 5 && Me.CurrentTarget != null && Me.CurrentTarget != TentacleofIsorath2,
                        new Action(r => Me.ClearTarget())),
                    new Decorator(r => TentacleofIsorath2 != null && TentacleofIsorath2.Distance < 5 && Me.CurrentTarget == null,
                        new Action(delegate
                        {
                            WoWMovement.MoveStop();
                            if (Me.Mounted)
                            { Lua.DoString("Dismount()"); }
                            TentacleofIsorath2.Target();
                            TentacleofIsorath2.Interact();
                        }))
            ));
        }
        public Composite Part3()
        {
            return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 3) && !_needhealing && !Me.Combat,
                new PrioritySelector(
                    new Decorator(r => (Me.Combat && Me.CurrentTarget != null && Me.CurrentTarget == TentacleofIsorath3 && TentacleofIsorath3.Distance > 5) || (TentacleofIsorath3 != null && TentacleofIsorath3.Distance > 5),
                        new Action(r => Navigator.MoveTo(TentacleofIsorath3.Location))),
                    new Decorator(r => TentacleofIsorath3 != null && TentacleofIsorath3.Distance < 5 && Me.CurrentTarget != null && Me.CurrentTarget != TentacleofIsorath3,
                        new Action(r => Me.ClearTarget())),
                    new Decorator(r => TentacleofIsorath3 != null && TentacleofIsorath3.Distance < 5 && Me.CurrentTarget == null,
                        new Action(delegate
                        {
                            WoWMovement.MoveStop();
                            if (Me.Mounted)
                            { Lua.DoString("Dismount()"); }
                            TentacleofIsorath3.Target();
                            TentacleofIsorath3.Interact();
                        }))
            ));
        }
        public Composite Part4()
        {
            return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 4) && !_needhealing && !Me.Combat,
                new PrioritySelector(
                    new Decorator(r => (Me.Combat && Me.CurrentTarget != null && Me.CurrentTarget == TentacleofIsorath4 && TentacleofIsorath4.Distance > 5) || (TentacleofIsorath4 != null && TentacleofIsorath4.Distance > 5),
                        new Action(r => Navigator.MoveTo(TentacleofIsorath4.Location))),
                    new Decorator(r => TentacleofIsorath4 != null && TentacleofIsorath4.Distance < 5 && Me.CurrentTarget != null && Me.CurrentTarget != TentacleofIsorath4,
                        new Action(r => Me.ClearTarget())),
                    new Decorator(r => TentacleofIsorath4 != null && TentacleofIsorath4.Distance < 5 && Me.CurrentTarget == null,
                        new Action(delegate
                        {
                            WoWMovement.MoveStop();
                            if (Me.Mounted)
                            { Lua.DoString("Dismount()"); }
                            TentacleofIsorath4.Target();
                            TentacleofIsorath4.Interact();
                        }))
            ));
        }
        public Composite Checkheal()
        {
            return new Decorator(r => !_needhealing,
                new PrioritySelector(
                    new Decorator(r => Me.HasAura(90805) || Me.HealthPercent < 25,
                        new Action(r => _needhealing=true))
            ));
        }
        public Composite Stopheal()
        {
            return new Decorator(r => _needhealing,
                new PrioritySelector(
                    new Decorator(r => !Me.HasAura(90805) && Me.HealthPercent > 95 && power == 0,
                        new Action(r => _needhealing = false))
            ));
        }
        public Composite RunToStart()
        {
            return new Decorator(r => Me.IsQuestComplete(QuestId) && !_closebehavior,
                new PrioritySelector(
                    new Decorator(r => Me.Location.Distance(_safeLoc1) > 5,
                        new Action(delegate
                        {
                            TreeRoot.StatusText = "Running to start!";
                            Navigator.MoveTo(_safeLoc1);
                        }
                    )),
                    new Decorator(r => Me.Location.Distance(_safeLoc1) <= 5 && !Me.HasAura(90805) && Me.HealthPercent > 95 && power == 0,
                        new Action(r => _closebehavior = true))
            ));
        }
         
        protected Composite CreateBehavior_MainCombat()
        {
            return _root ?? (_root =
                new Decorator(ret => !_isBehaviorDone,
                    new PrioritySelector(
                        RunToStart(),
                        DoneYet,
                        Checkheal(),
                        Stopheal(),
                        MoveBestLoc(),
                        new Decorator(r => Me.CurrentTarget != null && Me.CurrentTarget.IsFriendly,
                            new Action(r => Me.ClearTarget())),
                        Part2(),
                        Part3(),
                        Part1(),
                        Part4(),
                        DoDps
            )));
        }
        #region Cleanup

        private bool _isDisposed;

        ~TheTerrorsofIsorath()
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

        // ReSharper disable once CSharpWarnings::CS0672
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}



