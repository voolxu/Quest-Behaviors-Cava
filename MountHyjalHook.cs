using Styx.Pathing;

#region Usings

using System.Threading.Tasks;
using CommonBehaviors.Actions;
using System.Collections.Generic;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.MountHyjalHook
{
    [CustomBehaviorFileName(@"Cava\MountHyjalHook")]
    public class BehaviorHook : CustomForcedBehavior
    {
        public BehaviorHook(Dictionary<string, string> args)
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


        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> RunToCorpse()
        {
            if (Me.IsGhost && (StyxWoW.Me.Location.Distance(StyxWoW.Me.CorpsePoint) >= 40))
            {
                QBCLog.Info("Running To Corpse");
                Navigator.MoveTo(StyxWoW.Me.CorpsePoint);
                return true;
            }
            return false;
        }

        private static Composite CreateHook()
        {
            return new ActionRunCoroutine(ctx => MyCoroutine());
        }

        static async Task<bool> MyCoroutine()
        {
            if (Me.IsAlive)
                return false;
            return await RunToCorpse();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting MountHyjalHook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Death_CorpseRun", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for MountHyjalHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing MountHyjalHook");
                    TreeHooks.Instance.RemoveHook("Death_CorpseRun", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for MountHyjalHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
