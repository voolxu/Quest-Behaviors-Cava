
#region Summary and Documentation
#endregion


#region Examples
#endregion

using System;

#region Usings

using CommonBehaviors.Actions;
using Styx.Pathing;
using System.Diagnostics;
using Styx.CommonBot;
using System.Collections.Generic;
using System.Linq;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.SpecificQuests.CheerUpYiMo
{
    [CustomBehaviorFileName(@"Cava\30082-KrasarangWilds-CheerUpYiMo")]
	public class Quest30082 : CustomForcedBehavior
	{
		public Quest30082(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;

			QuestId = 30082;
		}
		private int QuestId { get; set; }

        private readonly Stopwatch _doingQuestTimer = new Stopwatch();
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private WoWUnit StartNPC { get { return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(r => r.Entry == 58376); } }
		private readonly WoWPoint _startLocal = new WoWPoint(-322.8219, -865.1127, 119.8495);
        private WoWUnit RollNPC { get { return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(r => r.Entry == 57310); } }

        // Private variables for internal state
        private Composite _root;
        private bool _isBehaviorDone;
        private bool IsOnFinishedRun { get; set; }


        //private bool _isDisposed;
        //private bool _isBehaviorDone;

        //private bool IsBehaviorDone;
		//

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    DoneYet,
                    FindStartit,
                    Startit,
                    Rollit,
                    new ActionAlwaysSucceed()
                    ));
        }

        public Composite Rollit
        {
            get
            {
                return new Decorator(ret => RollNPC != null && !Me.Combat,
                    new Action(delegate
                    {
                        var rollLocal = RollNPC.Location;
                        rollLocal.X = rollLocal.X + (float) 0.500;
                        rollLocal.Y = rollLocal.Y - (float) 2.420;
                        FindWoWPointHeightZ(ref rollLocal);
                        if (rollLocal.Distance(Me.Location) > 2)
                        {
                            //TreeRoot.StatusText = "npc at " + RollNPC.Location;
                            //TreeRoot.StatusText = "moving to " + rollLocal;
                            //TreeRoot.StatusText = "distance is " + rollLocal.Distance(Me.Location);
                            Navigator.MoveTo(rollLocal);
                        }
                        if (!(rollLocal.Distance(Me.Location) <= 2)) return;
                        WoWMovement.MoveStop();
                        if (Me.IsMounted()) Lua.DoString("Dismount()");
                        if (Me.IsShapeshifted()) Lua.DoString("CancelShapeshiftForm()");
                        RollNPC.Target();
                        RollNPC.Face();
                        RollNPC.Interact();
                        StyxWoW.Sleep(2000);
                    }));
            }
        }
        public Composite Startit
        {
            get
            {
                return new Decorator(ret => RollNPC == null && StartNPC != null && !Me.Combat,
                    new Action(delegate
                    {
                        if (StartNPC.Location.Distance(Me.Location) > 5)
                        {
                            Navigator.MoveTo(StartNPC.Location);
                        }
                        if (!(StartNPC.Location.Distance(Me.Location) <= 5)) return;
                        WoWMovement.MoveStop();
                        if (Me.IsMounted()) Lua.DoString("Dismount()");
                        if (Me.IsShapeshifted()) Lua.DoString("CancelShapeshiftForm()");
                        StartNPC.Target();
                        StartNPC.Face();
                        StartNPC.Interact();
                        Lua.DoString("SelectGossipOption(1)");
                        StyxWoW.Sleep(1500);
                        Lua.DoString("SelectGossipOption(2)");
                        StyxWoW.Sleep(2000);
                        Me.ClearTarget();
                    }));
            }
        }
        public Composite FindStartit
        {
            get
            {
                return new Decorator(ret => RollNPC == null && StartNPC == null && !Me.Combat,
                    new Action(delegate
                    {
                        Navigator.MoveTo(_startLocal);
                        WoWMovement.MoveStop();
                        if (StartNPC == null)
                        {
                            StyxWoW.Sleep(10000);
                        }
                    }));
            }
        }

        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId) || _doingQuestTimer.ElapsedMilliseconds >= 180000,
                    new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }
        private void FindWoWPointHeightZ(ref WoWPoint wowPoint)
        {
            var heights = Navigator.FindHeights(wowPoint.X, wowPoint.Y);
            if (heights == null || !heights.Any())
                return;
            var tmpZ = wowPoint.Z;
            wowPoint.Z = heights.OrderBy(h => Math.Abs(h - tmpZ)).FirstOrDefault();
        }
        
        #region Overrides of CustomForcedBehavior

        public override bool IsDone
        {
            get { return _isBehaviorDone; }
        }
        public override void OnFinished()
        {
            if (IsOnFinishedRun)
                { return; }
            TreeRoot.GoalText = string.Empty;
            TreeRoot.StatusText = string.Empty;
            base.OnFinished();
            IsOnFinishedRun = true;
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();
            if (!IsDone)
            {
                this.UpdateGoalText(QuestId);
            }
        }
        #endregion
    }
}
