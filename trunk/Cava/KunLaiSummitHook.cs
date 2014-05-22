
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
namespace Honorbuddy.Quest_Behaviors.Cava.KunLaiSummitHook
{
    [CustomBehaviorFileName(@"Cava\KunLaiSummitHook")]
    public class KunLaiSummitHook : CustomForcedBehavior
    {
        public KunLaiSummitHook(Dictionary<string, string> args)
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
        private static Composite _klsHook;

        private static bool IsObjectiveComplete(int objectiveId, uint questId)
        {
            if (Me.QuestLog.GetQuestById(questId) == null)
            {
                return false;
            }
            var returnVal = Lua.GetReturnVal<int>(string.Format("return GetQuestLogIndexByID({0})", questId), 0);
            return
                Lua.GetReturnVal<bool>(string.Format("return GetQuestLogLeaderBoard({0},{1})", objectiveId, returnVal),
                    2);
        }

        private static WoWObject ShrineBody { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(ret => ret.Entry == 211597 && ret.Distance <= 6)); } }
        private static WoWObject ShrineBreath { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(ret => ret.Entry == 211601 && ret.Distance <= 6)); } }
        private static WoWObject ShrineHeart { get { return (ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(ret => ret.Entry == 211602 && ret.Distance <= 6)); } }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckShrineBody()
        {
            if ((ShrineBody != null && Me.QuestLog.GetQuestById(330684) != null && !IsObjectiveComplete(1, 30684)) || (ShrineBody != null && Me.QuestLog.GetQuestById(31306) != null && !IsObjectiveComplete(1, 31306)))
            {
                QBCLog.Info("Interacting with Shrine of the Seeker's Body");
                WoWMovement.MoveStop();
                ShrineBody.Interact();
            }
            return false;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckShrineBreath()
        {
            if ((ShrineBreath != null && Me.QuestLog.GetQuestById(330684) != null && !IsObjectiveComplete(2, 30684)) || (ShrineBreath != null && Me.QuestLog.GetQuestById(31306) != null && !IsObjectiveComplete(2, 31306)))
            {
                QBCLog.Info("Interacting with Shrine of the Seeker's Breath");
                WoWMovement.MoveStop();
                ShrineBreath.Interact();
            }
            return false;
        }

        // ReSharper disable once CSharpWarnings::CS1998
        static async Task<bool> CheckShrineHeart()
        {
            if ((ShrineHeart != null && Me.QuestLog.GetQuestById(330684) != null && !IsObjectiveComplete(3, 30684)) || (ShrineHeart != null && Me.QuestLog.GetQuestById(31306) != null && !IsObjectiveComplete(3, 31306)))
            {
                QBCLog.Info("Interacting with Shrine of the Seeker's Breath");
                WoWMovement.MoveStop();
                ShrineHeart.Interact();
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
            return await CheckShrineBody() || await CheckShrineBreath() || await CheckShrineHeart();
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_klsHook == null)
                {
                    QBCLog.Info("Inserting KunLaiSummitHook");
                    _klsHook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _klsHook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for KunLaiSummitHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_klsHook != null)
                {
                    QBCLog.Info("Removing KunLaiSummitHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _klsHook);
                    _klsHook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for KunLaiSummitHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
