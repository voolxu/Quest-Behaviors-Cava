#region Usings
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.SpecificQuests.Cava.TheGreatBankHeist
{
	[CustomBehaviorFileName(@"Cava\14122-Kezan-TheGreatBankHeist")]
    // ReSharper disable once UnusedMember.Global
	public class Q14122 : CustomForcedBehavior
	{
		public Q14122(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;
			QuestId = 14122;
		}

		private int QuestId { get; set; }

		private bool _isBehaviorDone;
        private readonly WoWPoint _wp = new WoWPoint(-8366.285, 1726.376, 39.95956);
		private int _petAbilityIndex;

		private readonly string[] _bossWhisperIcons =
		{
			// Amazing G-Ray INV_Misc_EngGizmos_20.blp
			"INV_Misc_EngGizmos_20.blp",
			// Blastcrackers
			"INV_Misc_Bomb_07.blp",
			// Ear-O-Scope!
			"INV_Misc_Ear_NightElf_02.blp",
			// Infinifold Lockpick
			"INV_Misc_EngGizmos_swissArmy.blp",
			// Kaja'mite Drill
			"INV_Weapon_ShortBlade_21.blp"
		};


	    private static WoWGameObject Bank
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWGameObject>()
					.Where(ctx => ctx.Entry == 195449)
					.OrderBy(ctx => ctx.DistanceSqr).FirstOrDefault();
			}
		}
		private Composite _root;

		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new ActionRunCoroutine(ctx => MainCoroutine()));
		}

		async Task<bool> MainCoroutine()
		{
			if (_isBehaviorDone)
			{
				return false;
			}

			var quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
			if (quest.IsCompleted)
			{
				if (StyxWoW.Me.HasAura(67476))
					Lua.DoString("VehicleExit()");
				_isBehaviorDone = true;
				return true;
			}
            if (StyxWoW.Me.Location.DistanceSqr(_wp) > 5 * 5 && !StyxWoW.Me.HasAura(67476))
			{
				TreeRoot.StatusText = "Moving to location";
				Navigator.MoveTo(_wp);
				return true;
			}

			if (!StyxWoW.Me.HasAura(67476))
			{
				Bank.Interact();
				await Coroutine.Sleep((int)Delay.LagDuration.TotalMilliseconds);
				return true;
			}

			if (_petAbilityIndex > 0)
			{
				await Coroutine.Sleep((int)Delay.BeforeButtonClick.TotalMilliseconds);
				Lua.DoString("CastPetAction({0})", _petAbilityIndex);
				_petAbilityIndex = 0;
				return true;
			}

			return false;
		}

		public override bool IsDone
		{
			get { return (_isBehaviorDone); }
		}

		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
		    if (IsDone) return;
		    Lua.Events.AttachEvent("CHAT_MSG_RAID_BOSS_WHISPER", BossWhisperHandler);
		    this.UpdateGoalText(QuestId);
		}

		public override void OnFinished()
		{
			Lua.Events.DetachEvent("CHAT_MSG_RAID_BOSS_WHISPER", BossWhisperHandler);
			base.OnFinished();
		}

	    private void BossWhisperHandler(object sender, LuaEventArgs arg)
		{
			var msg = arg.Args[0].ToString();
			var match = _bossWhisperIcons.FirstOrDefault(msg.Contains);
			_petAbilityIndex = match != null ? (_bossWhisperIcons.IndexOf(match) + 1) : 0;
		}
	}
}
