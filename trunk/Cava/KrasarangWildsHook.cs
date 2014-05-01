
#region Usings

using System.Collections.Generic;
using System.Linq;

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


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.KrasarangWildsHook
{
    [CustomBehaviorFileName(@"Cava\KrasarangWildsHook")]
    public class KrasarangWildsHook : CustomForcedBehavior
    {
        public KrasarangWildsHook(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;
            _state = GetAttributeAsNullable<bool>("State", false, null, null) ?? false;
        }

        // ReSharper disable once NotAccessedField.Local
        private bool _inserted;
        private readonly bool _state;
		public override bool IsDone { get { return true; } }
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private static Composite _kdHook;

        public static WoWUnit KoroMistwalker
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 58978 && u.IsAlive && u.HasAura(140812)).OrderBy(u => u.Distance).FirstOrDefault(); }
        }


        private static Composite CreateHook()
        {
            return
                new Decorator(r => Me.QuestLog.GetQuestById(30269) != null && !Me.QuestLog.GetQuestById(30269).IsCompleted && KoroMistwalker != null,
                    new Action(r =>
                    {
                        QBCLog.Info("Removing Arrow from Koro Mistwalker");
                        if (KoroMistwalker.Location.Distance(Me.Location) > 5)
                            Navigator.MoveTo(KoroMistwalker.Location);
                        if (!(KoroMistwalker.Location.Distance(Me.Location) <= 5)) return;
                        KoroMistwalker.Target();
                        StyxWoW.Sleep(1000);
                        KoroMistwalker.Face();
                        StyxWoW.Sleep(1000);
                        KoroMistwalker.Interact();
                    }));
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (_state)
            {
                if (_kdHook == null)
                {
                    QBCLog.Info("Inserting KrasarangWildsHook");
                    _kdHook = CreateHook();
                    TreeHooks.Instance.InsertHook("Questbot_Main", 0, _kdHook);
                }
                else
                {
                    QBCLog.Info("Insert was requested for KrasarangWildsHook, but was already present");
                }

                _inserted = true;
            }

            else
            {
                if (_kdHook != null)
                {
                    QBCLog.Info("Removing KrasarangWildsHook");
                    TreeHooks.Instance.RemoveHook("Questbot_Main", _kdHook);
                    _kdHook = null;
                }
                else
                {
                    QBCLog.Info("Remove was requested for KrasarangWildsHook, but hook was not present");
                }

                _inserted = false;
            }
        }
    }
}
