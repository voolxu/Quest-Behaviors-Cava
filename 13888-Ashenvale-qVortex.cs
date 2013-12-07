using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

// ReSharper disable CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.QVortex
// ReSharper restore CheckNamespace
{
	[CustomBehaviorFileName(@"Cava\13888-Ashenvale-QVortex")]
    // ReSharper disable UnusedMember.Global
	public class QVortex : CustomForcedBehavior
    // ReSharper restore UnusedMember.Global
	{
		public QVortex(Dictionary<string, string> args)
			: base(args)
		{
			QuestId = 13888;
		}

	    private int QuestId { get; set; }
		private bool _isBehaviorDone;
		private Composite _root;
	    private static bool OnCooldown1 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(121);if b==0 then return 1 else return 0 end", 0) == 0; } }
	    private static bool OnCooldown2 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(122);if b==0 then return 1 else return 0 end", 0) == 0; } }
	    private static bool OnCooldown3 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(123);if b==0 then return 1 else return 0 end", 0) == 0; } }
	    private static bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		public override bool IsDone { get { return _isBehaviorDone; }}
	    private WoWUnit MobMagmathar { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where (u => u.Entry == 34295 && u.IsAlive && u.Distance < 80).OrderBy(u => u.Distance).FirstOrDefault(); }}
		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
			if (!IsDone)
			{
				if (TreeRoot.Current != null && TreeRoot.Current.Root != null && TreeRoot.Current.Root.LastStatus != RunStatus.Running)
				{
					var currentRoot = TreeRoot.Current.Root;
				    var composite = currentRoot as GroupComposite;
				    if (composite != null)
					{
						var root = composite;
						root.InsertChild(0, CreateBehavior());
					}
				}
				PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
				TreeRoot.GoalText = ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
			}
		}

	    private bool IsQuestComplete()
		{
			var quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
			return quest == null || quest.IsCompleted;
		}

	    private Composite DoneYet
		{
			get
			{
				return
					new Decorator(ret => IsQuestComplete(), new Action(delegate
					{
						if (InVehicle)
							Lua.DoString("CastPetAction(5)");
						_isBehaviorDone = true;
						return RunStatus.Success;
					}));
			}
		}

	    private Composite KillBoss
		{
			get
			{
				return new Decorator(context => MobMagmathar != null,
						new Action(context =>
						{
							MobMagmathar.Target();
						    if (!OnCooldown1)
						    {
						        Lua.DoString("CastPetAction(1)");
						        Thread.Sleep(500);
						    }
						    if (!OnCooldown2)
						    {
						        Lua.DoString("CastPetAction(2)");
                                Thread.Sleep(500);
						    }
                            if (!OnCooldown3)
						    {
						        Lua.DoString("CastPetAction(3)");
                                Thread.Sleep(500);
						    }
                            return RunStatus.Success;
						}));
			}
		}

		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
				new PrioritySelector(
					new Decorator(context => !InVehicle,
						new Action(context => { _isBehaviorDone = true; })),
					DoneYet,
					KillBoss,
					new ActionAlwaysSucceed())));
		}
	}
}
