
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
namespace Honorbuddy.Quest_Behaviors.Cava.Winterspring
{
    [CustomBehaviorFileName(@"Cava\WinterspringHook")]
    public class WinterspringHook : CustomForcedBehavior
    {
        public WinterspringHook(Dictionary<string, string> args)
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

        public static WoWUnit MobSolidIce
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 49233 && u.Distance < 4).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> HitSolidIce()
        {
            if (MobSolidIce != null && Me.QuestLog.GetQuestById(28632) != null && !Me.QuestLog.GetQuestById(28632).IsCompleted)
            {
                QBCLog.Info("Interacting with Solid Ice");
                MobSolidIce.Face();
                MobSolidIce.Interact();
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
            return await HitSolidIce();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting WinterspringHook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for WinterspringHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing WinterspringHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for WinterspringHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
