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

using CommonBehaviors.Actions;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

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
namespace Honorbuddy.Quest_Behaviors.Cava.ADisarmingDistraction
{
    [CustomBehaviorFileName(@"Cava\27761-Uldum-ADisarmingDistraction")]
    public class BomberMan : CustomForcedBehavior // A Disarming Distraction
    {
        ~BomberMan()
        {
            Dispose(false);
        }

        public BomberMan(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 27761;
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
                return
                    new Decorator(ret => Me.IsQuestComplete(QuestId) && Me.Mounted,
                        new Action(delegate
                        {
                            TreeRoot.StatusText = "Finished!";
                            _isBehaviorDone = true;
                            return RunStatus.Success;
                        }));

            }
        }







        public List<WoWUnit> Enemies
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.FactionId == 2334 && u.IsAlive).OrderBy(u => u.Distance).ToList();
            }
        }


        public WoWUnit ClosestBomb()
        {
            return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 46888 && u.IsAlive && u.Location.Distance(_badBomb) > 10).OrderBy(u => u.Distance).FirstOrDefault();
        }


        public int Hostiles
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().Count(
                        u => u.IsHostile && u.Location.Distance(ClosestBomb().Location) <= 10);
            }

        }

        public Composite DeployHologram
        {
            get
            {
                return new Decorator(ret => Hostiles > 0,
                    new Action(r => Hologram().Use()));
            }
        }


        public Composite FindBomb
        {
            get
            {
                return new Decorator(ret => Me.Location.Distance(ClosestBomb().Location) > 12,
                    new Action(delegate
                    {
                        var x = ClosestBomb().Location;
                        x.Z += 10;
                        Flightor.MoveTo(x);
                    }));
            }
        }


        public Composite BreakCombat
        {
            get
            {
                return new Decorator(ret => Me.Combat,
                    new Action(r => Hologram().Use()));
            }
        }

        public Composite Mount
        {
            get
            {
                return new Decorator(ret => !Me.Mounted && ClosestBomb().Distance > 10,
                    new Action(ret => Lua.DoString("RunMacroText('/run for i=1,  GetNumCompanions('MOUNT') do local flags = select(6, GetCompanionInfo('MOUNT', i)); if bit.band(flags, 0x2) ~= 0 then CallCompanion('MOUNT',i) return 1 end end return nil','0')"))
                );
            }
        }

        public Composite UseAndGo
        {
            get
            {
                return new Action(delegate {
										Lua.DoString("Dismount()");								
                    ClosestBomb().Interact();
                    WoWMovement.MoveStop();
                    Lua.DoString("RunMacroText('/run for i=1,  GetNumCompanions('MOUNT') do local flags = select(6, GetCompanionInfo('MOUNT', i)); if bit.band(flags, 0x2) ~= 0 then CallCompanion('MOUNT',i) return 1 end end return nil','0')");
                });
            }
        }

        public WoWItem Hologram()
        {
            return Me.BagItems.FirstOrDefault(x => x.Entry == 62398);
        }

        protected Composite CreateBehavior_QuestbotMain()
        {

            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
                new PrioritySelector(
                    new Decorator(ret => Me.IsQuestComplete(QuestId) && Me.Location.Distance(_safeLoc) > 5,
                            new Action(ret => Flightor.MoveTo(_safeLoc))),
                    new Decorator(ret => Me.IsQuestComplete(QuestId) && Me.Location.Distance(_safeLoc) <= 5,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new Action(ret => _isBehaviorDone = true),
                            new ActionAlwaysSucceed()
                    )),
                    new Decorator(ret => Me.HasAura(33943) && Me.Location.Distance(_safeLoc) > 5,
                            new Action(ret => Flightor.MoveTo(_safeLoc))),
                    new Decorator(ret => Me.HasAura(33943) && Me.Location.Distance(_safeLoc) <= 5,
                        new Sequence(
                            new Action(ret => QBCLog.Info("Removing Flight Form at Safe Place")),
                            new Action(ret => Lua.DoString("Dismount()")),
                            new Decorator(ret => Me.IsShapeshifted(),
                                new Action(ret => Lua.DoString("CancelShapeshiftForm()"))),
                            new Action(ret => WoWMovement.MoveStop()),
                            new Action(ret => Lua.DoString("RunMacroText('/run for i=1,  GetNumCompanions('MOUNT') do local flags = select(6, GetCompanionInfo('MOUNT', i)); if bit.band(flags, 0x2) ~= 0 then CallCompanion('MOUNT',i) return 1 end end return nil','0')"))
                            )),
                    new Decorator(ret => !Me.HasAura(33943) && !Me.IsQuestComplete(QuestId),
                        new Sequence(
                            new Decorator(ret => Me.Combat,
                                new Action(ret => Hologram().Use())),
                            new Decorator(ret => !Me.Mounted && ClosestBomb().Distance > 10,
                                new Action(ret => Lua.DoString("RunMacroText('/run for i=1,  GetNumCompanions('MOUNT') do local flags = select(6, GetCompanionInfo('MOUNT', i)); if bit.band(flags, 0x2) ~= 0 then CallCompanion('MOUNT',i) return 1 end end return nil','0')")))
                    )),
                    new Decorator(ret => ClosestBomb() == null,
                           new Action(ret => Flightor.MoveTo(_badBomb))),
                    FindBomb,
                    new Decorator(ret => Hostiles > 0,
                        new Action(r => Hologram().Use())
                    ),
                    UseAndGo,
                    new ActionAlwaysSucceed()
            )));
        }

        readonly WoWPoint _badBomb = new WoWPoint(-10561.68, -2429.371, 91.56037);
        readonly WoWPoint _safeLoc = new WoWPoint(-10644.76, -2211.07, 92.3418);

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