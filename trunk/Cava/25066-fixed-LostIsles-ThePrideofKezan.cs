using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.fixedThePrideofKezan
{
    [CustomBehaviorFileName(@"Cava\25066-fixed-LostIsles-ThePrideofKezan")]
    public class Q25066 : CustomForcedBehavior
	{
		public Q25066(Dictionary<string, string> args)
            : base(args){}
        
        public static LocalPlayer Me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
        readonly WoWPoint _endloc = new WoWPoint(1662.314, 2717.742, 189.7396);
        readonly WoWPoint _startloc = new WoWPoint(1782.963, 2884.958, 157.274);
        readonly WoWPoint _flyloc = new WoWPoint(1782.963, 2884.958, 157.274);
		
        
		public List<WoWUnit> Objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 39039 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Flylist
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 38387 && !u.IsDead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        static public bool OnCooldown1 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(121);if b==0 then return 1 else return 0 end", 0) == 0; } }
        static public bool OnCooldown2 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(122);if b==0 then return 1 else return 0 end", 0) == 0; } }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(


                    new Decorator(ret => Me.QuestLog.GetQuestById(25066) != null && Me.QuestLog.GetQuestById(25066).IsCompleted && !InVehicle,
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
							if (Me.QuestLog.GetQuestById(25066).IsCompleted)
							{
								while (Me.Location.Distance(_endloc) > 10)
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
							if (Objmob.Count > 0)
							{
								Objmob[0].Target();
								WoWMovement.ClickToMove(Objmob[0].Location);
                                if (!OnCooldown2)
                                    Lua.DoString("RunMacroText('/click OverrideActionBarButton2','0')");
                                if (!OnCooldown1)
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

