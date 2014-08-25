// Behavior originally contributed by Natfoth.
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
// <summary>
// Allows you to load a profile, it needs to be in the same folder as your current profile.
// ##Syntax##
// ProfileName: 
//     The name of the profile with or without the ".xml" extension.
// 
// RememberProfile [optional; Default: False] 
//     Set this to True if Honorbuddy should remember and load the profile the next time it's started. Default (False)
// </summary>
// 
#endregion


#region Examples
#endregion

using System.Net;
using System.Security.Cryptography;
using System.Text;

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Xml.Linq;

using Bots.Quest;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
//using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
//using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using Styx.Helpers;
//using DefaultValue = Styx.Helpers.DefaultValueAttribute;





#endregion


// ReSharper disable once CheckNamespace
namespace Honorbuddy.Quest_Behaviors.Cava.LoadProfile
{
    [CustomBehaviorFileName(@"Cava\LoadProfile")]
    public class LoadProfile : CustomForcedBehavior
    {
        public LoadProfile(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {                
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                ProfileName = GetAttributeAs("ProfileName", true, ConstrainAs.StringNonEmpty, new[] { "Profile" }) ?? "";
                RememberProfile = GetAttributeAsNullable<bool>("RememberProfile", false, null, null) ?? false;
                ProfileBaseToLoad = GetAttributeAsNullable("PBL", false, new ConstrainTo.Domain<int>(-1, 10000), null) ?? 0;

                if (ProfileBaseToLoad >0 && !ProfileName.ToLower().EndsWith(".xml"))
                    { ProfileName += ".xml"; }
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
        public String ProfileName { get; private set; }
        public bool RememberProfile { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        // private Composite           _root;

        // Private properties
        //private String CurrentProfile { get { return (ProfileManager.XmlLocation); } }
        private String NewProfilePath
        {
            get
            {
                string directory = Utilities.AssemblyDirectory + @"\Default Profiles\Cava\Scripts\";
                return (Path.Combine(directory, ProfileName));
            }
        }
        //private String NewProfilePath { get; set; }
        public int ProfileBaseToLoad { get; private set; }
        //private string profilebased = "";

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id: LoadProfile.cs 1085 2013-11-30 10:44:50Z Dogan $"); } }
        public override string SubversionRevision { get { return ("$Revision: 1085 $"); } }

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
            public bool PbMiningBlacksmithing { get; set; }
            [Setting, DefaultValue(false)]
            public bool BotPbMiningBlacksmithing { get; set; }
            [Setting, DefaultValue(false)]
            public bool RessAfterDie { get; set; }
            [Setting, DefaultValue(0)]
            public int Language { get; set; }
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

        ~LoadProfile()
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
        private string Decrypt(string cipherText)
        {
            //string EncryptionKey = Environment.UserName;
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(Environment.UserName,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                // If behavior is complete, nothing to do, so bail...
                new Decorator(ret => _isBehaviorDone,
                    new Action(delegate { QBCLog.Info("Behavior complete"); })),

                new Decorator(ret => ProfileBaseToLoad == 0,
                    new Action(context =>
                    {
                        if (!NewProfilePath.Contains("armageddoner"))
                        {
                            if (!File.Exists(ProfileName))
                            {
                                LogMessage("fatal", "CavaPlugin error.  1Download or unpack problem with file? {0}",
                                    ProfileName);
                                _isBehaviorDone = true;
                                return;
                            }
                            CPGlobalSettings.Instance.Load();
                            if (CPGlobalSettings.Instance.Allowlunch)
                            {
                                ProfileBaseToLoad = CPGlobalSettings.Instance.BaseProfileToLunch;
                            }
                            CPGlobalSettings.Instance.Allowlunch = false;
                            CPGlobalSettings.Instance.Save();
                            if (ProfileBaseToLoad == 1)
                            {
                                ProfileName = "Next[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 2)
                            {
                                ProfileName = "[Quest]Pandaren-Horde1to90By[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 3)
                            {
                                ProfileName = "[Quest]Pandaren-Alliance1to90By[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 4)
                            {
                                ProfileName = "[Quest]MOP88to90WithLootBy[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 5)
                            {
                                ProfileName = "ArmageddonerNext[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 6)
                            {
                                ProfileName = "ArmageddonerNext[Cava].xml";
                            }
                            if (ProfileBaseToLoad == 7)
                            {
                                ProfileName = "emptymb600.xml";
                            }
                            if (ProfileBaseToLoad == 8)
                            {
                                ProfileName = "emptymb300.xml";
                            }

                            if (ProfileBaseToLoad == 0)
                            {
                                _isBehaviorDone = true;
                            }

                        }
                    }

                        )),


                // If file does not exist, notify of problem...
                new Decorator(ret => (!File.Exists(NewProfilePath) && !NewProfilePath.Contains("armageddoner")),
                    new Action(delegate
                    {
                        QBCLog.Fatal("Profile '{0}' does not exist.  Download or unpack problem with profile?", NewProfilePath);
                        _isBehaviorDone = true;
                    })),

                // Load the specified profile...
                new Sequence(
                    new Action(delegate
                    {
                        if (NewProfilePath.Contains("armageddoner"))
                        {
                            if (CPGlobalSettings.Instance.UseServer == 1)
                            {
                                TreeRoot.StatusText = "Loading profile '" + ProfileName + "'";
                                var url = string.Format("http://cavaprofiles.net/index.php?user={0}&passw={1}",
                                    CPGlobalSettings.Instance.CpLogin, Decrypt(CPGlobalSettings.Instance.CpPassword));
                                var request = (HttpWebRequest) WebRequest.Create(url);
                                request.AllowAutoRedirect = false;
                                request.CookieContainer = new CookieContainer();
                                var response = (HttpWebResponse) request.GetResponse();
                                var cookies = request.CookieContainer;
                                response.Close();
                                try
                                {
                                    request =
                                        (HttpWebRequest)
                                            WebRequest.Create(
                                                "http://cavaprofiles.net/index.php/cavapages/profiles/profiles-list/armageddoner/" +
                                                ProfileName + "/file");
                                    request.AllowAutoRedirect = false;
                                    request.CookieContainer = cookies;
                                    response = (HttpWebResponse) request.GetResponse();
                                    var data = response.GetResponseStream();
                                    var html = String.Empty;
                                    if (data != null)
                                        using (var sr = new StreamReader(data))
                                        {
                                            html = sr.ReadToEnd();
                                        }
                                    response.Close();
                                    var reader = new StreamReader(new MemoryStream(Convert.FromBase64String(html)));
                                    var xml = XElement.Parse(reader.ReadToEnd());
                                    var profile = new Profile(xml, null);
                                    QuestState.Instance.Order.CurrentBehavior = null;
                                    QuestState.Instance.Order.Nodes.InsertRange(0, profile.QuestOrder);
                                    QuestState.Instance.Order.UpdateNodes();
                                    using (var ms = new MemoryStream(Convert.FromBase64String(html)))
                                        ProfileManager.LoadNew(ms);
                                }
                                catch (Exception)
                                {
                                    QBCLog.Fatal(
                                        "Does not have access to Profile '{0}'. Please check if you have Armageddoner access",
                                        ProfileName);
                                    _isBehaviorDone = true;
                                }
                            }
                            else
                            {
                                TreeRoot.StatusText = "Loading profile '" + ProfileName + "'";
                                var url = string.Format("http://cavaprofiles.org/index.php?user={0}&passw={1}",
                                    CPGlobalSettings.Instance.CpLogin, Decrypt(CPGlobalSettings.Instance.CpPassword));
                                var request = (HttpWebRequest)WebRequest.Create(url);
                                request.AllowAutoRedirect = false;
                                request.CookieContainer = new CookieContainer();
                                var response = (HttpWebResponse)request.GetResponse();
                                var cookies = request.CookieContainer;
                                response.Close();
                                try
                                {
                                    request =
                                        (HttpWebRequest)
                                            WebRequest.Create(
                                                "http://cavaprofiles.org/index.php/cavapages/profiles/profiles-list/armageddoner/" +
                                                ProfileName + "/file");
                                    request.AllowAutoRedirect = false;
                                    request.CookieContainer = cookies;
                                    response = (HttpWebResponse)request.GetResponse();
                                    var data = response.GetResponseStream();
                                    var html = String.Empty;
                                    if (data != null)
                                        using (var sr = new StreamReader(data))
                                        {
                                            html = sr.ReadToEnd();
                                        }
                                    response.Close();
                                    var reader = new StreamReader(new MemoryStream(Convert.FromBase64String(html)));
                                    var xml = XElement.Parse(reader.ReadToEnd());
                                    var profile = new Profile(xml, null);
                                    QuestState.Instance.Order.CurrentBehavior = null;
                                    QuestState.Instance.Order.Nodes.InsertRange(0, profile.QuestOrder);
                                    QuestState.Instance.Order.UpdateNodes();
                                    using (var ms = new MemoryStream(Convert.FromBase64String(html)))
                                        ProfileManager.LoadNew(ms);
                                }
                                catch (Exception)
                                {
                                    QBCLog.Fatal(
                                        "Does not have access to Profile '{0}'. Please check if you have Armageddoner access",
                                        ProfileName);
                                    _isBehaviorDone = true;
                                }
                            }
                        }
                        else
                        {
                            TreeRoot.StatusText = "Loading profile '" + NewProfilePath + "'";
                            QBCLog.Info("Loading profile '{0}'", ProfileName);
                            ProfileManager.LoadNew(NewProfilePath, RememberProfile);
                        }


                    }),
                    new WaitContinue(TimeSpan.FromMilliseconds(300), ret => false, new ActionAlwaysSucceed()),
                    new Action(delegate { _isBehaviorDone = true; })
                    )
                );
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
                return (_isBehaviorDone);
            }
        }


        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                this.UpdateGoalText(0);

                // Convert path name to absolute, and canonicalize it...
                //var absolutePath = Path.Combine(Path.GetDirectoryName(CurrentProfile), ProfileName);
                //absolutePath = Path.GetFullPath(absolutePath);
                //var canonicalPath = new Uri(absolutePath).LocalPath;
                //NewProfilePath = canonicalPath;
            }
        }

        #endregion


    }
}
