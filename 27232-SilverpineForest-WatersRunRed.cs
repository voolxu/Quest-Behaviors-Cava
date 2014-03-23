//using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
//using System.Threading;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
//using Styx.CommonBot.Routines;
//using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
using Honorbuddy.QuestBehaviorCore;

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.WatersRunRed
{
    [CustomBehaviorFileName(@"Cava\27232-SilverpineForest-WatersRunRed")]
    public class Q27232 : CustomForcedBehavior
	{
        public Q27232(Dictionary<string, string> args)
            : base(args){}
        public static LocalPlayer Me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
		public double Angle = 0;
		public double CurentAngle = 0;
        public List<WoWUnit> MobWorgenList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 45270 && !u.IsDead && u.Y <= 875 && u.Distance < 150).OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Turret
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => (u.Entry == 45263)).OrderBy(u => u.Distance).ToList();
            }
        }
        private readonly WoWPoint _turretLoc = new WoWPoint(710.7488, 947.981, 34.75594);
        private readonly WoWPoint firstshot = new WoWPoint(719.7801, 826.9634, 31.05201);

        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    // exit vehicle if quest is done.
				    new Decorator(ret => Me.QuestLog.GetQuestById(27232) !=null && Me.QuestLog.GetQuestById(27232).IsCompleted,
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
                    new Decorator(ret => MobWorgenList.Count == 0,
                        new Sequence(
                            new Action(ret => WoWMovement.ClickToMove(firstshot)),
                            new DecoratorContinue(ret => Me.CurrentTarget == null,
                                new Sequence(
                                    new Action(ret => WoWMovement.ConstantFace(Me.CurrentTarget.Guid)),
                                    new Action(ret => Angle = (firstshot.Z - Me.Z) / (Me.Location.Distance(firstshot))),
                                    new Action(ret => CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0)),
                                    new Decorator(ret => CurentAngle < Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (Angle - CurentAngle))))),
                                    new Decorator(ret => CurentAngle > Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - Angle))))),
                                    new Sleep(1000)
                    )))),
                    new Decorator(ret => MobWorgenList.Count > 0,
                        new Sequence(
                            new Action(ret => MobWorgenList[0].Target()),
                            new DecoratorContinue(ret => Me.CurrentTarget != null && Me.CurrentTarget.IsAlive,
                                new Sequence(
                                    new Action(ret => WoWMovement.ConstantFace(Me.CurrentTarget.Guid)),
                                    new Action(ret => Angle = (Me.CurrentTarget.Z - Me.Z) / (Me.CurrentTarget.Location.Distance(Me.Location))),
                                    new Action(ret => CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0)),
                                    new Decorator(ret => CurentAngle < Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (Angle - CurentAngle))))),
                                    new Decorator(ret => CurentAngle > Angle,
                                        new Action(ret => Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - Angle))))),
                                    new Sleep(1000)
                    )))),
                    new Action(ret => Lua.DoString("RunMacroText('/click OverrideActionBarButton1','0')"))

					/*new Action(ret =>
					{
						//Lua.DoString("CastPetAction({0})", 1);	
						//if(MobWorgenList.Count == 0)
						//	return;
						MobWorgenList[0].Target();
						while (Me.CurrentTarget != null && Me.CurrentTarget.IsAlive )
						{
							//WoWMovement.ConstantFace(Me.CurrentTarget.Guid);
							//Angle = (Me.CurrentTarget.Z - Me.Z) / (Me.CurrentTarget.Location.Distance(Me.Location));
							//CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0);
							if (CurentAngle < Angle)
							{
								Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (Angle - CurentAngle)));
							}
							if (CurentAngle > Angle)
							{
								Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - Angle)));
							}
							Lua.DoString("CastPetAction({0})", 1);
						}
					})*/
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

