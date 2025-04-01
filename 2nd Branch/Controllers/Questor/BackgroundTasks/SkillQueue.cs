extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class SkillQueue
    {
        #region Constructors

        static SkillQueue()
        {
            Interlocked.Increment(ref SkillQueueInstances);
            State.CurrentSkillQueueState = SkillQueueState.Begin;
        }

        #endregion Constructors

        #region Fields

        private static DateTime _nextSkillCheck;

        private static readonly Dictionary<char, int> RomanDictionary = new Dictionary<char, int>
        {
            {'I', 1},
            {'V', 5}
        };

        private static readonly int SkillQueueInstances;
        private static DateTime _lastPulse = DateTime.UtcNow;
        private static List<string> _myRawSkillPlan = new List<string>();
        private static DateTime _nextRetrieveSkillQueueInfoAction = DateTime.MinValue;
        private static XDocument _xmlSkillPreReqs;
        private static Dictionary<DirectSkill, int> mySkillPlanAsDirectSkill = new Dictionary<DirectSkill, int>();
        private static Dictionary<string, int> mySkillPlanAsText = new Dictionary<string, int>();

        #endregion Fields

        #region Properties

        private static List<DirectSkill> MyCharacterSheetSkills { get; set; }

        private static List<DirectSkill> MySkillQueue { get; set; }

        #endregion Properties

        #region Methods

        public static bool AddPlannedSkillsToSkillQueue()
        {
            if (!ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                return false;

            if (!ESCache.Instance.DirectEve.Skills.AreMySkillsReady)
            {
                Log.WriteLine("SkillQueueController: RefreshMySkills");
                Time.Instance.LastRefreshMySkills = DateTime.UtcNow;
                ESCache.Instance.DirectEve.Skills.RefreshMySkills();
                return false;
            }

            if (!ESCache.Instance.DirectEve.Skills.IsReady)
            {
                if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillQueueController: if (!ESCache.Instance.DirectEve.Skills.IsReady)");
                return false;
            }

            if (!ESCache.Instance.DirectEve.Me.IsOmegaClone && ESCache.Instance.DirectEve.Skills.TotalSkillPoints > 5000000)
            {
                //
                // we cant add any more skills after 5million if we are an alpha: only Omega clones can train above 5mil SP.
                //
                return true;
            }

            if (10 > ESCache.Instance.DirectEve.Skills.GetNumberOfSkillsInQueue)
            {
                ImportRawSkillPlan();
                ReadySkillPlanAsText();

                int skillNum = 0;
                foreach (KeyValuePair<string, int> skillInSkillPlan in mySkillPlanAsText)
                {
                    skillNum++;
                    Log.WriteLine("[" + skillNum + "] SkillPlan Skill [" + skillInSkillPlan.Key + "] Level [" + skillInSkillPlan.Value + "]");
                    foreach (DirectSkill skillInMyHead in ESCache.Instance.DirectEve.Skills.MySkills)
                        if (skillInMyHead.TypeName == skillInSkillPlan.Key)
                            if (skillInMyHead.Level < skillInMyHead.MaxCloneSkillLevel)
                            {
                                if (10 > ESCache.Instance.DirectEve.Skills.GetNumberOfSkillsInQueue)
                                    break;

                                Log.WriteLine("[" + skillInMyHead.TypeName + "][" + skillInMyHead.Level + "] is less than MaxCloneSkillLevel [" + skillInMyHead.MaxCloneSkillLevel + "]");
                                if (skillInMyHead.Level < skillInSkillPlan.Value)
                                {
                                    Log.WriteLine("[" + skillInMyHead.TypeName + "][" + skillInMyHead.Level + "] is less than skillInSkillPlan.Level [" + skillInSkillPlan.Value + "]");
                                    if (ESCache.Instance.DirectEve.Skills.MySkillQueue.All(i => i.TypeName != skillInSkillPlan.Key))
                                    {
                                        Log.WriteLine("[" + skillInMyHead.TypeName + "][" + (skillInMyHead.Level + 1) + "] Add Skill to end of queue.");
                                        ESCache.Instance.DirectEve.Skills.AddSkillToEndOfQueue(skillInMyHead.TypeId);
                                        Time.Instance.LastSkillQueueModification = DateTime.UtcNow;
                                        Time.Instance.LastSkillQueueCheck = DateTime.UtcNow;
                                    }
                                }
                            }
                }

                Time.Instance.LastSkillQueueCheck = DateTime.UtcNow;
                return true;
            }

            Time.Instance.LastSkillQueueCheck = DateTime.UtcNow;
            return true;
        }

        public static bool ChangeSkillQueueState(SkillQueueState state)
        {
            try
            {
                if (State.CurrentSkillQueueState != state)
                {
                    Log.WriteLine("New SkillQueueState [" + state + "]");
                    State.CurrentSkillQueueState = state;
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ClearSystemSpecificSettings()
        {
            try
            {
                _myRawSkillPlan.Clear();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool DoWeHaveTheRightPrerequisites(int skillID)
        {
            try
            {
                if (_xmlSkillPreReqs == null)
                {
                    _xmlSkillPreReqs = XDocument.Load(Settings.Instance.Path + "\\Skill_Prerequisites.xml");
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("Skill_Prerequisites.xml Loaded.");
                }
            }
            catch (Exception)
            {
                if (DebugConfig.DebugSkillQueue) Log.WriteLine("Skill_Prerequisites.xml exception -- does the file exist?");
                return false;
            }

            foreach (XElement skills in _xmlSkillPreReqs.Descendants("skill"))
                if (skillID.ToString().Equals(skills.Attribute("id").Value))
                {
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("skillID.ToString().Equals(skills.Attribute(\"id\").Value == TRUE");
                    foreach (XElement preRegs in skills.Descendants("preqskill"))
                        if (MyCharacterSheetSkills.Any(i => i.TypeId.ToString().Equals(preRegs.Attribute("id").Value)))
                        {
                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("We have this Prerequisite: " + preRegs.Attribute("id").Value);
                            if (MyCharacterSheetSkills.Any(i => i.TypeId.ToString().Equals(preRegs.Attribute("id").Value) && i.Level < Convert.ToInt32(preRegs.Value)))
                            {
                                if (DebugConfig.DebugSkillQueue) Log.WriteLine("We don't meet the required level on this skill: " + preRegs.Attribute("id").Value);
                                return false;
                            }

                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("We meet the required skill level on this skill: " + preRegs.Attribute("id").Value);
                        }
                        else
                        {
                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("We don't have this prerequisite: " + preRegs.Attribute("id").Value);
                        }
                    // this is also good for skills with no pre requirements
                    return true;
                }
            return false;
            // not in list which is unlikely
        }

        public static bool DoWeHaveThisSkillAlreadyInOurItemHangar(int skillID)
        {
            if (!ESCache.Instance.InStation) return false;
            IEnumerable<DirectItem> items = ESCache.Instance.ItemHangar.Items.Where(k => k.TypeId == skillID).ToList();
            if (items.Any())
            {
                if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillPlan.DoWeHaveThisSkillAlreadyInOurItemHangar: We already have this skill in our hangar " + skillID);
                return true;
            }

            if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillPlan.DoWeHaveThisSkillAlreadyInOurItemHangar: We don't have this skill in our hangar " + skillID);
            return false;
        }

        public static bool ImportRawSkillPlan()
        {
            if (_myRawSkillPlan == null || _myRawSkillPlan.Count == 0)
            {
                if (_myRawSkillPlan == null)
                    _myRawSkillPlan = new List<string>();

                _myRawSkillPlan.Clear();

                try
                {
                    string skillPlanFile = Path.Combine(Settings.Instance.Path, "QuestorSettings\\skillPlan-", ESCache.Instance.DirectEve.Me.Name, ".txt").ToLower();

                    if (!File.Exists(skillPlanFile))
                    {
                        string genericSkillPlanFile = Path.Combine(Settings.Instance.Path, "QuestorSettings\\skillPlan.txt").ToLower();
                        Log.WriteLine("importskillplan: Missing Character Specific skill plan file [" + skillPlanFile + "], trying generic file [" + genericSkillPlanFile + "]");
                        skillPlanFile = genericSkillPlanFile;
                    }

                    if (!File.Exists(skillPlanFile))
                    {
                        Log.WriteLine("importskillplan: Missing Generic skill plan file [" + skillPlanFile + "]");
                        return false;
                    }

                    Log.WriteLine("importskillplan: Loading SkillPlan from [" + skillPlanFile + "]");

                    // Use using StreamReader for disposing.
                    using (StreamReader readTextFile = new StreamReader(skillPlanFile))
                    {
                        string line;
                        while ((line = readTextFile.ReadLine()) != null)
                            _myRawSkillPlan.Add(line);
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("importskillplan: Exception was: [" + exception + "]");
                    return false;
                }
            }

            return true;
        }

        public static bool InjectSkillBook(int skillID)
        {
            try
            {
                IEnumerable<DirectItem> items = ESCache.Instance.ItemHangar.Items.Where(k => k.TypeId == skillID).ToList();
                if (DoWeHaveThisSkillAlreadyInOurItemHangar(skillID))
                {
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillBook [" + skillID + "] found in ItemHangar");
                    DirectItem SkillBookToInject = items.FirstOrDefault(s => s.TypeId == skillID);
                    if (SkillBookToInject != null)
                    {
                        if (MyCharacterSheetSkills != null && !MyCharacterSheetSkills.Any(i => i.TypeName == SkillBookToInject.TypeName || i.GivenName == SkillBookToInject.TypeName))
                        {
                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillBook:  GivenName [" + SkillBookToInject.GivenName + "] TypeName [" + SkillBookToInject.TypeName + "] is being injected");
                            if (DoWeHaveTheRightPrerequisites(SkillBookToInject.TypeId))
                            {
                                SkillBookToInject.InjectSkill();
                                State.CurrentSkillQueueState = SkillQueueState.ReadCharacterSheetSkills;
                                return true;
                            }

                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("Skillbook: We don't have the right Prerequisites for " + SkillBookToInject.GivenName);
                        }

                        if (MyCharacterSheetSkills != null && MyCharacterSheetSkills.Any(i => i.TypeName == SkillBookToInject.TypeName))
                        {
                            if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillBook:  TypeName [" + SkillBookToInject.TypeName + "] is already injected, why are we trying to do so again? aborting injection attempt ");
                            return true;
                        }
                    }

                    return false;
                }
                Log.WriteLine("We don't have this skill in our hangar");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: SkillQueue");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void ProcessState()
        {
            if (!ESCache.Instance.DirectEve.Session.IsReady)
                return;

            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 3500)
                return;
            _lastPulse = DateTime.UtcNow;

            if (DateTime.UtcNow < Time.Instance.LastSkillQueueModification.AddMilliseconds(ESCache.Instance.RandomNumber(6000, 15000)))
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: if (DateTime.UtcNow < Time.Instance.LastSkillQueueModification.AddMilliseconds(ESCache.Instance.RandomNumber(6000, 15000)))");
                return;
            }

            if (DebugConfig.DebugSkillQueue)
                Log.WriteLine("DebugSkillQueue: SkillQueue ProcessState");

            if (!ESCache.Instance.EveAccount.AutoSkillTraining)
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: AutoSkillTraining [" + ESCache.Instance.EveAccount.AutoSkillTraining + "]");
               return;
            }

            if (!ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: !ESCache.Instance.InStation && !ESCache.Instance.InSpace");
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: if (ESCache.Instance.InSpace)");
                return;
            }

            if (Time.Instance.Started_DateTime.AddSeconds(6) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: if (Time.Instance.QuestorStarted_DateTime.AddSeconds(6) > DateTime.UtcNow)");
                return;
            }

            if (ESCache.Instance.InStation && Time.Instance.LastDockAction.AddSeconds(10) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugSkillQueue)
                    Log.WriteLine("DebugSkillQueue: LastDockAction [" + Time.Instance.LastDockAction.ToShortTimeString() + "] waiting");
                return;
            }

            // Skill check
            if (_nextSkillCheck < DateTime.UtcNow)
            {
                _nextSkillCheck = DateTime.UtcNow.AddMinutes(new Random().Next(10, 15));

                if (ESCache.Instance.DirectEve.Skills.AreMySkillsReady)
                {
                    bool skillInTraining = ESCache.Instance.DirectEve.Skills.SkillInTraining;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TrainingNow), skillInTraining);
                    if (skillInTraining)
                    {
                        DirectSkill last = ESCache.Instance.DirectEve.Skills.MySkillQueue.LastOrDefault();
                        if (last != null)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MySkillQueueEnds), last.TrainingEndTime);
                    }
                }
            }

            switch (State.CurrentSkillQueueState)
            {
                case SkillQueueState.Idle:
                    if (DateTime.UtcNow > Time.Instance.LastSkillQueueCheck.AddMinutes(45))
                        ChangeSkillQueueState(SkillQueueState.Begin);

                    break;

                case SkillQueueState.Begin:
                    Log.WriteLine("[" + ESCache.Instance.EveAccount.MaskedCharacterName + "] Has [" + ESCache.Instance.DirectEve.Skills.TotalSkillPointsAsString + "] TotalSkillPoints trained");
                    ChangeSkillQueueState(SkillQueueState.LoadPlan);
                    break;

                case SkillQueueState.LoadPlan:
                    if (!ImportRawSkillPlan()) return;
                    ChangeSkillQueueState(SkillQueueState.CheckTrainingQueue);
                    break;

                case SkillQueueState.CheckTrainingQueue:
                    if (!AddPlannedSkillsToSkillQueue()) return;
                    ChangeSkillQueueState(SkillQueueState.Idle);
                    break;
            }
        }

        public static void ReadySkillPlanAsDirectSkill()
        {
            if (mySkillPlanAsDirectSkill == null || mySkillPlanAsDirectSkill.Count == 0)
                try
                {
                    if (mySkillPlanAsDirectSkill == null)
                        mySkillPlanAsDirectSkill = new Dictionary<DirectSkill, int>();

                    mySkillPlanAsDirectSkill.Clear();

                    foreach (KeyValuePair<string, int> plannedSkillAsText in mySkillPlanAsText)
                        foreach (DirectSkill SkillInMyHead in ESCache.Instance.DirectEve.Skills.MySkills)
                            if (plannedSkillAsText.Key == SkillInMyHead.TypeName)
                            {
                                //mySkillPlanAsDirectSkill.
                            }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }
        }

        public static void ReadySkillPlanAsText()
        {
            if (mySkillPlanAsText == null || mySkillPlanAsText.Count == 0)
                try
                {
                    if (mySkillPlanAsText == null)
                        mySkillPlanAsText = new Dictionary<string, int>();

                    mySkillPlanAsText.Clear();
                    int i = 0;
                    foreach (string importedSkill in _myRawSkillPlan)
                    {
                        i++;
                        string romanNumeral = ParseRomanNumeral(importedSkill);
                        string skillName = importedSkill.Substring(0, importedSkill.Length - CountNonSpaceChars(romanNumeral));
                        skillName = skillName.Trim();
                        int levelPlanned = Decode(romanNumeral);
                        if (mySkillPlanAsText.ContainsKey(skillName))
                        {
                            if (mySkillPlanAsText.FirstOrDefault(x => x.Key == skillName).Value < levelPlanned)
                            {
                                mySkillPlanAsText.Remove(skillName);
                                mySkillPlanAsText.Add(skillName, levelPlanned);
                            }
                            continue;
                        }

                        mySkillPlanAsText.Add(skillName, levelPlanned);
                        if (DebugConfig.DebugSkillQueue) Log.WriteLine("Skills.readySkillPlan [" + i + "]" + importedSkill + "] LevelPlanned[" + levelPlanned + "][" + romanNumeral + "]");
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }
        }

        public static bool RetrieveSkillQueueInfo()
        {
            try
            {
                if (DateTime.UtcNow > _nextRetrieveSkillQueueInfoAction)
                {
                    MySkillQueue = ESCache.Instance.DirectEve.Skills.MySkillQueue;
                    _nextRetrieveSkillQueueInfoAction = DateTime.UtcNow.AddSeconds(10);
                    if (MySkillQueue != null)
                    {
                        if (DebugConfig.DebugSkillQueue) Log.WriteLine("MySkillQueue is not null, continue");
                        return true;
                    }

                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("RetrieveSkillQueueInfo: MySkillQueue is null, how? retry in 10 sec");
                    return true;
                }

                if (DebugConfig.DebugSkillQueue) Log.WriteLine("Waiting...");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("SkillPlan: Exception [" + exception + "]");
                return false;
            }
        }

        /**
        public static bool BuySkill(int skillID, string typeName)
        {
            try
            {
                if (!ESCache.Instance.InStation) return false;
                if (DateTime.UtcNow < _nextSkillTrainingAction)
                {
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillPlan.buySkill: Next Skill Training Action is set to continue in [" + Math.Round(_nextSkillTrainingAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "] seconds");
                    return false;
                }

                if (buyingSkillTypeID != 0)
                {
                    buyingIterator++;

                    if (buyingIterator > 20)
                    {
                        Log.WriteLine("buySkill: buying iterator < 20 with SkillID" + skillID);
                        buyingSkillTypeID = 0;
                        buyingSkillTypeName = string.Empty;
                        buyingIterator = 0;
                        return true;
                    }
                    // only buy if we do not have it already in our itemhangar
                    if (DoWeHaveThisSkillAlreadyInOurItemHangar(skillID))
                    {
                        Log.WriteLine("buySkill: We already purchased this skill" + skillID);
                        buyingSkillTypeID = 0;
                        buyingSkillTypeName = string.Empty;
                        buyingIterator = 0;
                        return true;
                    }

                    DirectMarketWindow marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
                    if (true)
                    {
                        if (marketWindow == null)
                        {
                            _nextSkillTrainingAction = DateTime.UtcNow.AddSeconds(10);
                            Log.WriteLine("Opening market window");
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                            Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                            return false;
                        }

                        if (!marketWindow.IsReady)
                        {
                            _nextSkillTrainingAction = DateTime.UtcNow.AddSeconds(3);
                            return false;
                        }

                        if (marketWindow.DetailTypeId != skillID)
                        {
                            // No, load the right order
                            marketWindow.LoadTypeId(skillID);
                            Log.WriteLine("Loading market with right typeid ");
                            _nextSkillTrainingAction = DateTime.UtcNow.AddSeconds(6);
                            return false;
                        }

                        // Get the median sell price
                        DirectInvType type;
                        type = ESCache.Instance.DirectEve.GetInvType(skillID);
                        double? maxPrice = 0;
                        if (type != null)
                        {
                            //maxPrice = type.AveragePrice * 10;
                            Log.WriteLine("maxPrice " + maxPrice.ToString());
                            // Do we have orders?
                            //IEnumerable<DirectOrder> orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.Price < maxPrice).ToList();
                            IEnumerable<DirectOrder> orders = marketWindow.SellOrders.Where(o => o.StationId == ESCache.Instance.DirectEve.Session.StationId).ToList();
                            if (orders.Any())
                            {
                                DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();
                                if (order != null)
                                {
                                    //
                                    // do we have the isk to perform this transaction? - not as silly as you think, some skills are expensive!
                                    //
                                    if (order.Price < ESCache.Instance.MyWalletBalance)
                                    {
                                        order.Buy(1, DirectOrderRange.Station);
                                        Log.WriteLine("Buying skill with typeid & waiting 20 seconds ( to ensure we do not buy the skillbook twice ) " + skillID);
                                        buyingSkillTypeID = 0;
                                        buyingSkillTypeName = string.Empty;
                                        buyingIterator = 0;
                                        // Wait for the order to go through
                                        _nextRetrieveCharactersheetInfoAction = DateTime.MinValue;
                                        // ensure we get the character sheet update
                                        _nextSkillTrainingAction = DateTime.UtcNow.AddSeconds(20);
                                        return true;
                                    }

                                    Log.WriteLine("We do not have enough isk to purchase [" + typeName + "] at [" + Math.Round(order.Price, 0) + "] isk. We have [" + Math.Round(ESCache.Instance.MyWalletBalance, 0) + "] isk");
                                    buyingSkillTypeID = 0;
                                    buyingSkillTypeName = string.Empty;
                                    buyingIterator = 0;
                                    return false;
                                }

                                Log.WriteLine("order was null.");
                                buyingSkillTypeID = 0;
                                buyingSkillTypeName = string.Empty;
                                buyingIterator = 0;
                                return false;
                            }

                            Log.WriteLine("No orders for the skill could be found with a price less than 10 * the AveragePrice");
                            buyingSkillTypeID = 0;
                            buyingSkillTypeName = string.Empty;
                            buyingIterator = 0;
                            return false;
                        }

                        Log.WriteLine("no skill could be found with a typeid of [" + buyingSkillTypeID + "]");
                        buyingSkillTypeID = 0;
                        buyingSkillTypeName = string.Empty;
                        buyingIterator = 0;
                        return false;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }
        **/

        public static int SkillLevel(string SkillToLookFor)
        {
            try
            {
                if (MyCharacterSheetSkills == null || MyCharacterSheetSkills.Count == 0)
                {
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillLevel: if (!MyCharacterSheetSkills.Any())");

                    MyCharacterSheetSkills = ESCache.Instance.DirectEve.Skills.MySkills;
                }

                foreach (DirectSkill knownskill in MyCharacterSheetSkills)
                    if (knownskill.TypeName == SkillToLookFor)
                        return knownskill.Level;

                if (DebugConfig.DebugSkillQueue) Log.WriteLine("SkillLevel: We do not have [" + SkillToLookFor + "] yet");
                return 0;
            }
            catch (Exception exception)
            {
                Log.WriteLine("SkillLevel: Exception [" + exception + "]");
                return 0;
            }
        }

        private static int CountNonSpaceChars(string value)
        {
            return value.Count(c => !char.IsWhiteSpace(c));
        }

        private static int Decode(string roman)
        {
            roman = roman.ToUpper();
            int total = 0, minus = 0;

            for (int icount2 = 0; icount2 < roman.Length; icount2++) // Iterate through characters.
            {
                int thisNumeral = RomanDictionary[roman[icount2]] - minus;

                if (icount2 >= roman.Length - 1 ||
                    thisNumeral + minus >= RomanDictionary[roman[icount2 + 1]])
                {
                    total += thisNumeral;
                    minus = 0;
                }
                else
                {
                    minus = thisNumeral;
                }
            }
            return total;
        }

        private static int GetInvTypeId(string moduleName)
        {
            try
            {
                if (_xmlSkillPreReqs == null)
                {
                    _xmlSkillPreReqs = XDocument.Load(Settings.Instance.Path + "\\Skill_Prerequisites.xml");
                    if (DebugConfig.DebugSkillQueue) Log.WriteLine("Skill_Prerequisites.xml Loaded.");
                }

                return Convert.ToInt32(_xmlSkillPreReqs.Element("document").Elements("skill").Where(i => i.Attribute("name").Value.ToLower() == moduleName.ToLower()).Select(e => e.Attribute("id").Value).FirstOrDefault());
            }
            catch (Exception e)
            {
                Log.WriteLine("Exception:  [" + e.Message + "]");
                return 0;
            }
        }

        private static string ParseRomanNumeral(string importedSkill)
        {
            string subString = importedSkill.Substring(importedSkill.Length - 3);

            try
            {
                bool startsWithWhiteSpace = char.IsWhiteSpace(subString, 0); // 0 = first character
                if (startsWithWhiteSpace || char.IsLower(subString, 0))
                {
                    subString = importedSkill.Substring(importedSkill.Length - 2);
                    startsWithWhiteSpace = char.IsWhiteSpace(subString, 0); // 0 = first character
                    if (startsWithWhiteSpace)
                    {
                        subString = importedSkill.Substring(importedSkill.Length - 1);
                        startsWithWhiteSpace = char.IsWhiteSpace(subString, 0); // 0 = first character
                        if (startsWithWhiteSpace)
                            return subString;
                        return subString;
                    }
                    return subString;
                }
                return subString;
            }
            catch (Exception exception)
            {
                Log.WriteLine("ParseRomanNumeral: Exception was [" + exception + "]");
            }
            return subString;
        }

        #endregion Methods
    }

    public class SkillQueueItem
    {
        #region Methods

        public SkillQueueItem Clone()
        {
            SkillQueueItem stuffToHaul = new SkillQueueItem
            {
                TypeId = TypeId,
                Quantity = Quantity,
                Description = Description
            };
            return stuffToHaul;
        }

        #endregion Methods

        #region Fields

        //
        // use priority levels to decide what to do 1st vs last, 1 being the highest priority.
        //
        private readonly int Priority = 5;

        private string _name = string.Empty;

        #endregion Fields

        #region Constructors

        public SkillQueueItem()
        {
        }

        public SkillQueueItem(XElement xmlSkillQueueItem)
        {
            try
            {
                TypeId = (int)xmlSkillQueueItem.Attribute("typeId");
                Quantity = (int)xmlSkillQueueItem.Attribute("quantity");
                Description = (string)xmlSkillQueueItem.Attribute("description") ?? (string)xmlSkillQueueItem.Attribute("typeId");

                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    Log.WriteLine("ERROR: xmlInventoryItem.TypeId: " + TypeId + " was NOT found in type storage. Fix your xmlInventoryItem type ids.");
                /// else
                ///    Log.WriteLine("xmlInventoryItem.TypeId: " + TypeId + " was found in type storage");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public string Description { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                    return _name;

                string ret = string.Empty;
                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    return ret;

                DirectInvType invType = ESCache.Instance.DirectEve.GetInvType(TypeId);

                if (invType == null)
                    return ret;

                string typeName = invType.TypeName;
                _name = typeName;
                return typeName;
            }
        }

        public int Quantity { get; set; }
        public int TypeId { get; private set; }

        #endregion Properties
    }
}