using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


namespace Honorbuddy.Quest_Behaviors.Cava.QVortex
{
    [CustomBehaviorFileName(@"Cava\13888-Ashenvale-qVortex")]
    public class _13888:CustomForcedBehavior
    {
        public _13888(Dictionary<string, string> Args)
            : base(Args)
        {
            QuestId = GetAttributeAsNullable<int>("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
        }

        public int QuestId { get; set; }
        public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
        public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
        private bool IsBehaviorDone = false;
        private Composite _root;
        public List<WoWUnit> q13888_magmathar
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 34295 && !StyxWoW.Me.IsDead)).OrderBy(ret => ret.Distance).ToList();
            }
        }
        public List<WoWUnit> q13888_vehicle
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 34222 && !StyxWoW.Me.IsDead)).OrderBy(ret => ret.Distance).ToList();
            }
        }
        public override bool IsDone
        {
            get
            {
                return (IsBehaviorDone);
            }
        }
        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                PlayerQuest Quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
                TreeRoot.GoalText = ((Quest != null) ? ("\"" + Quest.Name + "\"") : "In Progress");
            }
        }
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(
                        ret => q13888_magmathar[0].IsAlive,
                        new PrioritySelector(
                            new Decorator(
                                ret => StyxWoW.Me.CurrentTarget != q13888_magmathar[0],
                                new Sequence(
                                    new Action(ret => 
                                    {
							            if (q13888_magmathar.Count > 0 && q13888_magmathar[0].Location.Distance(StyxWoW.Me.Location) > 100)
							            {
								            Thread.Sleep(1000);
							            }
							            if (q13888_magmathar.Count > 0 && (q13888_magmathar[0].Location.Distance(StyxWoW.Me.Location) <= 100))
							            {
                                            while (!StyxWoW.Me.QuestLog.GetQuestById(13888).IsCompleted)
                                            {
                                                q13888_magmathar[0].Face();
                                                q13888_magmathar[0].Target();
                                                Lua.DoString("CastPetAction(1)");
                                                Thread.Sleep(200);
                                                Lua.DoString("CastPetAction(2)");
                                                Thread.Sleep(200);
                                                Lua.DoString("CastPetAction(3)");
                                                Thread.Sleep(200);
                                            }
							            }
                                    }))))),
                     new Decorator(
                         ret => StyxWoW.Me.QuestLog.GetQuestById(13888).IsCompleted,
                         new Sequence(
                             new Action(ret => Lua.DoString("CastPetAction(5)")),
                             new Action(ret => IsBehaviorDone = true)))
                    ));

        }
    }
}
