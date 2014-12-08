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
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.TheBattleofThunderPass
{
    [CustomBehaviorFileName(@"Cava\34124-SMV-WoD-TheBattleofThunderPass")]
    // ReSharper disable once UnusedMember.Global
    public class TheBattleofThunderPass : CustomForcedBehavior
    {
        public TheBattleofThunderPass(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                QuestId = 34124;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
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
        private readonly WoWPoint _startStage = new WoWPoint(6022.613, 2863.982, 199.8381);
        private readonly WoWPoint _startEvent = new WoWPoint(6002.226, 2923.12, 184.4509);
        private readonly WoWPoint _healLoc = new WoWPoint(6029.628, 2911.399, 192.7821);

        private static List<WoWUnit> Adds
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                        .Where(
                            u =>
                                (u.IsAlive &&
                                 (u.Entry == 76549 || u.Entry == 76960 || u.Entry == 76633 || u.Entry == 76639)))
                        .OrderBy(u => u.Distance).ToList();
            }
        }

        private static List<WoWUnit> AddsEvent5
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                        .Where(
                            u =>
                                (u.IsAlive && u.Z > 203 &&
                                 (u.Entry == 76549 || u.Entry == 76960 || u.Entry == 76633 || u.Entry == 76639)))
                        .OrderBy(u => u.Distance).ToList();
            }
        }
        //IronGrunt 76549, 76960
        //Blackrock Hammer-Sister 76633  
        //Blackrock Ironhusk 76639

        public WoWUnit IronClusterpult { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAlive && u.Entry == 76629).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit ViciousLongtusk { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAlive && u.Entry == 76624).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit MalgrimStormhand { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAlive && u.Entry == 76630).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit Maggoc { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAlive && u.Entry == 76491).OrderBy(u => u.Distance).FirstOrDefault(); } }
        public WoWUnit Durotan { get { return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAlive && u.Entry == 76484 && u.Distance < 10).OrderBy(u => u.Distance).FirstOrDefault(); } }

        private int Stage { get { return ( Bots.DungeonBuddy.Helpers.ScriptHelpers.CurrentScenarioInfo.CurrentStage.StageNumber); } }
        private bool StageLoc1;
        private bool StageLoc2;
        private bool StageLoc3;
        private bool StageLoc4;
        private bool StageLoc5;
        private bool StageLoc6;
        private bool StageLoc7;
        private bool StageLoc8;
        private bool StageLoc9;
        #region Overrides of CustomForcedBehavior

        protected Composite CreateBehavior_MainCombat()
        {
			return new ActionRunCoroutine(ctx => MainCoroutine());
        }

	    async Task<bool> MainCoroutine()
	    {
	        if (!IsDone)
	        {
	            if (Stage == 1 && !StageLoc1)
	            {

	                if (Me.Location.Distance(_startStage) > 5)
	                {
	                    QBCLog.Info("Moving to Start Stage Location");
	                    Navigator.MoveTo(_startStage);
	                    return true;
	                }
	                StageLoc1 = true;
	                return false;
	            }
                if (Stage == 2 && !StageLoc2)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc2 = true;
                    return false;
                }
                if (Stage == 3 && !StageLoc3)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc3 = true;
                    return false;
                }
                if (Stage == 4 && !StageLoc4)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc4 = true;
                    return false;
                }
                if (Stage == 5 && !StageLoc5)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc5 = true;
                    return false;
                }
                if (Stage == 6 && !StageLoc6)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc6 = true;
                    return false;
                }
                if (Stage == 7 && !StageLoc7)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc7 = true;
                    return false;
                }
                if (Stage == 8 && !StageLoc8)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc8 = true;
                    return false;
                }
                if (Stage == 9 && !StageLoc9)
                {

                    if (Me.Location.Distance(_startStage) > 5)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return true;
                    }
                    StageLoc9 = true;
                    return false;
                }
	        

	        if (Stage == 0 || Stage == 1)
		        {
                    QBCLog.Info("Moving to Start Event Location");
                    Navigator.MoveTo(_startEvent);
		            if (Durotan != null)
		            {
                        QBCLog.Info("Starting Event");
                        Durotan.Interact();
		                Lua.DoString("SelectGossipOption(1)");
		            }
		            return false;
		        }
		        if (Stage > 0 && Me.HealthPercent < 30)
		        {
                    QBCLog.Info("Moving to Heal Location");
                    Navigator.MoveTo(_healLoc);
                    return true;
		        }
                if (Stage == 2 || Stage == 4 || Stage == 8)
		        {
		            if (Adds.Count == 0)
		            {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
		            }
                    else
                    {
                        QBCLog.Info("Killing adds");
                        BotPoi.Current = new BotPoi(Adds[0], PoiType.Kill);
                        Adds[0].Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing adds");
                            BotPoi.Current = new BotPoi(Adds[0], PoiType.Kill);
                            Adds[0].Target();
                        }
                    }
		            return false;
		        }
		        if (Stage == 3)
		        {
		            if (IronClusterpult == null)
		            {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return false;
                    }
                    if (IronClusterpult != null && IronClusterpult.IsCasting && (IronClusterpult.CastingSpellId == 155255 || IronClusterpult.CastingSpellId == 155243))
		            {
                        QBCLog.Info("Moving Behind Iron Clusterpult");
                        Navigator.MoveTo(IronClusterpult.Location.RayCast(IronClusterpult.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
                        return true;
		            }
                    if (IronClusterpult != null && !IronClusterpult.IsCasting)
                    {
                        QBCLog.Info("Killing Iron Clusterpult");
                        BotPoi.Current = new BotPoi(IronClusterpult, PoiType.Kill);
                        IronClusterpult.Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing Iron Clusterpult");
                            BotPoi.Current = new BotPoi(IronClusterpult, PoiType.Kill);
                            IronClusterpult.Target();
                        }
                    }
		            return false;
		        }
		        if (Stage == 5)
		        {
                    if (Adds.Count == 0 && AddsEvent5.Count == 0)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return false;
                    }
		            if (AddsEvent5.Count > 0)
		            {
                        QBCLog.Info("Killing adds for event 5");
                        BotPoi.Current = new BotPoi(AddsEvent5[0], PoiType.Kill);
                        AddsEvent5[0].Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing adds for event 5");
                            BotPoi.Current = new BotPoi(AddsEvent5[0], PoiType.Kill);
                            AddsEvent5[0].Target();
                        }
                        return false;
		            }
                    if (AddsEvent5.Count == 0 && Adds.Count > 0)
                    {
                        QBCLog.Info("Killing adds");
                        BotPoi.Current = new BotPoi(Adds[0], PoiType.Kill);
                        Adds[0].Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing adds");
                            BotPoi.Current = new BotPoi(Adds[0], PoiType.Kill);
                            Adds[0].Target();
                        }
                    }
                    return false;
		        }
		        if (Stage == 6)
		        {
                    if (ViciousLongtusk == null )
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return false;
                    }
                    if (ViciousLongtusk != null && ViciousLongtusk.IsCasting && (ViciousLongtusk.CastingSpellId == 149656 || ViciousLongtusk.CastingSpellId == 146410))
                    {
                        QBCLog.Info("Moving Behind Iron Clusterpult");
                        Navigator.MoveTo(ViciousLongtusk.Location.RayCast(ViciousLongtusk.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
                        return true;
                    }
                    if (ViciousLongtusk != null && !ViciousLongtusk.IsCasting)
                    {
                        QBCLog.Info("Killing Vicious Longtusk");
                        BotPoi.Current = new BotPoi(ViciousLongtusk, PoiType.Kill);
                        ViciousLongtusk.Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing Vicious Longtusk");
                            BotPoi.Current = new BotPoi(ViciousLongtusk, PoiType.Kill);
                            ViciousLongtusk.Target();
                        }
                    }
                    return false;
		        }
                if (Stage == 7)
                {
                    if (MalgrimStormhand == null)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return false;
                    }
                    if (MalgrimStormhand != null && MalgrimStormhand.IsCasting && MalgrimStormhand.CastingSpellId == 148815)
                    {
                        QBCLog.Info("Moving Behind Iron Clusterpult");
                        Navigator.MoveTo(MalgrimStormhand.Location.RayCast(MalgrimStormhand.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
                        return true;
                    }
                    if (MalgrimStormhand != null && !MalgrimStormhand.IsCasting)
                    {
                        QBCLog.Info("Killing Malgrim Stormhand");
                        BotPoi.Current = new BotPoi(MalgrimStormhand, PoiType.Kill);
                        MalgrimStormhand.Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing Malgrim Stormhand");
                            BotPoi.Current = new BotPoi(MalgrimStormhand, PoiType.Kill);
                            MalgrimStormhand.Target();
                        }
                    }
                    return false;
                }
                if (Stage == 9)
                {
                    if (Maggoc == null)
                    {
                        QBCLog.Info("Moving to Start Stage Location");
                        Navigator.MoveTo(_startStage);
                        return false;
                    }
                    if (Maggoc != null && Maggoc.IsCasting && Maggoc.CastingSpellId == 155230)
                    {
                        QBCLog.Info("Moving Behind Maggoc");
                        Navigator.MoveTo(Maggoc.Location.RayCast(Maggoc.Rotation + WoWMathHelper.DegreesToRadians(150), 10f));
                        return true;
                    }
                    if (Maggoc != null && Maggoc.IsCasting && Maggoc.CastingSpellId == 155290)
                    {
                        QBCLog.Info("Moving away from Maggoc");
                        Navigator.MoveTo(_healLoc);
                        return true;
                    }
                    if (Maggoc != null && !Maggoc.IsCasting)
                    {
                        QBCLog.Info("Killing Maggoc");
                        BotPoi.Current = new BotPoi(Maggoc, PoiType.Kill);
                        Maggoc.Target();
                        if (BotPoi.Current.Type != PoiType.Kill)
                        {
                            QBCLog.Info("Killing Maggoc");
                            BotPoi.Current = new BotPoi(Maggoc, PoiType.Kill);
                            Maggoc.Target();
                        }
                    }
                    return false;
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