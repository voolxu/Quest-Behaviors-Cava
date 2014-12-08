using Buddy.Coroutines;
using Styx.CommonBot;
using Styx.Pathing;

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
    [CustomBehaviorFileName(@"Cava\Wod-GorgrondHook")]
    // ReSharper disable once UnusedMember.Global
    public class GorgrondHook : CustomForcedBehavior
    {
        public GorgrondHook(Dictionary<string, string> args)
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
        private static readonly WoWPoint SafeLocA = new WoWPoint(6154.936, 1441.654, 42.25444);
        private static readonly WoWPoint SafeLocB = new WoWPoint(6492.491, 1318.957, 45.0764);
        private static readonly WoWPoint SafeLocC = new WoWPoint(6879.865, 1040.616, 68.42307);
        private static readonly WoWPoint SafeLocD = new WoWPoint(7147.883, 1588.083, 81.71139);
        private static readonly WoWPoint SafeLocE = new WoWPoint(6232.965, 766.1824, 99.63248);

        private static WoWUnit CraterLordIgneous { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 81528 && u.IsTargetingMeOrPet).OrderBy(u => u.Distance).FirstOrDefault(); } }
        private static WoWObject OgreBarricade { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.State.ToString() == "Ready" && o.Entry == 227114 && o.Distance < 3)); } }
        private static WoWItem ItemGorenDisguise { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 112958)); } }
        private static WoWUnit GrotheUncreator { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 81251 && u.IsTargetingMeOrPet).OrderBy(u => u.Distance).FirstOrDefault(); } }
        private static WoWUnit KhargaxtheDevourer { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 81537 && u.IsTargetingMeOrPet).OrderBy(u => u.Distance).FirstOrDefault(); } }


        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckHasAura()
        {
            if (Me.HasAura(158238))
            {
                QBCLog.Debug("Harmfull aura detected. Movement Direction: back");
                WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
                await Coroutine.Sleep(500);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
            }
            return false;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckKhargaxtheDevourer()
        {
            if (KhargaxtheDevourer == null) return false;
            QBCLog.Info("Moving Away from Khargax the Devourer");
            Navigator.MoveTo(SafeLocE);
            return true;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckCraterLordIgneous()
        {
            if (CraterLordIgneous == null) return false;
            QBCLog.Info("Moving Away from Crater Lord Igneous");
            if (Me.Location.Distance(SafeLocA) < Me.Location.Distance(SafeLocB))
            {
                if (!(Me.Location.Distance(SafeLocA) > 5)) return false;
                Navigator.MoveTo(SafeLocA);
                return true;
            }
            if (!(Me.Location.Distance(SafeLocB) > 5)) return false;
            Navigator.MoveTo(SafeLocB);
            return true;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckGrotheUncreator()
        {
            if (GrotheUncreator == null) return false;
            QBCLog.Info("Moving Away from Gro the Uncreator");
            if (Me.Location.Distance(SafeLocC) < Me.Location.Distance(SafeLocD))
            {
                if (!(Me.Location.Distance(SafeLocC) > 5)) return false;
                Navigator.MoveTo(SafeLocC);
                return true;
            }
            if (!(Me.Location.Distance(SafeLocD) > 5)) return false;
            Navigator.MoveTo(SafeLocD);
            return true;
        }
        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckForOgreBarricade()
        {
            if (OgreBarricade == null) return false;
            QBCLog.Info("Removing Ogre Barricade from my way");
            WoWMovement.MoveStop();
            OgreBarricade.Interact();
            return true;
        }

        static async Task<bool> CheckForGorenDisguise()
        {
            if ((Me.QuestLog.GetQuestById(36209) == null && Me.QuestLog.GetQuestById(35041) == null) || Me.HasAura(164332) || ItemGorenDisguise == null)
                return false;
            QBCLog.Info("Using Item Goren Disguise to apply Mask");
            WoWMovement.MoveStop();
            // ReSharper disable once ObjectCreationAsStatement
            new Mount.ActionLandAndDismount();
            Lua.DoString("UseItemByName(112958)");
            await Coroutine.Sleep(1000);
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
            return await CheckCraterLordIgneous() || await CheckForOgreBarricade() || await CheckForGorenDisguise() || await CheckGrotheUncreator() || await CheckKhargaxtheDevourer() || await CheckHasAura();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_hook == null)
                {
                    QBCLog.Info("Inserting Gorgrond Hook");
                    _hook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _hook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for Gorgrond Hook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_hook != null)
                {
                    QBCLog.Info("Removing Gorgrond Hook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _hook);
                    _hook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for Gorgrond Hook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
