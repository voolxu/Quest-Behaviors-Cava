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

using System.Diagnostics;
using Styx.Pathing;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.NightTerrors
{
    [CustomBehaviorFileName(@"Cava\28170-HordeTwilightHighlands-NightTerrors")]
    public class NightTerrors : CustomForcedBehavior
    {
        public NightTerrors(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 28170;
            }
            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }
        public int QuestId { get; set; }
        private bool _isBehaviorDone;
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();
        private readonly WoWPoint _shrineLoc1 = new WoWPoint(-3409.618, -4238.957, 221.422);
        private readonly WoWPoint _shrineLoc2 = new WoWPoint(-3458.498, -4200.433, 212.6733);
        private readonly WoWPoint _shrineLoc3 = new WoWPoint(-3483.773, -4242.503, 214.5404);

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
            _doingQuestTimer.Start();
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_MainCombat());
                this.UpdateGoalText(QuestId);
            }
        }

        private static WoWItem TheLightofSouls
        {
            get
            {
                return
                    (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 67537));
            }
        }

        public Composite DoneYet
        {
            get
            {
                return new Decorator(ret => Me.IsQuestComplete(QuestId) || _doingQuestTimer.ElapsedMilliseconds >= 180000 || !Me.HasAura(88981),
                    new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        if (Me.HasAura(88981))
                        {
                            Lua.DoString("CancelUnitBuff('player',GetSpellInfo(88981))");
                        }
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));
            }
        }

        protected Composite CreateBehavior_MainCombat()
        {
            return _root ?? (_root = 
                new Decorator(ret => !_isBehaviorDone,
                    new PrioritySelector(
                        DoneYet,
                        new Decorator(ret => Me.Combat && Me.HasAura(88981) && TheLightofSouls.Cooldown == 0,
                            new Action(ret => TheLightofSouls.UseContainerItem())),
                        new Decorator(ret => !Me.IsQuestObjectiveComplete(QuestId, 1),
                            new Action(ret => Navigator.MoveTo(_shrineLoc1))),
                        new Decorator(ret => Me.IsQuestObjectiveComplete(QuestId, 1) && !Me.IsQuestObjectiveComplete(QuestId, 2),
                            new Action(ret => Navigator.MoveTo(_shrineLoc2))),
                        new Decorator(ret => Me.IsQuestObjectiveComplete(QuestId, 1) && Me.IsQuestObjectiveComplete(QuestId, 2) && !Me.IsQuestObjectiveComplete(QuestId, 3),
                            new Action(ret => Navigator.MoveTo(_shrineLoc3)))
            )));
        }
        #region Cleanup

        private bool _isDisposed;

        ~NightTerrors()
        {
            Dispose(false);
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
                    TreeHooks.Instance.RemoveHook("Combat_Main", CreateBehavior_MainCombat());
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

        // ReSharper disable once CSharpWarnings::CS0672
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}



