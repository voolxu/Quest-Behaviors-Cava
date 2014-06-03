
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
namespace Honorbuddy.Quest_Behaviors.Cava.GilneasHook
{
    [CustomBehaviorFileName(@"Cava\GilneasHook")]
    public class GilneasHook : CustomForcedBehavior
    {
        public GilneasHook(Dictionary<string, string> args)
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

        private static readonly WoWPoint Stuckloc = new WoWPoint(-1791.174, 1400.276, 20.29372);
        public static WoWUnit MobKingGennGreymane
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 36332 && u.Distance < 5).OrderBy(u => u.Distance).FirstOrDefault(); }
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckStuckBody()
        {
            if (Me.HasAura(68631) && MobKingGennGreymane != null)
            {
                QBCLog.Info("Interacting with King Genn Greymane");
                MobKingGennGreymane.Interact();
                Lua.DoString("RunMacroText('/click QuestFrameCompleteQuestButton')");
            }
            return false;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckStuckLocal()
        {
            if (Me.Location.Distance(Stuckloc) <= 4)
            {
                QBCLog.Debug("Stuck. Unstuck Movement Direction: Left");
                WoWMovement.Move(WoWMovement.MovementDirection.StrafeLeft);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.StrafeLeft);
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
            return await CheckStuckBody() || await CheckStuckLocal();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting GilneasHook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for GilneasHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing GilneasHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for GilneasHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
