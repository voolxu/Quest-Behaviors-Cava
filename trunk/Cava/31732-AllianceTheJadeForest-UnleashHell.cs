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
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.UnleashHell
{
    [CustomBehaviorFileName(@"Cava\31732-AllianceTheJadeForest-UnleashHell")]
    public class UnleashHell : CustomForcedBehavior
    {
        public UnleashHell(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 31732;
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
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();

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
            _doingQuestTimer.Start();
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_MainCombat());
                this.UpdateGoalText(QuestId);
            }
        }
        public WoWUnit MobHorde
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.IsAlive && (u.Entry == 66398 || u.Entry == 66399))
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }
        public WoWUnit MobShreder
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.IsAlive && u.Entry == 66397)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }
        public WoWUnit Explosives1
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 66530)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }
        public WoWUnit Explosives2
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(u => u.Entry == 66559)
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();
            }
        }


        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId) || _doingQuestTimer.ElapsedMilliseconds >= 180000,
                    new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        Lua.DoString("VehicleExit()");
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        private void Shoot(WoWUnit who)
        {
            //If the target is moving, the projectile is not instant
            WoWMovement.ClickToMove(who.IsMoving ? who.Location.RayCast(who.Rotation, 10f) : who.Location);
            //Fire pew pew
            if (CanUsePetButton(1))
            {
                Lua.DoString("CastPetAction({0})", 1);
            }
            if (CanUsePetButton(2))
            {
                Lua.DoString("CastPetAction({0})", 2);
            }
            
        }
        private bool CanUsePetButton(int index)
        {
            var lua = string.Format("if GetPetActionCooldown({0}) == 0 then return 1 end return 0", index);
            return Lua.GetReturnVal<int>(lua, 0) == 1;
        }


        public Composite KillObj1
        {
            get
            {
                return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 1) && MobHorde != null && Me.IsOnTransport,
                    new Action(r => Shoot(MobHorde)));
            }
        }
        public Composite KillObj2
        {
            get
            {
                return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 2) && MobShreder != null && Me.IsOnTransport,
                    new Action(r => Shoot(MobShreder)));
            }
        }
        public Composite KillObj3
        {
            get
            {
                return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 3) && Explosives1 != null && Me.IsOnTransport,
                    new Action(r => Shoot(Explosives1)));
            }
        }
        public Composite KillObj4
        {
            get
            {
                return new Decorator(r => !Me.IsQuestObjectiveComplete(QuestId, 4) && Explosives2 != null && Me.IsOnTransport,
                    new Action(r => Shoot(Explosives2)));
            }
        }

        protected Composite CreateBehavior_MainCombat()
        {
            return _root ?? (_root = 
                new Decorator(ret => !_isBehaviorDone,
                    new PrioritySelector(
                        DoneYet,
                        KillObj1,
                        KillObj2,
                        KillObj3,
                        KillObj4,
                        new ActionAlwaysSucceed()
            )));
        }
        #region Cleanup

        private bool _isDisposed;

        ~UnleashHell()
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



