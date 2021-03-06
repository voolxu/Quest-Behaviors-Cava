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

using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
//using Styx.CommonBot.Profiles.Quest;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Honorbuddy.QuestBehaviorCore;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.SpecificQuests.Cava.TheLastLineOfDefense
{
    [CustomBehaviorFileName(@"Cava\13086-Icecrow-TheLastLineOfDefense")]
    // ReSharper disable once UnusedMember.Global
    public class TheLastLineOfDefense : CustomForcedBehavior
    {
        private const uint ForgottenDepthsSlayerId = 30593;//kill 100
        private const uint FrostbroodDestroyerId = 30575;//kill 3
        private const double WeaponAzimuthMax = -0.10;
        private const double WeaponAzimuthMin = -0.67;
        private const double WeaponMuzzleVelocity = 600;
        private const double WeaponProjectileGravity = 100;
        private readonly VehicleWeapon _argentCannon;
        private WoWUnit _argentCannonunit;


        private WoWUnit ArgentCannon
        {
            get
            {
                if (!Query.IsViable(_argentCannonunit))
                {
                    _argentCannonunit = (Me.TransportGuid.IsValid)
                        ? ObjectManager.GetObjectByGuid<WoWUnit>(Me.TransportGuid)
                        : null;
                }

                return _argentCannonunit;
            }
        }
        
        private readonly uint[] _mobIds = {ForgottenDepthsSlayerId, FrostbroodDestroyerId};

        private bool _isBehaviorDone;

        private Composite _root;

        public TheLastLineOfDefense(Dictionary<string, string> args) : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            QuestId = 13086;
            var weaponArticulation = new WeaponArticulation(WeaponAzimuthMin, WeaponAzimuthMax);
            _argentCannon = new VehicleWeapon(1, weaponArticulation, WeaponMuzzleVelocity, WeaponProjectileGravity);
        }

        private int QuestId { get; set; }


        public override bool IsDone
        {
            get { return _isBehaviorDone; }
        }

        private static LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
        }


        private WoWUnit BestTarget
        {
            get
            {
                var activeMover = WoWMovement.ActiveMover;
                var myLoc = activeMover.Location;

                var myTarget = activeMover.CurrentTarget;
                
                if (myTarget != null && myTarget.Z < 405 && myTarget.IsAlive && _mobIds.Contains(myTarget.Entry) && myTarget.DistanceSqr > 50*50)
                    return myTarget;

                return (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                    where _mobIds.Contains(unit.Entry) && unit.IsAlive && unit.Z < 405
                    let distanceSqr = myLoc.DistanceSqr(unit.Location)
                    where distanceSqr > 50*50
                    orderby distanceSqr
                    select unit).FirstOrDefault();
            }
        }


        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_CombatMain());

                this.UpdateGoalText(QuestId);
            }
        }


        private Composite CreateBehavior_CombatMain()
        {
            return _root ??
                   (_root =
                       new Decorator(
                           ret => !_isBehaviorDone,
                           new PrioritySelector(CreateBehavior_CheckQuestCompletion(), CreateBehavior_ShootCatapult())));
        }


        private Composite CreateBehavior_CheckQuestCompletion()
        {
            return new Decorator(ret => Me.IsQuestComplete(QuestId) || !Me.IsOnTransport,
                new Action(ctx =>
                    {
                        Lua.DoString("VehicleExit()");
                        _isBehaviorDone = true;
                    }));
        }


        private Composite CreateBehavior_ShootCatapult()
        {
            WoWUnit target = null;
            return new PrioritySelector(ctx => target = BestTarget,
                new Decorator(ctx => target != null && _argentCannon.WeaponAim(target) && Me.IsOnTransport,
                    new Action(ctx =>
                        //_argentCannon.WeaponFire())),
                        Lua.DoString("RunMacroText('/click OverrideActionBarButton1','0')"))),
                new Action(context =>
                {
                    if (ArgentCannon.ManaPercent >= 90)
                    {
                        StyxWoW.Sleep(1000);
                        Lua.DoString("CastPetAction(2)"); 
                       // OverrideActionBarButton2
                       
                    }
                    if (ArgentCannon.HealthPercent <= 5)
                    {
                        Lua.DoString("VehicleExit()");
                        _isBehaviorDone = true;
                    }
                    if (!Me.IsOnTransport)
                    {
                        _isBehaviorDone = true;
                    }

                })
            );
        }

           #region Cleanup

        private bool _isDisposed;

        ~TheLastLineOfDefense()
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