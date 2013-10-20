using System;
using System.Collections.Generic;
using System.Linq;
using Bots.Grind;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


namespace Honorbuddy.Quest_Behaviors.Cava.EscapefromSilverbrook
{

    [CustomBehaviorFileName(@"Cava\12308-GrizzlyHills-EscapefromSilverbrook")]
    public class EscapefromSilverbrook : CustomForcedBehavior
    {
        ~EscapefromSilverbrook()
        {
            Dispose(false);
        }

        public EscapefromSilverbrook(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.

                QuestId = 12308;
                QuestRequirementComplete = GetAttributeAsNullable<QuestCompleteRequirement>("QuestCompleteRequirement", false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable<QuestInLogRequirement>("QuestInLogRequirement", false, null, null) ?? QuestInLogRequirement.InLog;

            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error",
                           "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message + "\nFROM HERE:\n" + except.StackTrace +
                           "\n");
                IsAttributeProblem = true;
            }
        }

        // Attributes provided by caller
        public int MobIds { get; private set; }
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }
        public WoWPoint Location { get; private set; }

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
                    // empty, for now
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }
            //Logging.Write("Disposing");

            //LevelBot.BehaviorFlags |= ~BehaviorFlags.Combat;

            _isDisposed = true;
        }

        #region Overrides of CustomForcedBehavior
        public Composite DoneYet
        {
            get
            {
                return
                    new Decorator(ret => IsQuestComplete(), new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        public bool IsQuestComplete()
        {
            if (QuestId == 0)
                return false;

            var quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
            return quest == null || quest.IsCompleted;
        }

        public Composite DoDps
        {
            get
            {
                return
                    new PrioritySelector(RoutineManager.Current.HealBehavior, RoutineManager.Current.CombatBuffBehavior, RoutineManager.Current.CombatBehavior);
            }
        }

        public List<WoWUnit> Wolf
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(r => (r.Entry == 27417 && r.IsAlive)).OrderBy(r => r.Distance).ToList(); }
        }

        public Composite GoobyPls
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(context => LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat),
                        new Action(context =>
                            {
                                LevelBot.BehaviorFlags &= ~BehaviorFlags.Combat;
                            }
                        )
                    ),
                    //new Decorator(context => !LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat),
                    //    new Action(context =>
                    //       {
                    //           LevelBot.BehaviorFlags |= BehaviorFlags.Combat;
                    //       }
                    //    )
                    //),
                    new Decorator(r => Me.InVehicle && Wolf.Count > 0, new Action(r =>
                         {
                             if (Wolf[0].Distance <= 15)
                             {  
                                 Lua.DoString("if GetPetActionCooldown(3) == 0 then CastPetAction(3) end");
                                 Lua.DoString("if GetPetActionCooldown(2) == 0 then CastPetAction(2) end");
                                 Lua.DoString("CastPetAction(1)");
                                 SpellManager.ClickRemoteLocation(Me.Location);
                             }
                         })),
                     new Decorator(context => Me.InVehicle,
                         new Action(context => { Lua.DoString("if GetPetActionCooldown(2) == 0 then CastPetAction(2) end"); })
                     )
 
                     //new Decorator(r => Me.InVehicle,
                     //   new Sequence(
                     //       new Action(ret => Lua.DoString("if GetPetActionCooldown(3) == 0 then CastPetAction(3) end")),
                     //       new Action(ret => Lua.DoString("if GetPetActionCooldown(2) == 0 then CastPetAction(2) end"))
                     //   ))
                    );
            }
        }

        protected override Composite CreateBehavior()
        {

            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(DoneYet, GoobyPls, new ActionAlwaysSucceed())));
        }

        public override void Dispose()
        {
            if (!LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat))
            {
                LevelBot.BehaviorFlags |= BehaviorFlags.Combat;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone || !Me.InVehicle || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
                TreeRoot.GoalText = this.GetType().Name + ": " +
                                    ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
            }
        }

        #endregion
    }
}