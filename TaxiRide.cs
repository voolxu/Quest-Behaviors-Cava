// Behavior originally contributed by Cava
// Part of this code obtained from HB QB's and UseTaxi.cs originaly contributed by Vlad
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
// QUICK DOX:
// CavaTaxiRide interact with Flighter masters to pick a fly or get a destiny list names.
//
// BEHAVIOR ATTRIBUTES:
//
// QuestId: (Optional) - associates a quest with this behavior. 
// QuestCompleteRequirement [Default:NotComplete]:
// QuestInLogRequirement [Default:InLog]:
//	If the quest is complete or not in the quest log, this Behavior will not be executed.
//	A full discussion of how the Quest* attributes operate is described in
//      http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//
// MobId: (Required) - Id of the Flight Master to use
// NpcState [optional; Default: DontCare]
//	[Allowed values: Alive, DontCare]
//          This represents the state the NPC must be in when searching for targets
//          with which we can interact.
//
// TaxiNumber: (Optional)- Specifies the Number of the Flight Path on the TaxiMap
// DestName: (Optional) [Default: ViewNodesOnly] - Specifies the destination NAME of the node on the TaxiMap. 
//	If bouth TaxiNumber and DestName are omitted bot will use default ViewNodesOnly, and only give an outputlist of nodes (number name)
//	The TaxiNumber its a number and have prio over the Destname (if bouth are give, bot will only use the TaxiNumber
//	The DestName should be a name string in the list of your TaxiMap node names. The argument is CASE SENSITIVE!
//
// WaitTime [optional; Default: 1500ms]
//	Defines the number of milliseconds to wait after the interaction is successfully
//	conducted before carrying on with the behavior on other mobs.
//
//
// THINGS TO KNOW:
//
// The idea of this Behavior is use the FPs, its not intended to move to near Flight master,
// its always a good idea move bot near the MobId before start this behavior
// If char doesnt know the Destiny flight node name, the will not fly, its always good idea add an RunTo (Destiny XYZ) after use this behavior
// Likethis (RunTo Near MobId) -> (use Behavior) -> (RunTo Destiny XYZ)
//
// You can use signal & inside DestName, but becomes &amp; like Fizzle &amp; Pozzik
// Cant use the signal ' inside DestName, when nodes have that like Fizzle & Pozzik's Speedbarge, Thousand Needles
// you can use the part before or after the signal ' like:
// DestName="Fizzle &amp; Pozzik" or DestName="s Speedbarge, Thousand Needles"
//
// EXAMPLES:
// <CustomBehavior File="TaxiRide" MobId="12345" NpcState="Alive" TaxiNumber="2" />
// <CustomBehavior File="TaxiRide" MobId="12345" DestName="Orgrimmar" WaitTime="1000" />
// <CustomBehavior File="TaxiRide" MobId="12345" DestName="ViewNodesOnly" />
//
//added:
// bot move or fly to near MobId
// bot clear blacklisted MobId
// bot try Opening Taxi Frames 5 times if fail all, will close behavior
// if dont have wait for npc after 3 minutes will close behavior
#endregion

using System.Diagnostics;
using Styx.Pathing;

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

using Honorbuddy.QuestBehaviorCore;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
#endregion


// ReSharper disable once CheckNamespace
namespace Styx.Bot.Quest_Behaviors.Cava2.TaxiRide
{
    [CustomBehaviorFileName(@"Cava\TaxiRide")]
    public class TaxiRide : CustomForcedBehavior
    {
	    #region Constructor and argument processing  
        public enum NpcStateType
        {
            Alive,
            DontCare,
        }

