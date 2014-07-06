
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
namespace Honorbuddy.Quest_Behaviors.Cava.TheLostIslesHook
{
    [CustomBehaviorFileName(@"Cava\TheLostIslesHook")]
    public class TheLostIslesHook : CustomForcedBehavior
    {
        public TheLostIslesHook(Dictionary<string, string> args)
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

        public static WoWUnit MobDocZapnozzle
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 36608 && u.Distance < 5).OrderBy(u => u.Distance).FirstOrDefault(); }
        }
        public static WoWUnit GoblinZombie
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 38753 && u.Distance < 15 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); }
        }
        public static WoWUnit OostanHeadhunter
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 38811 && u.Distance < 30 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); }
        }
        public static WoWUnit LittlePygmie1
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 38808 && u.Distance < 30 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); }
        }
        public static WoWUnit LittlePygmie2
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 38809 && u.Distance < 30 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); }
        }
        public static WoWUnit LittlePygmie3
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 38810 && u.Distance < 30 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        
        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckStuckBody()
        {
            if (Me.HasAura(69010) && MobDocZapnozzle != null)
            {
                QBCLog.Info("Interacting with King Genn Greymane");
                MobDocZapnozzle.Interact();
                Lua.DoString("RunMacroText('/click QuestFrameCompleteQuestButton')");
            }
            return false;
        }
        static async Task<bool> CheckNeedUseRocketBoots()
        {
            if (!Me.HasAura(72887) && GoblinZombie != null)
            {
                QBCLog.Info("Using Super Booster Rocket Boots");
                WoWMovement.MoveStop();
                Lua.DoString("UseItemByName(52013)");
            }
            if (Me.HasAura(72887) && GoblinZombie == null && (OostanHeadhunter != null || LittlePygmie1 != null || LittlePygmie2 != null || LittlePygmie3 != null))
            {
                QBCLog.Info("Removing Super Booster Rocket Boots");
                Lua.DoString("UseItemByName(52013)");
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
            return await CheckStuckBody() || await CheckNeedUseRocketBoots();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting TheLostIslesHook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for TheLostIslesHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing TheLostIslesHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for TheLostIslesHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
