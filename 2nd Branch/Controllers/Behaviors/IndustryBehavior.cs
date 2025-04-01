extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Questor.Behaviors
{
    public class IndustryBehavior
    {
        public IndustryBehavior()
        {
            ResetStatesToDefaults();
        }

        public static XElement xmlIndustryInputs = null;
        public static XElement xmlIndustryOutputs = null;

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: IndustryBehavior");

                //HomeBookmarkName =
                //    (string)CharacterSettingsXml.Element("HomeBookmarkName") ?? (string)CharacterSettingsXml.Element("HomeBookmarkName") ??
                //    (string)CommonSettingsXml.Element("HomeBookmarkName") ?? (string)CommonSettingsXml.Element("HomeBookmarkName") ?? "HomeBookmarkName";
                //Log.WriteLine("LoadSettings: IndustryBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
                /**
                AllowMiningInAsteroidBelts =
                    (bool?)CharacterSettingsXml.Element("allowMiningInAsteroidBelts") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInAsteroidBelts") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInAsteroidBelts [" + AllowMiningInAsteroidBelts + "]");
                AllowMiningInMiningAnomolies =
                    (bool?)CharacterSettingsXml.Element("allowMiningInMiningAnomolies") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInMiningAnomolies") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInMiningAnomolies [" + AllowMiningInMiningAnomolies + "]");
                AllowMiningInMiningSignatures =
                    (bool?)CharacterSettingsXml.Element("allowMiningInMiningSignatures") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInMiningSignatures") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInMiningSignatures [" + AllowMiningInMiningSignatures + "]");
                **/


                xmlIndustryInputs = Settings.CharacterSettingsXml.Element("industryInputs") ?? Settings.CommonSettingsXml.Element("industryInputs");
                if (xmlIndustryInputs != null)
                {
                    foreach (XElement xmlIndividualIndustryInput in xmlIndustryInputs.Elements("industryInput"))
                    {
                        DirectItem individualIndustryInputToAdd = new DirectItem(ESCache.Instance.DirectEve);
                        individualIndustryInputToAdd.TypeId = (int)xmlIndividualIndustryInput.Attribute("typeId");
                        Log.WriteLine("Adding industryInput [" + individualIndustryInputToAdd.TypeName + "] TypeId [" + individualIndustryInputToAdd.TypeId + "] Quantity [" + (long)xmlIndividualIndustryInput.Attribute("quantity") + "]");
                    }
                }

                xmlIndustryOutputs = Settings.CharacterSettingsXml.Element("industryOutputs") ?? Settings.CommonSettingsXml.Element("industryOutputs");
                if (xmlIndustryOutputs != null)
                {
                    foreach (XElement xmlIndividualIndustryInput in xmlIndustryInputs.Elements("industryOutput"))
                    {
                        DirectItem individualIndustryOutputToAdd = new DirectItem(ESCache.Instance.DirectEve);
                        individualIndustryOutputToAdd.TypeId = (int)xmlIndividualIndustryInput.Attribute("typeId");
                        Log.WriteLine("Adding industryOutput [" + individualIndustryOutputToAdd.TypeName + "] TypeId [" + individualIndustryOutputToAdd.TypeId + "] Quantity [" + (long)xmlIndividualIndustryInput.Attribute("quantity") + "]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }



        public static bool ChangeIndustryBehaviorState(IndustryBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentIndustryBehaviorState != _StateToSet)
                {
                    if (_StateToSet == IndustryBehaviorState.GotoHomeBookmark)
                    {
                        Traveler.Destination = null;
                        State.CurrentTravelerState = States.TravelerState.Idle;
                    }

                    Log.WriteLine("New IndustryBehaviorState [" + _StateToSet + "]");
                    State.CurrentIndustryBehaviorState = _StateToSet;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static bool DoWeHaveTheInputsWeNeed
        {
            get
            {
                try
                {
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("DoWeHaveTheInputsWeNeed: Start");
                    if (ESCache.Instance.InStation)
                    {
                        //
                        // Do we want to be able to pull items from other hangars?
                        //
                        if (ESCache.Instance.ItemHangar == null)
                            return false;

                        if (!ESCache.Instance.ItemHangar.Items.Any())
                            return false;

                        if (ESCache.Instance.ItemHangar.Items.Any())
                        {
                            //
                            // Add checking for items here. We will need some XML settings similar to ammo: individual items and number of those items needed at minimum
                            //
                            if (xmlIndustryInputs != null)
                            {
                                foreach (XElement xmlIndividualIndustryInput in xmlIndustryInputs.Elements("industryInput"))
                                {
                                    try
                                    {
                                        DirectItem individualIndustryInput = new DirectItem(ESCache.Instance.DirectEve);
                                        individualIndustryInput.TypeId = (int)xmlIndividualIndustryInput.Attribute("typeId");
                                        if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == individualIndustryInput.TypeId && individualIndustryInput.Quantity > i.Quantity))
                                        {
                                            Log.WriteLine("DoWeHaveTheInputsWeNeed: Not Enough [" + individualIndustryInput.TypeName + "] in the hangar");
                                            return false;
                                        }

                                        continue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                        return false;
                                    }
                                }
                            }

                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static bool DoWeWantToCreateNewJob_AfterLoadingBluePrint(DirectItem blueprint, XElement xmlIndividualIndustryOutput)
        {
            try
            {
                //
                //How many slots do we have left?
                //
                if (!ESCache.Instance.OpenIndustryWindow()) return false;

                if (!DoesIndustryWindowHaveTheRightBlueprintLoaded(blueprint, xmlIndividualIndustryOutput))
                    return false;

                if (IndustryWindow == null) return false;

                if (IndustryWindow.used_slots > 0 && IndustryWindow.max_slots == IndustryWindow.used_slots)
                {
                    if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000, blueprint.ItemId.ToString())) Log.WriteLine("DeWeWantToCreateNewJobs: max_slots [" + IndustryWindow.max_slots + "] used_slots [" + IndustryWindow.used_slots + "] false");
                    return false;
                }

                if (IndustryWindow.BlueprintRunsRemaining == 0)
                {
                    Log.WriteLine("DeWeWantToCreateNewJobs: runsRemaining [" + IndustryWindow.BlueprintRunsRemaining + "] return false");
                    return false;
                }

                //are runs set correctly?
                int maxRunsToTryToMake = (int?)xmlIndividualIndustryOutput.Attribute("maxRunsToTryToMake") ?? 1;
                Log.WriteLine("runs [" + IndustryWindow.runs + "] maxRunsToTryToMake [" + maxRunsToTryToMake + "] maxRuns [" + IndustryWindow.maxRuns + "] BlueprintRunsRemaining [" + IndustryWindow.BlueprintRunsRemaining + "]");
                if (IndustryWindow.runs != Math.Min(IndustryWindow.BlueprintRunsRemaining, IndustryWindow.maxRuns))
                {
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("runs [" + IndustryWindow.runs + "] != Math.Min(IndustryWindow.BlueprintRunsRemaining [" + IndustryWindow.BlueprintRunsRemaining + "], IndustryWindow.maxRuns [" + IndustryWindow.maxRuns + "])");
                    if (IndustryWindow.runs != maxRunsToTryToMake)
                    {
                        if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (IndustryWindow.runs != maxRunsToTryToMake)");
                        if (IndustryWindow.runs > maxRunsToTryToMake)
                        {
                            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryWindow.runs [" + IndustryWindow.runs + "] greaterThan maxRunsToTryToMake [" + maxRunsToTryToMake + "]");
                            if (IndustryWindow.SingleLineEditIntegerControls.Any(i => i.Name == "JobRunsToMake"))
                            {
                                Log.WriteLine("runs [" + IndustryWindow.runs + "] maxRunsToTryToMake [" + maxRunsToTryToMake + "] clicking Down");
                                IndustryWindow.SingleLineEditIntegerControls.FirstOrDefault(i => i.Name == "JobRunsToMake").Down();
                                return false;
                            }

                            Log.WriteLine("No SingleLineEditIntegerControls named JobRunsToMake found!");
                            return false;
                        }

                        if (maxRunsToTryToMake > IndustryWindow.runs)
                        {
                            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryWindow.runs [" + IndustryWindow.runs + "] lessThan maxRunsToTryToMake [" + maxRunsToTryToMake + "]");
                            if (IndustryWindow.SingleLineEditIntegerControls.Any(i => i.Name == "JobRunsToMake"))
                            {
                                Log.WriteLine("runs [" + IndustryWindow.runs + "] maxRunsToTryToMake [" + maxRunsToTryToMake + "] clicking Up");
                                IndustryWindow.SingleLineEditIntegerControls.FirstOrDefault(i => i.Name == "JobRunsToMake").Up();
                                return false;
                            }

                            Log.WriteLine("No SingleLineEditIntegerControls named JobRunsToMake found!");
                            return false;
                        }
                    }
                }

                //
                // Do we have enough ISK to submit the jobs?
                //
                if (IndustryWindow.total_cost > (ESCache.Instance.DirectEve.Me.Wealth ?? 0))
                {
                    Log.WriteLine("DeWeWantToCreateNewJobs: Wealth [" + ESCache.Instance.DirectEve.Me.Wealth ?? 0 + "] total_cost [" + IndustryWindow.total_cost + "] return false");
                    return false;
                }
                else Log.WriteLine("DeWeWantToCreateNewJobs: Wealth [" + ESCache.Instance.DirectEve.Me.Wealth ?? 0 + "] total_cost [" + IndustryWindow.total_cost + "]");

                if (DirectEve.Interval(60000, 60000, blueprint.ItemId.ToString()))
                {
                    try
                    {
                        Log.WriteLine("max_slots [" + IndustryWindow.max_slots + "]");
                        Log.WriteLine("used_slots [" + IndustryWindow.used_slots + "]");
                        Log.WriteLine("BlueprintTypeID [" + IndustryWindow.BlueprintTypeID + "]");
                        try
                        {
                            if (IndustryWindow.OutputItem != null) Log.WriteLine("OutputItem [" + IndustryWindow.OutputItem.TypeName + "] TypeID [" + IndustryWindow.OutputItem.TypeId + "]");
                            else Log.WriteLine("OutputItem is null");
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }
                        Log.WriteLine("materialEfficiency [" + IndustryWindow.materialEfficiency + "]");
                        Log.WriteLine("timeEfficiency [" + IndustryWindow.timeEfficiency + "]");
                        Log.WriteLine("maxProductionLimit [" + IndustryWindow.maxProductionLimit + "]");
                        Log.WriteLine("runs [" + IndustryWindow.runs + "]");
                        Log.WriteLine("maxRuns [" + IndustryWindow.maxRuns + "]");
                        Log.WriteLine("distance [" + IndustryWindow.distance + "]");
                        Log.WriteLine("max_distance [" + IndustryWindow.max_distance + "]");
                        Log.WriteLine("probability [" + IndustryWindow.probability + "]");
                        Log.WriteLine("JobStatus [" + IndustryWindow.JobStatus + "]");
                        Log.WriteLine("BlueprintRunsRemaining [" + IndustryWindow.BlueprintRunsRemaining + "]");
                        Log.WriteLine("total_cost [" + IndustryWindow.total_cost + "]");
                        Log.WriteLine("Wealth [" + (ESCache.Instance.DirectEve.Me.Wealth ?? 0) + "]");
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool DoWeWantToCreateNewJobs_BeforeLoadingBlueprint
        {
            get
            {
                try
                {
                    //if it has been move than 1 min since LastIndustryJobsDeliverAll check to see if we want to create new jobs
                    if (DateTime.UtcNow > Time.Instance.LastIndustryJobsDeliverAll.AddMinutes(1))
                    {
                        //if 5 min have not passed since Time.Instance.LastIndustryNoJobsReadyToDeliver
                        if (Time.Instance.LastIndustryNoJobsReadyToDeliver.AddMinutes(5) > DateTime.UtcNow)
                        {
                            if (DirectEve.Interval(25000, 32000) && ESCache.Instance.ItemHangar.StackAll())
                                return false;

                            //How many seconds left until 5 min after Time.Instance.LastIndustryNoJobsReadyToDeliver
                            double secondsLeft = (double)Math.Round(Time.Instance.LastIndustryNoJobsReadyToDeliver.AddMinutes(5).Subtract(DateTime.UtcNow).TotalSeconds, 0);
                            if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000)) Log.WriteLine("DeWeWantToCreateNewJobs: LastIndustryNoJobsReadyToDeliver [" + secondsLeft + "] waiting");
                            return false;
                        }
                    }

                    if (ESCache.Instance.InStation)
                    {
                        //
                        // Do we want to be able to pull items from other hangars?
                        //
                        if (ESCache.Instance.ItemHangar == null)
                            return false;

                        if (!ESCache.Instance.ItemHangar.Items.Any())
                        {
                            if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000)) Log.WriteLine("DeWeWantToCreateNewJobs: false");
                            return false;
                        }

                        //
                        // Add checking for items here. We will need some XML settings similar to ammo: individual items and number of those items needed at minimum
                        // If we have enough items created already we probably want to move them to a market system or put them up on the market
                        //

                        if (!ESCache.Instance.OpenIndustryWindow()) return false;

                        if (IndustryWindow.used_slots > 0 && IndustryWindow.max_slots == IndustryWindow.used_slots)
                        {
                            if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000)) Log.WriteLine("DeWeWantToCreateNewJobs: max_slots [" + IndustryWindow.max_slots + "] used_slots [" + IndustryWindow.used_slots + "] false");
                            Time.Instance.LastIndustryNoJobsReadyToDeliver = DateTime.UtcNow;
                            ChangeIndustryBehaviorState(IndustryBehaviorState.JobsDeliverAll);
                            return false;
                        }

                        if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000)) Log.WriteLine("DeWeWantToCreateNewJobs: true");
                        return true;
                    }

                    if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(60000, 60000)) Log.WriteLine("DeWeWantToCreateNewJobs: false!!!");
                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        //
        // Industry Slots Total
        // Industry Slots Open
        //
        public static bool PrepareState()
        {
            try
            {
                //
                // Do we have enough materials? If true submit some industry jobs if false then gather materials!
                //

                if (DoWeHaveTheInputsWeNeed)
                {
                    ChangeIndustryBehaviorState(IndustryBehaviorState.CreateNewJobs);
                    return true;
                }

                ChangeIndustryBehaviorState(IndustryBehaviorState.BuyMissingInputs);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static DirectIndustryWindow IndustryWindow
        {
            get
            {
                if (ESCache.Instance.Windows.OfType<DirectIndustryWindow>().Any())
                {
                    return ESCache.Instance.Windows.OfType<DirectIndustryWindow>().FirstOrDefault();
                }

                return null;
            }
        }

        public static bool DoesIndustryWindowHaveTheRightBlueprintLoaded(DirectItem blueprint, XElement xmlIndividualIndustryOutput)
        {
            try
            {
                if (DirectEve.Interval(10000)) Log.WriteLine("from blueprintTypeId [" + blueprint.TypeId + "]");

                if (Time.Instance.LastIndustryJobStart.AddSeconds(6) > DateTime.UtcNow)
                {
                    Log.WriteLine("LastIndustryJobStart.AddSeconds(10) > DateTime.UtcNow");
                    return false;
                }

                if (!ESCache.Instance.OpenIndustryWindow())
                {
                    if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(10000)) Log.WriteLine("OpenIndustryWindow returned false");
                    return false;
                }

                //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(10000)) Log.WriteLine("OpenIndustryWindow true");
                if (IndustryWindow != null)
                {
                    if (IndustryWindow.BlueprintTypeID != null)
                    {
                        if (IndustryWindow.BlueprintItemID != null)
                        {
                            if (IndustryWindow.BlueprintTypeID == blueprint.TypeId && IndustryWindow.BlueprintItemID == blueprint.ItemId)
                            {
                                if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(15000, 15000, blueprint.ItemId.ToString())) Log.WriteLine("if (industryWindow.BlueprintTypeID == blueprint.TypeId && industryWindow.BlueprintItemID == blueprint.ItemId) return true");
                                return true;
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(30000)) Log.WriteLine("IndustryWindow.BlueprintItemID == null: no blueprint loaded yet");
                        }
                    }
                    else
                    {
                        if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(30000)) Log.WriteLine("IndustryWindow.BlueprintTypeID == null: no blueprint loaded yet");
                    }

                    if (DirectEve.Interval(3000))
                    {
                        if (DirectEve.Interval(10000)) Log.WriteLine("from blueprintTypeId [" + blueprint.TypeId + "] - UseBlueprint");
                        blueprint.UseBlueprint();
                        return false;
                    }

                    if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(10000)) Log.WriteLine("Blueprint needs to be loaded: waiting on IndustryWindow");
                    return false;
                }

                if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryWindow not found");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static bool ManufactureItem(DirectItem blueprint, XElement xmlIndividualIndustryOutput)
        {
            try
            {
                if (blueprint.CategoryId != (int)CategoryID.Blueprint)
                {
                    Log.WriteLine("ManufactureItem: blueprint.CategoryId [" + blueprint.CategoryId + "][" + blueprint.CategoryName + "] is not a blueprint. ");
                    return false;
                }

                if (ESCache.Instance.ItemHangar.Items.All(i => i.ItemId != blueprint.ItemId))
                {
                    Log.WriteLine("ManufactureItem: Blueprint is no longer in the Hangar: assuming item is being manufactured");
                    return true;
                }

                if (!ESCache.Instance.OpenIndustryWindow()) return false;

                if (!DoesIndustryWindowHaveTheRightBlueprintLoaded(blueprint, xmlIndividualIndustryOutput))
                    return false;

                if (!DoWeWantToCreateNewJob_AfterLoadingBluePrint(blueprint, xmlIndividualIndustryOutput)) return false;

                if (IndustryWindow.Buttons.Any(i => i.Type == IndustryWindowButtonType.START))
                {
                    //press start button
                    if (DirectEve.Interval(7000))
                    {
                        if (IndustryWindow.Buttons.FirstOrDefault(i => i.Type == IndustryWindowButtonType.START).Click())
                        {
                            Time.Instance.LastIndustryJobStart = DateTime.UtcNow;
                            Log.WriteLine("Start Button pressed!");
                            return true;
                        }

                        Log.WriteLine("Start button press failed!");
                        return false;
                    }

                    Log.WriteLine("Start pressed: waiting");
                    return false;
                }

                Log.WriteLine("No Start button found");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static bool CreateNewJobsState()
        {
            try
            {
                if (!DoWeWantToCreateNewJobs_BeforeLoadingBlueprint)
                {
                    //
                    // if we have made "enough" items we might want to move the items to a market system or put them up on the market
                    //
                    //ChangeIndustryBehaviorState(IndustryBehaviorState.JobsDeliverAll); dont change states here, there are some reasons DoWeWantToCreateNewJobs_BeforeLoadingBlueprint would return false that we should just wait longer...
                    if (DirectEve.Interval(30000)) Log.WriteLine("DoWeWantToCreateNewJobs_BeforeLoadingBlueprint returned false CurrentIndustryBehaviorState [" + State.CurrentIndustryBehaviorState + "]");
                    return false;
                }

                //
                // Create new industry jobs
                //
                if (ESCache.Instance.InStation)
                {
                    //
                    // Do we want to be able to pull items from other hangars?
                    //
                    if (ESCache.Instance.ItemHangar == null)
                        return false;

                    if (!ESCache.Instance.ItemHangar.Items.Any())
                        return false;

                    if (ESCache.Instance.ItemHangar.Items.Any(i => i.IsBlueprintCopy || i.IsBlueprintOriginal))
                    {
                        //
                        // Add checking for items here. We will need some XML settings similar to ammo: individual items and number of those items needed at minimum
                        // If we have enough items created already we probably want to move them to a market system or put them up on the market
                        //
                        List<DirectItem> blueprints = ESCache.Instance.ItemHangar.Items.Where(i => i.IsBlueprintCopy || i.IsBlueprintOriginal).ToList();
                        if (blueprints != null && blueprints.Any())
                        {
                            int NumOfBlueprintsInHangar = blueprints.Count();
                            if (DirectEve.Interval(60000, 60000, NumOfBlueprintsInHangar.ToString())) Log.WriteLine("Blueprints [" + NumOfBlueprintsInHangar + "]");
                            if (xmlIndustryOutputs != null)
                            {
                                try
                                {
                                    int NumDiffProductsToMake = xmlIndustryOutputs.Elements("industryOutput").Count();
                                    if (DirectEve.Interval(60000, 60000, NumDiffProductsToMake.ToString())) Log.WriteLine("industryOutput Products to Make [" + NumDiffProductsToMake + "]");
                                    foreach (XElement xmlIndividualIndustryOutput in xmlIndustryOutputs.Elements("industryOutput"))
                                    {
                                        DirectItem IndividualIndustryOutput = new DirectItem(ESCache.Instance.DirectEve);
                                        IndividualIndustryOutput.TypeId = (int)xmlIndividualIndustryOutput.Attribute("typeId");
                                        int blueprintTypeId = 0;
                                        try
                                        {
                                            blueprintTypeId = (int)xmlIndividualIndustryOutput.Attribute("blueprintTypeId");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteLine("Exception [" + ex + "]");
                                        }

                                        if (blueprintTypeId != 0)
                                        {
                                            if (blueprints.All(i => i.TypeId != blueprintTypeId))
                                            {
                                                if (DirectEve.Interval(60000, 60000, IndividualIndustryOutput.TypeName)) Log.WriteLine("[" + IndividualIndustryOutput.TypeName + "] blueprintTypeId [" + blueprintTypeId + "] - blueprint missing");
                                                continue;
                                            }

                                            if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == blueprintTypeId))
                                            {
                                                long TotalInItemHangarQuantity = ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == IndividualIndustryOutput.TypeId).Sum(e => e.Stacksize);
                                                long needToMakeQuantity = (long)xmlIndividualIndustryOutput.Attribute("quantity") - TotalInItemHangarQuantity;

                                                //
                                                // this doesnt take into account how many are currently baking! We should fix this so that it does!
                                                //
                                                if (needToMakeQuantity > 0)
                                                {
                                                    if (DirectEve.Interval(15000, 20000, IndividualIndustryOutput.TypeName)) Log.WriteLine("DoWeHaveTheOutputsWeNeed: [" + IndividualIndustryOutput.TypeName + "] Have [" + TotalInItemHangarQuantity + "] Need [" + needToMakeQuantity + "] more units");
                                                    foreach (var blueprintToUse in ESCache.Instance.ItemHangar.Items.Where(x => x.IsBlueprintCopy || x.IsBlueprintOriginal).Where(i => i.TypeId == blueprintTypeId).OrderByDescending(x => x.IsBlueprintCopy))
                                                    {
                                                        if (IndustryWindow != null)
                                                        {
                                                            if (IndustryWindow.JobStatus == 101) //job is done
                                                            {
                                                                IndustryWindow.Close();
                                                                return false;
                                                            }

                                                            if (IndustryWindow.jobID != 0 & IndustryWindow.JobStatus != 0 && IndustryWindow.BlueprintItemID == blueprintToUse.ItemId)
                                                            {
                                                                if (DirectEve.Interval(5000, 5000, blueprintToUse.ItemId.ToString())) Log.WriteLine("DoWeHaveTheOutputsWeNeed: [" + IndividualIndustryOutput.TypeName + "] this blueprint is already in use: next?");
                                                                continue;
                                                            }
                                                        }

                                                        if (!ManufactureItem(blueprintToUse, xmlIndividualIndustryOutput)) return false;
                                                        break;
                                                    }

                                                    return false;
                                                }

                                                if (DirectEve.Interval(60000, 60000, IndividualIndustryOutput.TypeName)) Log.WriteLine("DoWeHaveTheOutputsWeNeed: [" + IndividualIndustryOutput.TypeName + "] Have [" + TotalInItemHangarQuantity + "] Need [" + needToMakeQuantity + "] more units: Done: we have reached the production goal");
                                                continue;
                                            }

                                            continue;
                                        }

                                        if (DirectEve.Interval(60000, 60000, IndividualIndustryOutput.TypeName)) Log.WriteLine("No blueprintTypeId for [" + IndividualIndustryOutput.TypeName + "]");
                                        continue;
                                    }

                                    if (DirectEve.Interval(60000, 60000)) Log.WriteLine("No more blueprints to use");
                                    ChangeIndustryBehaviorState(IndustryBehaviorState.JobsDeliverAll);
                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                    return false;
                                }
                            }
                            else
                            {
                                if (DirectEve.Interval(5000)) Log.WriteLine("xmlIndustryOutputs is null");
                                return false;
                            }
                        }

                        if (DirectEve.Interval(5000)) Log.WriteLine("No blueprints found");
                        return true;
                    }

                    return false;
                }

                if (DirectEve.Interval(15000)) Log.WriteLine("CreateNewJobs: Not yet implemented");
                ChangeIndustryBehaviorState(IndustryBehaviorState.JobsDeliverAll);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static bool MoveItemsToMarketState()
        {
            //
            // Move items to a market system or put them up on the market
            //
            if (DirectEve.Interval(15000)) Log.WriteLine("MoveItemsToMarket: Not yet implemented: this really belongs in a different behavior!");
            //ChangeIndustryBehaviorState(IndustryBehaviorState.CreateSellOrders);
            return true;
        }

        public static bool CreateSellOrdersState()
        {
            //
            // Move items to a market system or put them up on the market
            //
            if (DirectEve.Interval(15000)) Log.WriteLine("CreateSellOrdersState: Not yet implemented: this really belongs in a different behavior!");
            ChangeIndustryBehaviorState(IndustryBehaviorState.Idle);
            return true;
        }

        public static bool DeliverState()
        {
            //
            // Deliver the industry jobs we have completed
            //
            if (DirectEve.Interval(15000)) Log.WriteLine("Jobs_Deliver_All");
            Jobs_Deliver_All();
            return true;
        }

        public static bool GatherInputsState()
        {
            //
            // Gather Inputs (Materials) needed for the industry jobs we want to submit
            // Might need to go to Jita (Market system) to buy some stuff
            //
            if (DirectEve.Interval(15000)) Log.WriteLine("GatherInputs: Not yet implemented");

            //
            // go to jita?
            //

            foreach (XElement xmlIndividualIndustryInput in xmlIndustryInputs.Elements("industryInput"))
            {
                try
                {
                    DirectItem individualIndustryInput = new DirectItem(ESCache.Instance.DirectEve);
                    individualIndustryInput.TypeId = (int)xmlIndividualIndustryInput.Attribute("typeId");
                    if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == individualIndustryInput.TypeId))
                    {
                        long TotalInItemHangarQuantity = ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == individualIndustryInput.TypeId).Sum(e => e.Stacksize);
                        long needToFindQuantity = (long)xmlIndividualIndustryInput.Attribute("quantity") - TotalInItemHangarQuantity;
                        Log.WriteLine("DoWeHaveTheInputsWeNeed: [" + individualIndustryInput.TypeName + "] Have [" + TotalInItemHangarQuantity + "] Need [" + needToFindQuantity + "] more units");
                        continue;
                    }

                    Log.WriteLine("DoWeHaveTheInputsWeNeed: [" + individualIndustryInput.TypeName + "] Have [0] Need [" + xmlIndividualIndustryInput.Attribute("quantity") + "] more units");
                    continue;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }

            ChangeIndustryBehaviorState(IndustryBehaviorState.CreateNewJobs);
            return true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(30000, 30000, State.CurrentIndustryBehaviorState.ToString())) Log.WriteLine("State.CurrentIndustryBehaviorState is [" + State.CurrentIndustryBehaviorState + "]");

                switch (State.CurrentIndustryBehaviorState)
                {
                    case IndustryBehaviorState.Idle:
                        IdleState();
                        break;

                    case IndustryBehaviorState.Start:
                        StartState();
                        break;

                    case IndustryBehaviorState.Switch:
                        SwitchState();
                        break;

                    case IndustryBehaviorState.Prepare:
                        PrepareState();
                        break;

                    case IndustryBehaviorState.BuyMissingInputs:
                        GatherInputsState();
                        break;

                    case IndustryBehaviorState.CreateNewJobs:
                        CreateNewJobsState();
                        break;

                    case IndustryBehaviorState.JobsDeliverAll:
                        DeliverState();
                        break;

                    case IndustryBehaviorState.LocalWatch:
                        LocalWatchState();
                        break;

                    case IndustryBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case IndustryBehaviorState.WarpOutStation:
                        WarpOutBookmarkState();
                        break;

                    case IndustryBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case IndustryBehaviorState.UnloadLoot:
                        UnloadLootState();
                        break;

                    case IndustryBehaviorState.Traveler:
                        TravelerState();
                        break;

                    case IndustryBehaviorState.Default:
                        ChangeIndustryBehaviorState(IndustryBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool EveryPulse()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryBehavior: EveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                    return false;
                }

                if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(12) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.InWormHoleSpace)
                {
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryBehavior: EveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                    return true;
                }

                if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryBehavior: EveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                    return true;
                }

                Panic.ProcessState(Settings.Instance.HomeBookmarkName);

                if (State.CurrentPanicState == PanicState.Resume)
                {
                    if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                    {
                        State.CurrentPanicState = PanicState.Normal;
                        State.CurrentTravelerState = States.TravelerState.Idle;
                        ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
                        return true;
                    }

                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("IndustryBehavior: EveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "IndustryBehavior.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + Settings.Instance.HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(Settings.Instance.HomeBookmarkName);

            if (State.CurrentTravelerState == States.TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + Settings.Instance.HomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    return;
                }

                Traveler.Destination = null;
                ChangeIndustryBehaviorState(IndustryBehaviorState.Start, true);
            }
        }

        private static void IdleState()
        {
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentUnloadLootState = States.UnloadLootState.Idle;

            //add a delay here? directeve.interval should be fine
            ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
        }

        public static void InvalidateCache()
        {
            // Method intentionally left empty.
        }

        public static bool Tabs_Jobs_Select()
        {
            //Switch to Jobs tab (bottom of the industry window) so we can see and possibly use the deliver jobs button
            return false;
        }

        public static bool Jobs_Deliver_All()
        {
            try
            {
                //if (!DebugConfig.ClaimLoginRewards) return false;
                if (ESCache.Instance.Paused) return true;

                if (!ESCache.Instance.InStation)
                {
                    Log.WriteLine("We are not in station?!");
                    return false;
                }

                if (ESCache.Instance.InStation)
                {
                    if (!ESCache.Instance.OpenIndustryWindow()) return false;
                    //if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("OpenIndustryWindow true");
                    //if (!Tabs_Jobs_Select())
                    //{
                    //    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("Tabs_Jobs_Select returned false");
                    //    return false;
                    //}

                    if (IndustryWindow != null)
                    {
                        if (IndustryWindow.Buttons.Any() && IndustryWindow.Buttons.Any(i => i.Type == IndustryWindowButtonType.JOBS_DELIVER_ALL))
                        {
                            if (DirectEve.Interval(5000, 7000))
                            {
                                Log.WriteLine("Press JOBS_DELIVER_ALL Button");
                                if (IndustryWindow.Buttons.FirstOrDefault(i => i.Type == IndustryWindowButtonType.JOBS_DELIVER_ALL).Click())
                                {
                                    Time.Instance.LastIndustryNoJobsReadyToDeliver = DateTime.UtcNow;
                                    Time.Instance.LastIndustryJobsDeliverAll = DateTime.UtcNow;
                                    //Time.Instance.LastLoginRewardClaim = DateTime.UtcNow;
                                    //Time.Instance.LastRewardRedeem = DateTime.UtcNow.AddDays(-2);
                                    return true;
                                }

                                Log.WriteLine("Press JOBS_DELIVER_ALL Button - returned false!");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Interval(6000, 6000, "Jobs_Deliver_All", true))
                        {
                            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("No JOBS_DELIVER_ALL button found");
                            ChangeIndustryBehaviorState(IndustryBehaviorState.CreateNewJobs);
                            return false;
                        }

                        return false;
                    }

                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("No industryWindow found");
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

        private static void LocalWatchState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeIndustryBehaviorState(IndustryBehaviorState.WarpOutStation);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeIndustryBehaviorState(IndustryBehaviorState.WaitingforBadGuytoGoAway);
                return;
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist)
            {
                Log.WriteLine("LocalWatchState: We have pvp timer: waiting");
                return;
            }

            ChangeIndustryBehaviorState(IndustryBehaviorState.WarpOutStation);
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("IndustryBehavior.ResetStatesToDefaults: start");
            State.CurrentIndustryBehaviorState = IndustryBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = States.UnloadLootState.Idle;
            State.CurrentTravelerState = States.TravelerState.AtDestination;
            Log.WriteLine("IndustryBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void StartState()
        {
            if (!DebugConfig.DebugIndustryBehavior)
            {
                if (Time.Instance.IsItDuringDowntimeNow)
                {
                    Log.WriteLine("IndustryController: Start: Downtime is less than 25 minutes from now: Pausing");
                    ControllerManager.Instance.SetPause(true);
                    return;
                }
            }

            ChangeIndustryBehaviorState(IndustryBehaviorState.Switch);
        }

        private static void SwitchState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.ChangeArmState(ArmState.ActivateTransportShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeIndustryBehaviorState(IndustryBehaviorState.UnloadLoot);
            }
        }

        private static void TravelerState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == States.TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeIndustryBehaviorState(IndustryBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeIndustryBehaviorState(IndustryBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeIndustryBehaviorState(IndustryBehaviorState.Idle, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void UnloadLootState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (State.CurrentUnloadLootState == States.UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    State.CurrentUnloadLootState = States.UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == States.UnloadLootState.Done)
                {
                    State.CurrentUnloadLootState = States.UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");

                    ChangeIndustryBehaviorState(IndustryBehaviorState.JobsDeliverAll);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void WaitingFoBadGuyToGoAway()
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + ESCache.Instance.RandomNumber(1, 3))
                return;

            ChangeIndustryBehaviorState(IndustryBehaviorState.LocalWatch);
        }

        private static void WarpOutBookmarkState()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark = warpOutBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        State.CurrentTravelerState = States.TravelerState.Idle;
                        ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == States.TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = States.TravelerState.Idle;
                            ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        State.CurrentTravelerState = States.TravelerState.Idle;
                        ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeIndustryBehaviorState(IndustryBehaviorState.GotoHomeBookmark);
        }
    }
}