#region Usings
using System;
using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using System.Collections.Generic;
using Honorbuddy.QuestBehaviorCore;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.ShadowMoonValleyHook
{
    [CustomBehaviorFileName(@"Cava\ShadowMoonValleyHook")]
    // ReSharper disable once UnusedMember.Global
    public class ShadowMoonValleyHook : CustomForcedBehavior
    {
        public ShadowMoonValleyHook(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;
            _state = GetAttributeAsNullable<bool>("State", false, null, null) ?? false;
        }

        // ReSharper disable once NotAccessedField.Local
        private static bool _inserted;
        private readonly bool _state;
        public override bool IsDone { get { return true; } }
        private static Composite _hook;

        private static WoWUnit MobSwamplighterHive
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 82366 && !u.IsBlacklistedForInteraction()).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckMobSwamplighterHive()
        {
            if (MobSwamplighterHive == null) return false;
            QBCLog.Info("BlackListing Swamplighter Hive");
            Blacklist.Add(MobSwamplighterHive, BlacklistFlags.All, TimeSpan.FromSeconds(300000));
            return false;
        }

        private static Composite CreateHook()
        {
            return new ActionRunCoroutine(ctx => MyCoroutine());
        }

        static async Task<bool> MyCoroutine()
        {
            return await CheckMobSwamplighterHive();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting Shadow Moon Valley Hook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for Shadow Moon Valley Hook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing Shadow Moon Valley Hook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for Shadow Moon Valley Hook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
