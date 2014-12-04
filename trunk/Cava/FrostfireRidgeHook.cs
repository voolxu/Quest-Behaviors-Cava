using System;
using Buddy.Coroutines;
using Styx.CommonBot;

#region Usings

using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using System.Collections.Generic;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.FrostfireRidgeHook
{
    [CustomBehaviorFileName(@"Cava\FrostfireRidgeHook")]
    public class FrostfireRidgeHook : CustomForcedBehavior
    {
        public FrostfireRidgeHook(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;
            _state = GetAttributeAsNullable<bool>("State", false, null, null) ?? false;
        }

        // ReSharper disable once NotAccessedField.Local
        private static bool _inserted;
        private readonly bool _state;
        public override bool IsDone { get { return true; } }
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private static Composite _hook;
        private static WoWObject IronHordeCannon { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 222870 && o.DynamicFlags == -65278 && o.CanUseNow())); } }
        private static WoWObject UsedIronHordeCannon { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 222870 && o.Distance < 100 && o.DynamicFlags != -65278 && !o.IsBlacklistedForInteraction())); } }
        private static WoWItem ItemBlackrockBlastingPowder { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 104039)); } }



        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckHasAura()
        {
            if (Me.HasAura(173611) || Me.HasAura(161918))
            {
                QBCLog.Debug("Harmfull aura detected. Movement Direction: back");
                WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
                await Coroutine.Sleep(500);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
            }
            return false;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckIronHordeCannon()
        {
            if (IronHordeCannon != null && ItemBlackrockBlastingPowder != null)
            {
                QBCLog.Info("Interaction With Iron Horde Cannon");
                WoWMovement.MoveStop();
                IronHordeCannon.Interact();
                return true;
            }
            if (UsedIronHordeCannon != null)
            {
                QBCLog.Info("Blacklisting Used Iron Horde Cannon");
                Blacklist.Add(UsedIronHordeCannon, BlacklistFlags.Interact, TimeSpan.FromSeconds(600000), "Blacklisting Used Iron Horde Cannon");
                return false;
            }

            return false;
        }

        private static Composite CreateHook()
        {
            return new ActionRunCoroutine(ctx => MyCoroutine());
        }

        static async Task<bool> MyCoroutine()
        {
            if (!Me.IsAlive)
                return false;
            return await CheckHasAura() || await CheckIronHordeCannon();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting Frostfire Ridge Hook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for Frostfire Ridge Hook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing Frostfire Ridge Hook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for Frostfire Ridge Hook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
