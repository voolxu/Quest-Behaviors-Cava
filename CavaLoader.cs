using System;
using System.Collections.Generic;
using System.IO;
using CommonBehaviors.Actions;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.TreeSharp;

using Action = Styx.TreeSharp.Action;

// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.CavaLoader
{
    [CustomBehaviorFileName(@"Cava\CavaLoader")]
    // ReSharper disable once UnusedMember.Global
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
            [Setting, DefaultValue(0)]
            public int UseServer { get; set; }
            [Setting, DefaultValue(true)]
            public bool DisablePlugin { get; set; }
        }
 
        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
 
        // Private properties
        private static String NewProfilePath
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
        private int ProfileBaseToLoad { get; set; }

				
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
                            if (ProfileBaseToLoad == 7) { ProfileName = "emptymb600"; }
                            if (ProfileBaseToLoad == 8) { ProfileName = "emptymb300"; }
							if (ProfileBaseToLoad == 10) { ProfileName = "[N-Quest]Armageddoner_Reserved[Cava]"; }

                            if (ProfileBaseToLoad == 0)
                            {
                                _isBehaviorDone = true;
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

         public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                TreeRoot.GoalText = this.GetType().Name + ": In Progress";
            }
        }
        #endregion
    }

}
