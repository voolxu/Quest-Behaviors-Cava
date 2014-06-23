#region Summary and Documentation
#endregion
#region Examples
#endregion

using System.Diagnostics;
using Buddy.Coroutines;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
namespace Honorbuddy.Quest_Behaviors.Cava.SeedsofDiscord
{
    [CustomBehaviorFileName(@"Cava\25308-MountHyjal-SeedsofDiscord")]
    public class SeedsofDiscord : CustomForcedBehavior
    {
        public SeedsofDiscord(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 25308;
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
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private Composite _root;

        // Private properties
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        WoWPoint _outHouseLoc = new WoWPoint(4831.595, -4221.065, 894.0665);
        WoWPoint _runToLoc = new WoWPoint(4805.728, -4185.581, 897.5305);
        public WoWUnit MobKarrgonn { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 40489 && u.IsAlive && !u.IsMoving && u.Distance < 15).OrderBy(u => u.Distance).FirstOrDefault(); }}
        public WoWUnit MobAzennios { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 40491 && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault(); } }

        //private static List<WoWUnit> MobKarrgonn { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 40489 && ret.IsAlive && !ret.IsMoving)).OrderBy(ret => ret.Distance).ToList(); } }
        //private static List<WoWUnit> MobAzennios { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(ret => (ret.Entry == 40491 && ret.IsAlive && ret.IsNeutral)).OrderBy(ret => ret.Distance).ToList(); } }
        private static WoWItem ItemOgreDisguise { get { return (StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == 55137)); } }
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();

        #region Overrides of CustomForcedBehavior

        protected Composite CreateBehavior_QuestbotMain()
        {
			return new ActionRunCoroutine(ctx => MainCoroutine());
        }

	    async Task<bool> MainCoroutine()
	    {
			if (!IsDone)
		    {
                //Remove Aura in combate
		        if (Me.Combat && Me.HasAura(75724))
		        {
		            QBCLog.Info("Im in Combat, Removing Aura");
                    Lua.DoString("CancelUnitBuff('player', GetSpellInfo(75724))");
		        }
		        //Get Aura
                if (!Me.HasAura(75724))
			    {
                    QBCLog.Info("Grabing Aura");
                    if (_outHouseLoc.Distance(Me.Location) > 2)
				    {
                        QBCLog.Info("Moving Near House");
                        Navigator.MoveTo(_outHouseLoc);
				    }
                    if (_outHouseLoc.Distance(Me.Location) <= 2)
                    {
                        QBCLog.Info("Using Item");
                        WoWMovement.MoveStop();
                        Lua.DoString("CancelShapeshiftForm()");
                        ItemOgreDisguise.Use();
                    }
                    return true;
			    }

                // Move To Place
                if (_runToLoc.Distance(Me.Location) > 5)
                {
                    QBCLog.Info("Moving to inside house");
                    Navigator.MoveTo(_runToLoc);
				    return true;
			    }

				// move to target
                if (_runToLoc.Distance(Me.Location) <= 5 && MobKarrgonn != null && MobKarrgonn.Location.Distance(Me.Location) > 5)
			    {
                    QBCLog.Info("Moving Near Karrgonn");
                    Navigator.MoveTo(MobKarrgonn.Location);
				    return true;
			    }
				// interact target
                if (MobKarrgonn != null && MobKarrgonn.Location.Distance(Me.Location) <= 5)
			    {
                    QBCLog.Info("Interacting with Karrgonn");
                    WoWMovement.MoveStop();
                    MobKarrgonn.Interact();
                    await Coroutine.Sleep(2000);
			        Lua.DoString("SelectGossipOption(1)");
                    await Coroutine.Sleep(10000);
				    return true;
			    }

				// move to second target
                if (!Me.Combat && MobKarrgonn == null && MobAzennios != null && MobAzennios.Location.Distance(Me.Location) > 5)
				{
                    QBCLog.Info("Moving Near Azennios");
                    Navigator.MoveTo(MobAzennios.Location);
                    return true;
				}

                // interact second target
                if (!Me.Combat && MobKarrgonn == null && MobAzennios != null && MobAzennios.Location.Distance(Me.Location) <= 5)
                {
                    QBCLog.Info("Starting Combat");
                    WoWMovement.MoveStop();
                    MobAzennios.Face();
                    MobAzennios.Target();
                    MobAzennios.Interact();
                    await Coroutine.Sleep(2000);
                    Lua.DoString("StartAttack()");
                    return true;
                }
                return true;
		    }
		    return false;
	    }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete)
                        || _doingQuestTimer.ElapsedMilliseconds >= 180000
                        || Me.Combat);
            }
        }

        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());
                this.UpdateGoalText(QuestId);
            }
        }

	    public override void OnFinished()
	    {
            Lua.DoString("CancelUnitBuff('player', GetSpellInfo(75724))");
            TreeHooks.Instance.RemoveHook("Questbot_Main", CreateBehavior_QuestbotMain());
			TreeRoot.GoalText = string.Empty;
			TreeRoot.StatusText = string.Empty;
		    base.OnFinished();
	    }

	    #endregion
    }
}