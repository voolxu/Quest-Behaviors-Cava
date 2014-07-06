using System.Collections.Generic;
using System.Linq;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.fixedFinalConfrontation
{
    [CustomBehaviorFileName(@"Cava\25251-fixed-LostIsles-FinalConfrontation")]
    public class Q25251 : CustomForcedBehavior
	{
		public Q25251(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer Me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		static public bool OnCooldown1 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(121);if b==0 then return 1 else return 0 end", 0) == 0; } }
		static public bool OnCooldown2 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(122);if b==0 then return 1 else return 0 end", 0) == 0; } }
		static public bool OnCooldown3 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(123);if b==0 then return 1 else return 0 end", 0) == 0; } }
        readonly WoWPoint _flyloc = new WoWPoint(2120.643, 2402.012, 49.6927);

        readonly WoWPoint _temploc1 = new WoWPoint(2148.491, 2389.996, 43.86974);
        readonly WoWPoint _temploc2 = new WoWPoint(2182.27, 2490.118, 13.51234);
        readonly WoWPoint _temploc3 = new WoWPoint(2249.442, 2558.885, 4.73251);
        readonly WoWPoint _temploc4 = new WoWPoint(2338.563, 2554.455, 4.389093);
        readonly WoWPoint _temploc5 = new WoWPoint(2401.248, 2528.158, 5.163994);
        readonly WoWPoint _temploc6 = new WoWPoint(2370.525, 2497.789, 12.36469);
        readonly WoWPoint _temploc = new WoWPoint(2328.042, 2458.34, 21.13433);

        private bool _locreached;
        private bool _locreached1;
        private bool _locreached2;
        private bool _locreached3;
        private bool _locreached4;
        private bool _locreached5;
        private bool _locreached6;
        public List<WoWUnit> Objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 39582 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Flylist
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


                    new Decorator(ret => Me.QuestLog.GetQuestById(25251) != null && Me.QuestLog.GetQuestById(25251).IsCompleted && !InVehicle,
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
                                _locreached1 = false;
                                _locreached2 = false;
                                _locreached3 = false;
                                _locreached4 = false;
                                _locreached5 = false;
                                _locreached6 = false;
                                _locreached = false;
                            }
						})),
					new Decorator(ret => InVehicle,
						new Action(ret =>
						{
							if (!InVehicle)
								return RunStatus.Success;
							if (Me.QuestLog.GetQuestById(25251).IsCompleted)
							{
								//while (Me.Location.Distance(_flyloc) > 10)
								//{
								//	Navigator.MoveTo(_flyloc);
                                //    StyxWoW.Sleep(1000);
                                //}
								Lua.DoString("VehicleExit()");
								return RunStatus.Success;
							}
                            if (!_locreached1)
                            {
                                    if (Me.Location.Distance(_temploc1)  > 3)
                                    { Navigator.MoveTo(_temploc1); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached1 = true;
                                    }
                             }
                                if (_locreached1 && !_locreached2)
                                {
                                    if (Me.Location.Distance(_temploc2) > 3)
                                    { Navigator.MoveTo(_temploc2); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached2 = true;
                                    }
                                }
                                if (_locreached2 && !_locreached3)
                                {
                                    if (Me.Location.Distance(_temploc3) > 3)
                                    { Navigator.MoveTo(_temploc3); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached3 = true;
                                    }
                                }
                                if (_locreached3 && !_locreached4)
                                {
                                    if (Me.Location.Distance(_temploc4) > 3)
                                    { Navigator.MoveTo(_temploc4); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached4 = true;
                                    }
                                }
                                if (_locreached4 && !_locreached5)
                                {
                                    if (Me.Location.Distance(_temploc5) > 3)
                                    { Navigator.MoveTo(_temploc5); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached5 = true;
                                    }
                                }
                                if (_locreached5 && !_locreached6)
                                {
                                    if (Me.Location.Distance(_temploc6) > 3)
                                    { Navigator.MoveTo(_temploc6); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached6 = true;
                                    }
                                }
                                if (_locreached6 && !_locreached)
                                {
                                    if (Me.Location.Distance(_temploc) > 3)
                                    { Navigator.MoveTo(_temploc); }
                                    else
                                    {
                                        StyxWoW.Sleep(1000);
                                        _locreached = true;
                                    }
                            }
						    if (_locreached && Me.Z < 15)
						    {
                                _locreached4 = false;
                                _locreached5 = false;
                                _locreached6 = false;
                                _locreached = false;
						    }
                            if (_locreached && Me.Z >= 15 && Objmob.Count > 0 && (Objmob[0].Location.Distance(Me.Location) > 40 || !Objmob[0].InLineOfSight))
							{
								Objmob[0].Target();
                                Navigator.MoveTo(Objmob[0].Location);
                                StyxWoW.Sleep(2000);
							}
                            if (_locreached && Me.Z >= 15 && Objmob.Count > 0 && Objmob[0].Location.Distance(Me.Location) <= 40 && Objmob[0].InLineOfSight)
							{
								WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
								WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
								Objmob[0].Target();
								Objmob[0].Face();
								if (!OnCooldown3)
								Lua.DoString("RunMacroText('/click OverrideActionBarButton3','0')");
								if (!OnCooldown2)
								Lua.DoString("RunMacroText('/click OverrideActionBarButton2','0')");
                                if (!OnCooldown1 && Objmob[0].Location.Distance(Me.Location) <= 10)
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

