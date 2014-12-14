using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
//using System.Linq;
//using System.Threading;
using System.Threading;
using CommonBehaviors.Actions;
//using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
//using Styx.CommonBot.Routines;
using Styx.Helpers;
//using Styx.Pathing;
//using Styx.Plugins;
using Styx.TreeSharp;
//using Styx.WoWInternals;
//using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.CavaLoader
{
    [CustomBehaviorFileName(@"Cava\CavaLoader")]
    public class CavaLoader : CustomForcedBehavior
    {
        public CavaLoader(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                ProfileBaseToLoad = GetAttributeAsNullable("PBL", false, new ConstrainTo.Domain<int>(0, 100), null) ?? 0;
                ProfileName = GetAttributeAs("ProfileName", false, ConstrainAs.StringNonEmpty, new[] { "Profile" }) ?? "";

								}
            catch (Exception except)
            {
               LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
               IsAttributeProblem = true;
            }
        }
        public static String ProfileName { get; private set; }
        private static Thread ChangeBotBase;
        public static readonly string PathToCavaProfiles = Path.Combine(Utilities.AssemblyDirectory + @"\Default Profiles\Cava\");

        public class CPGlobalSettings : Settings
        {
            public static readonly CPGlobalSettings Instance = new CPGlobalSettings();
            public CPGlobalSettings()
                : base(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@"Settings\CavaPlugin\Main-Settings.xml")))
            {
            }
            [Setting, DefaultValue(false)]
            public bool BotAllowUpdate { get; set; }
            [Setting, DefaultValue(false)]
            public bool AllowUpdate { get; set; }
            [Setting, DefaultValue(false)]
            public bool Allowlunch { get; set; }
            [Setting, DefaultValue(0)]
            public int BaseProfileToLunch { get; set; }
            [Setting, DefaultValue(false)]
            public bool AutoShutdownWhenUpdate { get; set; }
            [Setting, DefaultValue(false)]
            public bool PBMiningBlacksmithing { get; set; }
            [Setting, DefaultValue(false)]
            public bool BotPBMiningBlacksmithing { get; set; }
            [Setting, DefaultValue(0)]
            public int language { get; set; }
            [Setting, DefaultValue(false)]
            public bool Languageselected { get; set; }
            [Setting, DefaultValue("")]
            public string CpLogin { get; set; }
            [Setting, DefaultValue("")]
            public string CpPassword { get; set; }
            [Setting, DefaultValue(false)]
            public bool CpPanelBack { get; set; }
            [Setting, DefaultValue(false)]
            public bool ArmaPanelBack { get; set; }
            [Setting, DefaultValue(false)]
            public bool ProfMinBlack600 { get; set; }
            [Setting, DefaultValue(1)]
            public int UseServer { get; set; }
            [Setting, DefaultValue(true)]
            public bool DisablePlugin { get; set; }
        }
        
        // Attributes provided by caller
        //public String ProfilePath = Path.Combine(Utilities.AssemblyDirectory + @"\Default Profiles\Cava\");

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
 
        // Private properties
        private String NewProfilePath
        {
            get
            {
                var directory = Path.Combine(Utilities.AssemblyDirectory + @"\Default Profiles\Cava\Scripts\");
                return (Path.Combine(directory, ProfileName + ".xml"));
            }
        }

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id: CavaLoader.cs 369 2013-03-18 09:05:58Z chinajade $"); } }
        public override string SubversionRevision { get { return ("$Revision: 369 $"); } }
        public int ProfileBaseToLoad { get; private set; }

				
        ~CavaLoader()
        {
            Dispose(false);
        }

        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                if (isExplicitlyInitiatedDispose)
                {
                }
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;
                base.Dispose();
            }
            _isDisposed = true;
        }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return (
                new PrioritySelector(
                // If behavior is complete, nothing to do, so bail...
                    new Decorator(ret => _isBehaviorDone,
                    new Action(delegate { LogMessage("info", "Behavior complete"); })),

                    new Decorator(ret => ProfileBaseToLoad == 0,
                        new Action(context =>
                        {
                            CPGlobalSettings.Instance.Load();
                            if (CPGlobalSettings.Instance.Allowlunch)
                            {
                                ProfileBaseToLoad = CPGlobalSettings.Instance.BaseProfileToLunch;
                            }
                            CPGlobalSettings.Instance.Allowlunch = false;
                            CPGlobalSettings.Instance.Save();
                            if (ProfileBaseToLoad == 1) { ProfileName = "Next[Cava]"; }
                            if (ProfileBaseToLoad == 2) { ProfileName = "[Quest]Pandaren-Horde1to90By[Cava]"; }
                            if (ProfileBaseToLoad == 3) { ProfileName = "[Quest]Pandaren-Alliance1to90By[Cava]"; }
                            if (ProfileBaseToLoad == 4) { ProfileName = "ArmageddonerFast[Cava]"; }
                            if (ProfileBaseToLoad == 5) { ProfileName = "ArmageddonerNext[Cava]"; }
                            if (ProfileBaseToLoad == 6) { ProfileName = "ArmageddonerNext[Cava]"; }
                            if (ProfileBaseToLoad == 7) { ProfileName = "CavaProf\\MB\\[PB]MB(Cava)"; }
                            if (ProfileBaseToLoad == 8) { ProfileName = "CavaProf\\MB\\Free[PB]MB(Cava)"; }
							if (ProfileBaseToLoad == 10) { ProfileName = "[N-Quest]Armageddoner_Reserved[Cava]"; }

                            if (ProfileBaseToLoad == 7 || ProfileBaseToLoad == 8)
                            {
                                if (ChangeBotBase.ThreadState == ThreadState.Running)
                                    ChangeBotBase.Abort();
                                ChangeBotBase.Start();
                                _isBehaviorDone = true;
                                return;
                            }
               
                            if (ProfileBaseToLoad == 0)
                            {
                                _isBehaviorDone = true;
                                return;
                            }
                    })),

                    // If file does not exist, notify of problem...
                    new Decorator(ret => !File.Exists(NewProfilePath),
                        new Action(delegate
                        {
                            LogMessage("fatal", "Profile '{0}' does not exist.  Download or unpack problem with profile?", NewProfilePath);
                            _isBehaviorDone = true;
                    })),

                    // Load the specified profile...
                    new Decorator(ret => ProfileBaseToLoad > 0,
                        new Action(context =>
                        {
                            TreeRoot.StatusText = "Loading profile '" + NewProfilePath + "'";
                            LogMessage("info", "Loading profile '{0}'", ProfileName);
                            ProfileManager.LoadNew(NewProfilePath, false);
                            new WaitContinue(TimeSpan.FromMilliseconds(300), ret => false, new ActionAlwaysSucceed());
                            _isBehaviorDone = true;
                        }))
             ));
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone);
            }
        }

        private static void Start_PB_Bot()
        {
            BotBase pbBotBase;
            BotManager.Instance.Bots.TryGetValue("ProfessionBuddy", out pbBotBase);
            if (pbBotBase != null && BotManager.Current != pbBotBase)
            {
                TreeRoot.Stop();
                BotManager.Instance.SetCurrent(pbBotBase);
                new Sleep(2000);
                if (ProfileName == "emptymb600")
                    ProfileManager.LoadNew(Path.Combine(PathToCavaProfiles, "Scripts\\Prof\\MB\\[PB]MB(Cava).xml"), false);
                if (ProfileName == "emptymb300")
                    ProfileManager.LoadNew(Path.Combine(PathToCavaProfiles, "Scripts\\Prof\\MB\\Free[PB]MB(Cava).xml"), false);
                TreeRoot.Start();
            }
        }
        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            ChangeBotBase = new Thread(Start_PB_Bot);
            if (!IsDone)
            {
                TreeRoot.GoalText = this.GetType().Name + ": In Progress";
            }
        }
        #endregion
    }

}
