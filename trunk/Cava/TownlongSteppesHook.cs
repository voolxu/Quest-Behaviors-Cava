using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;

#region Usings

using Bots.Grind;
using Styx.CommonBot.POI;
using System.Collections.Generic;
//using System.Linq;

using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespaces
namespace Honorbuddy.Quest_Behaviors.Cava.TownlongSteppesHook
{
    [CustomBehaviorFileName(@"Cava\TownlongSteppesHook")]
    public class TownlongSteppesHook : CustomForcedBehavior
    {
        public TownlongSteppesHook(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;
            _state = GetAttributeAsNullable<bool>("State", false, null, null) ?? false;
        }

        // ReSharper disable once NotAccessedField.Local
        private static bool _inserted;
        private static bool _needpoolofharmony;
        private readonly bool _state;
        public override bool IsDone { get { return true; } }
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private static Composite _tsHook;

        private static int Power
        {
            get { return Lua.GetReturnVal<int>("return UnitPower(\"player\", ALTERNATE_POWER_INDEX)", 0); }
        }

        private static readonly WoWPoint SafeLoc = new WoWPoint(1749.766, 2338.6, 377.429);
        public static WoWUnit FearStricken { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => (u.Entry == 62276 || u.Entry == 62281 || u.Entry == 62282 || u.Entry == 62277) && u.IsDead && u.HasAura(119073) && u.Distance<=5).OrderBy(u => u.Distance).FirstOrDefault(); } }


        static async Task<bool> Checkpower()
        {
            if (!_needpoolofharmony && Power >= 65)
            {
                QBCLog.Info("Moving To Pool of Armony");
                _needpoolofharmony = true;
            }
            return false;
        }

        static async Task<bool> ClickFearStricken()
        {
            if (FearStricken == null)
                return false;

            QBCLog.Info("Interacting with Fear-Stricken Sentinel");
            FearStricken.Target();
            FearStricken.Interact();
            return true;
        }

        static async Task<bool> Movepool()
        {
            if (!_needpoolofharmony)
                return false;

            if (Power <= 5)
            {
                _needpoolofharmony = false;
                return false;
            }

            if (!Navigator.AtLocation(SafeLoc))
                Navigator.MoveTo(SafeLoc);
            return true;
        }
        private static Composite CreateHook()
        {
            return new ActionRunCoroutine(ctx => MyCoroutine());
        }

        static async Task<bool> MyCoroutine()
        {
            if (!Me.IsAlive)
                return false;
            return await Checkpower() || await Movepool() || await ClickFearStricken();
        }
        //Type 'System.Threading.Tasks.Task<bool>' is not awaitable

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_tsHook == null)
                {
                    QBCLog.Info("Inserting TownlongSteppesHook");
                    _tsHook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _tsHook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for TownlongSteppesHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_tsHook != null)
                {
                    QBCLog.Info("Removing TownlongSteppesHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _tsHook);
                    _tsHook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for TownlongSteppesHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
