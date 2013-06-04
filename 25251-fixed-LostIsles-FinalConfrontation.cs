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


namespace Honorbuddy.Quest_Behaviors.Cava.fixedFinalConfrontation
{
    [CustomBehaviorFileName(@"Cava\25251-fixed-LostIsles-FinalConfrontation")]
    public class q25251 : CustomForcedBehavior
	{
		public q25251(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		static public bool OnCooldown1 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(121);if b==0 then return 1 else return 0 end", 0) == 0; } }
		static public bool OnCooldown2 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(122);if b==0 then return 1 else return 0 end", 0) == 0; } }
		static public bool OnCooldown3 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(123);if b==0 then return 1 else return 0 end", 0) == 0; } }
		WoWPoint startloc = new WoWPoint(2329.542, 2460.758, 21.13339);
		WoWPoint flyloc = new WoWPoint(2120.643, 2402.012, 49.6927);

        WoWPoint temploc1 = new WoWPoint(2148.491, 2389.996, 43.86974);
        WoWPoint temploc2 = new WoWPoint(2182.27, 2490.118, 13.51234);
        WoWPoint temploc3 = new WoWPoint(2249.442, 2558.885, 4.73251);
        WoWPoint temploc4 = new WoWPoint(2338.563, 2554.455, 4.389093);
        WoWPoint temploc5 = new WoWPoint(2401.248, 2528.158, 5.163994);
        WoWPoint temploc6 = new WoWPoint(2370.525, 2497.789, 12.36469);
		WoWPoint temploc = new WoWPoint(2328.042, 2458.34, 21.13433);

        private bool locreached;
        private bool locreached1;
        private bool locreached2;
        private bool locreached3;
        private bool locreached4;
        private bool locreached5;
        private bool locreached6;
        public List<WoWUnit> objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 39582 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> flylist
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 39592 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(25251) !=null && me.QuestLog.GetQuestById(25251).IsCompleted,
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
							if (flylist.Count == 0)
							{
								Navigator.MoveTo(flyloc);
								Thread.Sleep(1000);
							}
							if (flylist.Count > 0 && flylist[0].Location.Distance(me.Location) > 5)
							{
								Navigator.MoveTo(flylist[0].Location);
								Thread.Sleep(1000);
							}
							if (flylist.Count > 0 && flylist[0].Location.Distance(me.Location) <= 5)
							{
								WoWMovement.MoveStop();
								flylist[0].Interact();
								Thread.Sleep(1000);
							}
						})),
					new Decorator(ret => InVehicle,
						new Action(ret =>
						{
							if (!InVehicle)
								return RunStatus.Success;
							if (me.QuestLog.GetQuestById(25251).IsCompleted)
							{
								while (me.Location.Distance(flyloc) > 10)
								{
									Navigator.MoveTo(flyloc);
									Thread.Sleep(1000);
                                }
								Lua.DoString("VehicleExit()");
								return RunStatus.Success;
							}
                            if (!locreached1)
                            {
                                    if (me.Location.Distance(temploc1)  > 3)
                                    { Navigator.MoveTo(temploc1); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached1 = true;
                                    }
                             }
                                if (locreached1 && !locreached2)
                                {
                                    if (me.Location.Distance(temploc2) > 3)
                                    { Navigator.MoveTo(temploc2); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached2 = true;
                                    }
                                }
                                if (locreached2 && !locreached3)
                                {
                                    if (me.Location.Distance(temploc3) > 3)
                                    { Navigator.MoveTo(temploc3); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached3 = true;
                                    }
                                }
                                if (locreached3 && !locreached4)
                                {
                                    if (me.Location.Distance(temploc4) > 3)
                                    { Navigator.MoveTo(temploc4); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached4 = true;
                                    }
                                }
                                if (locreached4 && !locreached5)
                                {
                                    if (me.Location.Distance(temploc5) > 3)
                                    { Navigator.MoveTo(temploc5); }
                                    else
                                    {
                                        Thread.Sleep(3000);
                                        locreached5 = true;
                                    }
                                }
                                if (locreached5 && !locreached6)
                                {
                                    if (me.Location.Distance(temploc6) > 3)
                                    { Navigator.MoveTo(temploc6); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached6 = true;
                                    }
                                }
                                if (locreached6 && !locreached)
                                {
                                    if (me.Location.Distance(temploc) > 3)
                                    { Navigator.MoveTo(temploc); }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        locreached = true;
                                    }
                            }
                            if (locreached && objmob.Count > 0 && (objmob[0].Location.Distance(me.Location) > 40 || !objmob[0].InLineOfSight))
							{
								objmob[0].Target();
                                Navigator.MoveTo(objmob[0].Location);
								Thread.Sleep(1000);
								Thread.Sleep(1000);
							}
                            if (locreached && objmob.Count > 0 && objmob[0].Location.Distance(me.Location) <= 40 && objmob[0].InLineOfSight)
							{
								WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
								WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
								objmob[0].Target();
								objmob[0].Face();
								if (!OnCooldown3)
								Lua.DoString("RunMacroText('/click OverrideActionBarButton3','0')");
								if (!OnCooldown2)
								Lua.DoString("RunMacroText('/click OverrideActionBarButton2','0')");
                                if (!OnCooldown1 && objmob[0].Location.Distance(me.Location) <= 10)
                                Lua.DoString("RunMacroText('/click OverrideActionBarButton1','0')");
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

    }
}

