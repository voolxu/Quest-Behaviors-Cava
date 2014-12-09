#region Usings
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buddy.Coroutines;
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
namespace Honorbuddy.Quest_Behaviors.Cava.ItemWhileMoving
{
    [CustomBehaviorFileName(@"Cava\ItemWhileMoving")]
    // ReSharper disable once UnusedMember.Global
    public class ItemWhileMoving : CustomForcedBehavior
    {
        public ItemWhileMoving(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                ItemId = GetAttributeAsNullable("ItemId", true, ConstrainAs.ItemId, null) ?? 0;
                Location = GetAttributeAsNullable("", true, ConstrainAs.WoWPointNonEmpty, null) ?? WoWPoint.Empty;
                QuestId = GetAttributeAsNullable("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
                QuestRequirementComplete = GetAttributeAsNullable<QuestCompleteRequirement>("QuestCompleteRequirement", false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable<QuestInLogRequirement>("QuestInLogRequirement", false, null, null) ?? QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public int ItemId { get; private set; }
        public WoWPoint Location { get; private set; }
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }


        // Private variables for internal state
        private bool _isBehaviorDone;
        // ReSharper disable once UnusedField.Compiler
        private bool _isDisposed;
        // ReSharper disable once UnusedField.Compiler
        private Composite _root;

        private LocalPlayer Me { get { return (StyxWoW.Me); } }

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id: ItemWhileMoving.cs 1581 2014-06-27 02:34:30Z Mainhaxor $"); } }
        public override string SubversionRevision { get { return ("$Revision: 1581 $"); } }

        private WoWItem WowItem
        {
            get
            {
                var inventory = ObjectManager.GetObjectsOfType<WoWItem>();

                foreach (WoWItem item in inventory)
                {
                    if (item.Entry == ItemId)
                        return item;
                }

                return inventory[0];
            }
        }


        
     
        
 
        #region Overrides of CustomForcedBehavior

        protected Composite CreateBehavior_MainCombat()
        {
			return new ActionRunCoroutine(ctx => MainCoroutine());
        }

	    async Task<bool> MainCoroutine()
	    {
	        if (!IsDone)
	        {
	            if (Me.QuestLog.GetQuestById((uint) QuestId) != null &&
	                Me.QuestLog.GetQuestById((uint) QuestId).IsCompleted)
	            {
	                TreeRoot.StatusText = "Finished!";
	                await Coroutine.Sleep(120);
	                _isBehaviorDone = true;
	                return true;
	            }

	            if (Location.Distance(Me.Location) <= 3)
	            {
	                _isBehaviorDone = true;
	                return true;
	            }

	            if (Location.Distance(Me.Location) > 3)
	            {
	                TreeRoot.StatusText = "Moving To Location: Using Item - " + WowItem.Name;
	                var pathtoDest1 = Navigator.GeneratePath(Me.Location, Location);
	                foreach (var p in pathtoDest1)
	                {
	                    while (!Me.IsDead && p.Distance(Me.Location) > 2)
	                    {
	                        await Coroutine.Sleep(100);
	                        WoWMovement.ClickToMove(p);
	                        WowItem.Interact();
	                        await Coroutine.Sleep(200);
	                        SpellManager.ClickRemoteLocation(Me.Location);
	                        await Coroutine.Sleep(500);
	                    }
	                }
	                return false;
	            }
	            return false;
	        }
	        return false;
	    }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Combat_Main", 0, CreateBehavior_MainCombat());

                this.UpdateGoalText(QuestId);
            }
        }

	    public override void OnFinished()
	    {
            TreeHooks.Instance.RemoveHook("Combat_Main", CreateBehavior_MainCombat());
			TreeRoot.GoalText = string.Empty;
			TreeRoot.StatusText = string.Empty;
		    base.OnFinished();
	    }

	    #endregion
    }
}