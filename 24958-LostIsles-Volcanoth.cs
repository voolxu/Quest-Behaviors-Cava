//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Summary and Documentation
#endregion


#region Examples
#endregion


#region Usings
using System;
using System.Collections.Generic;
using System.Linq;

using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.Volcanoth
{
    [CustomBehaviorFileName(@"Cava\24958-LostIsles-Volcanoth")]
	public class Volcanoth : CustomForcedBehavior
	{
		public Volcanoth(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;

			QuestId = 24958;//GetAttributeAsQuestId("QuestId", true, null) ?? 0;
		}
		public int QuestId { get; set; }
		private bool _isBehaviorDone;
		private Composite _root;
		
		public override bool IsDone
		{
			get
			{
				return _isBehaviorDone;
			}
		}
		private static LocalPlayer Me
		{
			get { return (StyxWoW.Me); }
		}

		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
			if (!IsDone)
			{
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());
				this.UpdateGoalText(QuestId);
			}
		}

        public WoWUnit Turtle
		{
			get
			{
                return ObjectManager.GetObjectsOfType<WoWUnit>(true).FirstOrDefault(u => u.Entry == 38855 && !u.IsDead);
			}
		}

		public Composite DoneYet
		{
			get
			{
				return new Decorator(ret => Me.IsQuestComplete(QuestId),
					new Action(delegate
					{
						TreeRoot.StatusText = "Finished!";
						_isBehaviorDone = true;
						return RunStatus.Success;
					}));
			}
		}

		private static WoWPoint CalculatePointBehindTarget()
		{
			return
				StyxWoW.Me.CurrentTarget.Location.RayCast(
					StyxWoW.Me.CurrentTarget.Rotation + WoWMathHelper.DegreesToRadians(150),10f);
		}

        public WoWItem Bootzooka
        {
            get
            {
                return Me.BagItems.FirstOrDefault(r => r.Entry == 52043);
            }
        }
        
        public Composite DpsHim
		{
			get
			{
				return new PrioritySelector(
                    new Decorator(r => Me.CurrentTarget == null && Turtle != null && Me.CurrentTarget != Turtle,
                        new Action(r => Turtle.Target())),
                    new Decorator(r => Turtle != null && Turtle.IsCasting && !Turtle.MeIsSafelyBehind,
                        new Action(r => Navigator.MoveTo(CalculatePointBehindTarget()))),
					
                    new Decorator(r => Turtle != null && !Turtle.IsCasting,
                        new Sequence(
                            new Action(c => Turtle.Target()),
                            new DecoratorContinue(c => Turtle.Location.Distance(Me.Location) > 20,
                                new Action(c => Navigator.MoveTo(Turtle.Location))),
                            new DecoratorContinue(c => Turtle.Location.Distance(Me.Location) < 20,
                                new Sequence(
                                    new Action(c => Turtle.Face()),
                                    new Action(c => WoWMovement.MoveStop()),
                                    new PrioritySelector(
                                        new Decorator(r => Bootzooka.CooldownTimeLeft.TotalSeconds > 0,
                                            new ActionAlwaysSucceed()),
					                    new Decorator(r => Bootzooka.CooldownTimeLeft.TotalSeconds <= 0,
                                            new Action(r=>Bootzooka.Use()))
                ))))));
			}
		}

        protected Composite CreateBehavior_QuestbotMain()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
                new PrioritySelector(
                    DoneYet,
                    DpsHim,
                    new ActionAlwaysSucceed())));
		}

		 #region Cleanup

        ~Volcanoth()
		{
			Dispose(false);
		}

		private bool _isDisposed;

        // ReSharper disable once CSharpWarnings::CS0672
		public override void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool isExplicitlyInitiatedDispose)
		{
			if (!_isDisposed)
			{
				// NOTE: we should call any Dispose() method for any managed or unmanaged
				// resource, if that resource provides a Dispose() method.

				// Clean up managed resources, if explicit disposal...
				if (isExplicitlyInitiatedDispose)
				{
					TreeHooks.Instance.RemoveHook("Questbot_Main", CreateBehavior_QuestbotMain());
				}

				// Clean up unmanaged resources (if any) here...
				TreeRoot.GoalText = string.Empty;
				TreeRoot.StatusText = string.Empty;

				// Call parent Dispose() (if it exists) here ...
                // ReSharper disable once CSharpWarnings::CS0618
				base.Dispose();
			}

			_isDisposed = true;
		}

		#endregion
	}
}
