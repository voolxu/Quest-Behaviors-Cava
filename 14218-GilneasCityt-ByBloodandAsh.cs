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
using Honorbuddy.QuestBehaviorCore;

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.ByBloodandAsh
{
    [CustomBehaviorFileName(@"Cava\14218-GilneasCityt-ByBloodandAsh")]
    public class Q14218 : CustomForcedBehavior
	{
        public Q14218(Dictionary<string, string> args)
            : base(args){}
        public static LocalPlayer Me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
		public double Angle = 0;
		public double CurentAngle = 0;
        public List<WoWUnit> MobWorgenList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 35229 && !u.IsDead && u.Distance < 150).OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Turret
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => (u.Entry == 35317)).OrderBy(u => u.Distance).ToList();
            }
        }
        private readonly WoWPoint _turretLoc = new WoWPoint(-1526.113, 1570.306, 26.53822);

        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    // exit vehicle if quest is done.
				    new Decorator(ret => Me.QuestLog.GetQuestById(14218) !=null && Me.QuestLog.GetQuestById(14218).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
							new Action(ret => Lua.DoString("VehicleExit()")),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                    })))),
                    // Get in a vehicle if not in one.
                    new Decorator(ret => !Query.IsInVehicle(),
                        new Sequence(
                            new DecoratorContinue(ret => Turret.Count == 0,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(_turretLoc)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => Turret.Count > 0 && Turret[0].Location.Distance(Me.Location) > 5,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(Turret[0].Location)),
                                    new Sleep(1000)
                            )),
                            new DecoratorContinue(ret => Turret.Count > 0 && Turret[0].Location.Distance(Me.Location) <= 5,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new Action(ret => Turret[0].Interact()),
                                    new Sleep(1000)
                    )))),
                    new Decorator(ret => MobWorgenList.Count > 0,
                        new Sequence(
                            new Action(ret => MobWorgenList[0].Target()),
                            new DecoratorContinue(ret => Me.CurrentTarget != null && Me.CurrentTarget.IsAlive,
                                new Sequence(
                                    new Action(ret => WoWMovement.ConstantFace(Me.CurrentTarget.Guid)),
                                    new Action(ret => Angle = -0.6108 - ((Me.CurrentTarget.Distance - 6) * -0.0066)),
                                    new Decorator(ret => Angle < -0.6108,
                                        new Action(ret => Angle = -0.6108)),
                                    new Decorator(ret => Angle > -0.0082,
                                        new Action(ret => Angle = -0.0082)),
                                    new Action(ret => CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0)),
                                    new Decorator(ret => CurentAngle < Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (Angle - CurentAngle))))),
                                    new Decorator(ret => CurentAngle > Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - Angle))))),
                                    //new Action(ret => Lua.DoString(string.Format("VehicleAimRequestAngle(\"{0}\")", Angle))),
                                    new Action(ret => Lua.DoString("if GetPetActionCooldown(1) == 0 then CastPetAction(1) end")),
                                    new Sleep(1000)
                    )))),
                    new Action(ret => Lua.DoString("if GetPetActionCooldown(1) == 0 then CastPetAction(1) end"))
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

