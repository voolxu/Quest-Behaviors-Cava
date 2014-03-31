using System;
using System.Collections.Generic;
//using System.Globalization;
//using System.Diagnostics;
using System.Linq;
//using System.Threading;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;

	/* This behavior is for killing Thane noobface in Grizzly Hills (Horde 12259 and Alliance 12255) 
		This behavior was developed by Kickazz006
		Code was taken from Shak
		How I used it in this behavior was chop each in half and take the bits that I needed
		Feel free to re-use the code to your liking (anyone else)
	*/


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.KillTheThaneofVoldrune
{
    [CustomBehaviorFileName(@"Cava\KillTheThaneofVoldrune")]
    public class TheThaneofVoldrune : CustomForcedBehavior
    {
        public TheThaneofVoldrune(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                QuestId = GetAttributeAsNullable("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0; Location = WoWPoint.Empty;
                Endloc = WoWPoint.Empty;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }
        public WoWPoint Location { get; private set; }
		public WoWPoint Endloc { get; private set; }
        public int QuestId { get; private set; }
		public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }
        public static LocalPlayer Me = StyxWoW.Me;
        static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
        readonly WoWPoint _endloc = new WoWPoint(2918.578, -2560.254, 116.1465);
        readonly WoWPoint _startloc = new WoWPoint(2956.376, -2538.126, 129.1578);
        readonly WoWPoint _flyloc = new WoWPoint(2788.155, -2508.851, 56.05595);

        #region Overrides of CustomForcedBehavior
		public List<WoWUnit> Objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 27377 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Flylist
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 27292 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(ret => Me.QuestLog.GetQuestById((uint)QuestId) != null && Me.QuestLog.GetQuestById((uint)QuestId).IsCompleted,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                    })))),
                     new Decorator(ret => !InVehicle,
                         new Action(ret =>
						{
							if (Flylist.Count == 0)
							{
								Navigator.MoveTo(_flyloc);
                                StyxWoW.Sleep(1000);
							}
							if (Flylist.Count > 0 && Flylist[0].Location.Distance(Me.Location) > 5)
							{
								Navigator.MoveTo(Flylist[0].Location);
                                StyxWoW.Sleep(1000);
							}
							if (Flylist.Count > 0 && Flylist[0].Location.Distance(Me.Location) <= 5)
							{
								WoWMovement.MoveStop();
								Flylist[0].Interact();
                                StyxWoW.Sleep(1000);
								Lua.DoString("SelectGossipOption(1)");
                                StyxWoW.Sleep(1000);
							}
						})),
					new Decorator(ret => InVehicle,
						new Action(ret =>
						{
                            if (!InVehicle)
                                return RunStatus.Success;
                            if (Me.QuestLog.GetQuestById((uint)QuestId).IsCompleted)
                            {
                                if (Me.Location.Distance(_endloc) > 10)
                                {
                                    WoWMovement.ClickToMove(_endloc);
                                    StyxWoW.Sleep(1000);
                                }
                                Lua.DoString("VehicleExit()");
                                return RunStatus.Success;
                            }
                            if (Objmob.Count == 0)
                            {
                                WoWMovement.ClickToMove(_startloc);
                                StyxWoW.Sleep(1000);
                            }
                            if (Objmob.Count > 0 && (Objmob[0].Location.Distance(Me.Location) > 50 || !Objmob[0].InLineOfSight))
                            {
                                Objmob[0].Target();
                                WoWMovement.ClickToMove(_startloc);
                                StyxWoW.Sleep(1000);
                            }
                            if (Objmob.Count > 0 && Objmob[0].Location.Distance(Me.Location) > 20 && Objmob[0].Location.Distance(Me.Location) <= 50 && Objmob[0].InLineOfSight)
                            {
                                Objmob[0].Target();
                                Objmob[0].Face();
                                WoWMovement.ClickToMove(Objmob[0].Location);
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton1','0')");
                                StyxWoW.Sleep(500);
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton2','0')");
                                StyxWoW.Sleep(500);
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton3','0')");
                                StyxWoW.Sleep(500);
                            }
                            if (Objmob.Count > 0 && Objmob[0].Location.Distance(Me.Location) <= 20 && Objmob[0].InLineOfSight)
                            {
                                WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
                                WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
                                Objmob[0].Target();
                                Objmob[0].Face();
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton1','0')");
                                StyxWoW.Sleep(500);
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton2','0')");
                                StyxWoW.Sleep(500);
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton3','0')");
                                StyxWoW.Sleep(500);
                            }
							return RunStatus.Running;
						}
    				))
	            )
			);
        }
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

        #endregion

    }
}