        public TaxiRide(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = GetAttributeAsNullable("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
                QuestRequirementComplete = GetAttributeAsNullable<QuestCompleteRequirement>("QuestCompleteRequirement", false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable<QuestInLogRequirement>("QuestInLogRequirement", false, null, null) ?? QuestInLogRequirement.InLog;

                DestName = GetAttributeAs("DestName", false, ConstrainAs.StringNonEmpty, null) ?? "ViewNodesOnly";
                MobId = GetAttributeAsNullable("MobId", true, ConstrainAs.MobId, null) ?? 0;
                NpcState = GetAttributeAsNullable<NpcStateType>("MobState", false, null, new[] { "NpcState" }) ?? NpcStateType.Alive;
                TaxiNumber = GetAttributeAs("TaxiNumber", false, ConstrainAs.StringNonEmpty, null) ?? "0";
                WaitForNpcs = GetAttributeAsNullable<bool>("WaitForNpcs", false, null, null) ?? false;
                WaitTime = GetAttributeAsNullable("WaitTime", false, ConstrainAs.Milliseconds, null) ?? 1500;
                NpcLocation = GetAttributeAsNullable("", false, ConstrainAs.WoWPointNonEmpty, null) ?? Me.Location;

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
        public int MobId { get; private set; }
        public NpcStateType NpcState { get; private set; }
        public bool WaitForNpcs { get; private set; }
        public int WaitTime { get; private set; }
        public string TaxiNumber { get; private set; }
	    public string DestName { get; set; }
        public WoWPoint NpcLocation { get; set; }
        #endregion


        #region Private and Convenience variables
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;
        private static LocalPlayer Me { get { return (StyxWoW.Me); } }
        private int _trynumber;
        private readonly Stopwatch _doingQuestTimer = new Stopwatch();
        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id: TaxiRide.cs 1085 2013-11-30 10:44:50Z Dogan $"); } }
        public override string SubversionRevision { get { return ("$Revision: 1085 $"); } }
	#endregion

        ~TaxiRide()
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
                    // empty, for now
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

        private WoWUnit CurrentNpc
        {
            get
            {
                var baseTargets = ObjectManager.GetObjectsOfType<WoWUnit>()
                        .OrderBy(target => target.Distance)
                        .Where(target => MobId == (int)target.Entry);
                var npcStateQualifiedTargets = baseTargets
                        .Where(target => ((NpcState == NpcStateType.DontCare)
                        || ((NpcState == NpcStateType.Alive) && target.IsAlive)));
                        var obj = npcStateQualifiedTargets.FirstOrDefault();
                if (obj != null)
                    { QBCLog.DeveloperInfo(obj.Name); }
                return obj;
            }
        }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(ret => Me.OnTaxi || _trynumber >= 5 || (_doingQuestTimer.ElapsedMilliseconds >= 180000 && !WaitForNpcs),
                        new Action(ret => _isBehaviorDone = true)),
                    new Decorator(ret => CurrentNpc == null && NpcLocation != WoWPoint.Empty && NpcLocation.Distance(Me.Location) > 10,
                        new Sequence(
                            new Action(ret => QBCLog.Info("Cant find flightmaster, Moving to place provided by profile")),
                            new DecoratorContinue(ret => StyxWoW.Me.MovementInfo.CanFly,
                                new Sequence(
                                    new Action(ret => Flightor.MoveTo(NpcLocation))
                            )),
                            new DecoratorContinue(ret => !StyxWoW.Me.MovementInfo.CanFly,
                                new Sequence(
                                    new Action(ret => Navigator.MoveTo(NpcLocation))
                            ))
                    )),
                    new Decorator(ret => CurrentNpc == null && NpcLocation.Distance(Me.Location) <= 25,
                        new Sequence(
                            new Action(ret => QBCLog.Info("Waiting for flightmaster to spawn")),
                            new Sleep(1000)
                    )),
                    new Decorator(ret => CurrentNpc != null && CurrentNpc.Location.Distance(Me.Location) > 5,
                        new Sequence(
                            new Action(ret => Navigator.MoveTo(CurrentNpc.Location)),
                            new Sleep(1000)
                    )),
                    new Decorator(ret => CurrentNpc != null && CurrentNpc.Location.Distance(Me.Location) <= 5 && Me.IsMoving,
                        new Action(ret => WoWMovement.MoveStop())),
                    new Decorator(ret => CurrentNpc != null && CurrentNpc.Location.Distance(Me.Location) <= 5 && Me.IsMounted(),
                        new Action(ret => new Mount.ActionLandAndDismount())),
                    new Decorator(ret => CurrentNpc != null && CurrentNpc.Location.Distance(Me.Location) <= 5 && Me.IsShapeshifted(),
                        new Action(ret => Lua.DoString("CancelShapeshiftForm()"))),
                    new Decorator(ret => TaxiNumber == "0" && DestName == "ViewNodesOnly" && CurrentNpc != null && CurrentNpc.Location.Distance(Me.Location) <= 5 ,
                        new Action(context =>
                        {
                            QBCLog.Info("Targeting Flightmaster: " + CurrentNpc.Name + " Distance: " +
                                        CurrentNpc.Location.Distance(Me.Location) + " to listing known TaxiNodes");
                            CurrentNpc.Target();
                            CurrentNpc.Interact();
                            Lua.DoString(string.Format("RunMacroText(\"{0}\")", "/run for i=1,NumTaxiNodes() do a=TaxiNodeName(i); print(i,a);end;"));
                            Me.ClearTarget();
                            StyxWoW.Sleep(WaitTime);
                            _isBehaviorDone = true;
                        }
                    )),
                    new Decorator(ret => TaxiNumber != "0" && CurrentNpc != null && !Me.OnTaxi && CurrentNpc.Location.Distance(Me.Location) <= 5,
                        new Action(context =>
                        {
                            QBCLog.Info("Targeting Flightmaster: " + CurrentNpc.Name + " Distance: " +
                                        CurrentNpc.Location.Distance(Me.Location));
                            CurrentNpc.Target();
                            CurrentNpc.Interact();
                            StyxWoW.Sleep(WaitTime);
                            Lua.DoString(string.Format("RunMacroText(\"{0}\")", "/click TaxiButton" + TaxiNumber));
                            Me.ClearTarget();
                            _trynumber = _trynumber + 1;
                            StyxWoW.Sleep(3000);
                            if (Me.OnTaxi) _isBehaviorDone = true;
                        }
                    )),
                    new Decorator(ret => DestName != "ViewNodesOnly" && CurrentNpc != null && !Me.OnTaxi && CurrentNpc.Location.Distance(Me.Location) <= 5,
                        new Action(context =>
                        {
                            QBCLog.Info("Taking a ride to: " + DestName);
                            CurrentNpc.Target();
                            CurrentNpc.Interact();
                            StyxWoW.Sleep(WaitTime);
                            Lua.DoString(string.Format("RunMacroText(\"{0}\")", "/run for i=1,NumTaxiNodes() do a=TaxiNodeName(i); if strmatch(a,'" + DestName + "')then b=i; TakeTaxiNode(b); end end"));
                            Me.ClearTarget();
                            _trynumber = _trynumber + 1;
                            StyxWoW.Sleep(3000);
                            if (Me.OnTaxi) _isBehaviorDone = true;
                        }
                    ))
            ));
        }

        // ReSharper disable once CSharpWarnings::CS0672
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }


        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();
            _doingQuestTimer.Start();
            Blacklist.Clear(blacklistEntry => blacklistEntry.Entry == MobId);

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                this.UpdateGoalText(QuestId, "TaxiRide started");
            }
        }

        #endregion
    }
}

