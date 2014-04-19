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
namespace Honorbuddy.Quest_Behaviors.Cava.InDefenseofKromgarFortress2
{
    [CustomBehaviorFileName(@"Cava\26058-Stonetalon-InDefenseofKromgarFortress")]
    public class Q26058 : CustomForcedBehavior
    {
        private const uint TurretId = 41895;

        private const double WeaponAzimuthMax = 0.4363;
        private const double WeaponAzimuthMin = -0.03491;
        private const double WeaponMuzzleVelocity = 1000;
        private const int QuestId = 26058;
        private readonly WoWPoint _questLocation = new WoWPoint(923.4741, -0.8692548, 92.59513);
        private readonly LocalBlacklist _targetBlacklist = new LocalBlacklist(TimeSpan.FromSeconds(30));

        private bool _isDone;
        private Composite _root;

        public Q26058(Dictionary<string, string> args) : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;
        }

        private VehicleWeapon WeaponFireCannon { get; set; }

        private LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();

        public WoWUnit BestTarget
        {
            get
            {
                var myCurrentTargat = Me.CurrentTarget;
                var myLoc = StyxWoW.Me.Location;
                if (IsValidTarget(myCurrentTargat))
                {
                    var targetLoc = myCurrentTargat.Location;
                    if (IsInPosition(targetLoc, myLoc))
                        return myCurrentTargat;
                }

                return (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                    where IsValidTarget(unit)
                    let loc = unit.Location
                    where IsInPosition(loc, myLoc)
                    orderby myLoc.DistanceSqr(loc)
                    select unit).FirstOrDefault();
            }
        }

        #region Overrides

        public override bool IsDone
        {
            get { return _isDone; }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_CombatMain());

                var weaponArticulation = new WeaponArticulation(WeaponAzimuthMin, WeaponAzimuthMax);
                WeaponFireCannon = new VehicleWeapon(1, weaponArticulation, WeaponMuzzleVelocity);

                this.UpdateGoalText(QuestId);
            }
        }

        private Composite CreateBehavior_CombatMain()
        {
            WoWUnit turret = null;
            WoWUnit selectedTarget = null;
            return _root ??
                   (_root = new Decorator(ctx => !IsDone,
                       new PrioritySelector(
                           new Decorator(ret => Me.IsQuestComplete(QuestId),
                               new Sequence(
                                   new Action(ret => TreeRoot.StatusText = "Finished!"),
                                   new Action(ret => Lua.DoString("VehicleExit()")),
                                   new Action(ctx => _isDone = true)
                           )),
                           new Decorator(ret => _doingQuestTimer.ElapsedMilliseconds >= 60000,
                                new Sequence(
                                    new Action(ret => _doingQuestTimer.Restart()),
                                    new Action(ret => Lua.DoString("VehicleExit()")),
                                    new Sleep(4000)
                           )),
                             new Decorator(ret => !Query.IsInVehicle(),
                               new PrioritySelector(ctx => turret = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == TurretId),
                                   new Decorator(ctx => turret == null,
                                       new Action(ctx => Navigator.MoveTo(_questLocation))),
                                   new Decorator(ctx => !turret.WithinInteractRange,
                                       new Action(ctx => Navigator.MoveTo(turret.Location))),
                                   new Decorator(ctx => turret.WithinInteractRange,
                                       new PrioritySelector(
                                           new Decorator(ctx => StyxWoW.Me.IsMoving,
                                               new Action(ctx => WoWMovement.MoveStop())),
                                           new Action(ctx => turret.Interact())
                           )))),
                           new PrioritySelector(ctx => selectedTarget = BestTarget,
                               new Decorator(ctx => selectedTarget != null,
                                   new PrioritySelector(
                                       new Decorator(ctx => selectedTarget.Guid != Me.CurrentTargetGuid,
                                           new Action(ctx => shoot(selectedTarget))),
                                       // Aim & Fire at the selected target...
                                       new Sequence(
                                           new Action(context =>
                                               {
                                                   // If weapon aim cannot address selected target, blacklist target for a few seconds...
                                                   if (!WeaponFireCannon.WeaponAim(selectedTarget))
                                                   {
                                                       _targetBlacklist.Add(selectedTarget, TimeSpan.FromSeconds(5));
                                                       return RunStatus.Failure;
                                                   }

                                                   // If weapon could not be fired, wait for it to become ready...
                                                   if (!WeaponFireCannon.WeaponFire())
                                                   {
                                                       return RunStatus.Failure;
                                                   }

                                                   return RunStatus.Success;
                                               }),
                                           new WaitContinue(
                                               Delay.AfterWeaponFire,
                                               context => false,
                                               new ActionAlwaysSucceed()
                   ))))))));
        }

        #endregion

        private bool IsValidTarget(WoWUnit unit)
        {
            return unit != null && !_targetBlacklist.Contains(unit) && !unit.IsDead &&
                   (unit.Entry == 42017 || unit.Entry == 42016 || unit.Entry == 42015);
        }

        private bool IsInPosition(WoWPoint unitLoc, WoWPoint myLoc)
        {
            return unitLoc.X > 935 && unitLoc.Y > 5 && unitLoc.Z >= myLoc.Z;
        }

        private void shoot(WoWUnit who)
        {
            var v = who.Location - StyxWoW.Me.Transport.Location;
            v.Normalize();
            Lua.DoString(string.Format("local pitch = {0}; local delta = pitch - VehicleAimGetAngle(); VehicleAimIncrement(delta);", Math.Asin(v.Z)));

            //If the target is moving, the projectile is not instant
            if (who.IsMoving)
            {
                WoWMovement.ClickToMove(who.Location.RayCast(who.Rotation, 20f));
            }
            else
            {
                WoWMovement.ClickToMove(who.Location);
            }
            //Fire pew pew
            Lua.DoString("CastPetAction({0})", 1);
        }

        #region Cleanup

        private bool _isDisposed;

        ~Q26058()
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
                    TreeHooks.Instance.RemoveHook("Combat_Main", CreateBehavior_CombatMain());
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