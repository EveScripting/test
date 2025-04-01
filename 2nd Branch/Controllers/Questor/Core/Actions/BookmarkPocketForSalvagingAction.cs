extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;

//using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static bool BookmarkPocketForSalvaging(DirectAgentMission myMission)
        {
            if (myMission == null) return true;

            if (DebugConfig.DebugSalvage) Log.WriteLine("Entered: BookmarkPocketForSalvaging");
            double RangeToConsiderWrecksDuringLootAll;
            List<ModuleCache> tractorBeams = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TractorBeam).ToList();
            if (tractorBeams.Count > 0)
                RangeToConsiderWrecksDuringLootAll = Math.Min(tractorBeams.Min(t => t.OptimalRange), ESCache.Instance.ActiveShip.MaxTargetRange);
            else
                RangeToConsiderWrecksDuringLootAll = 1500;

            if ((Salvage.LootEverything || Salvage.LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion) &&
                ESCache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) > Salvage.MinimumWreckCount)
            {
                if (DebugConfig.DebugSalvage)
                    Log.WriteLine("LootEverything [" + Salvage.LootEverything + "] UnLootedContainers [" + ESCache.Instance.UnlootedContainers.Count() +
                                  "LootedContainers [" + ESCache.Instance.LootedContainers.Count() + "] MinimumWreckCount [" + Salvage.MinimumWreckCount +
                                  "] We will wait until everything in range is looted.");

                if (ESCache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) > 0)
                {
                    if (DebugConfig.DebugSalvage)
                        Log.WriteLine("if (Cache.Instance.UnlootedContainers.Count [" +
                                      ESCache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) +
                                      "] (w => w.Distance <= RangeToConsiderWrecksDuringLootAll [" + RangeToConsiderWrecksDuringLootAll + "]) > 0)");
                    return false;
                }

                if (DebugConfig.DebugSalvage)
                    Log.WriteLine("LootEverything [" + Salvage.LootEverything +
                                  "] We have LootEverything set to on. We cant have any need for the pocket bookmarks... can we?!");
                return true;
            }

            if (Salvage.CreateSalvageBookmarks)
            {
                if (DebugConfig.DebugSalvage)
                    Log.WriteLine("CreateSalvageBookmarks [" + Salvage.CreateSalvageBookmarks + "]");

                if (MissionSettings.ThisMissionIsNotWorthSalvaging(myMission))
                {
                    Log.WriteLine("[" + myMission.Name + "] is a mission not worth salvaging, skipping salvage bookmark creation");
                    return true;
                }

                // Nothing to loot
                if (ESCache.Instance.UnlootedContainers.Count() < Salvage.MinimumWreckCount)
                {
                    if (DebugConfig.DebugSalvage)
                        Log.WriteLine("LootEverything [" + Salvage.LootEverything + "] UnlootedContainers [" +
                                      ESCache.Instance.UnlootedContainers.Count() +
                                      "] MinimumWreckCount [" + Salvage.MinimumWreckCount + "] We will wait until everything in range is looted.");
                    // If Settings.Instance.LootEverything is false we may leave behind a lot of unlooted containers.
                    // This scenario only happens when all wrecks are within tractor range and you have a salvager
                    // ( typically only with a Golem ).  Check to see if there are any cargo containers in space.  Cap
                    // boosters may cause an unneeded salvage trip but that is better than leaving millions in loot behind.
                    if (DateTime.UtcNow > Time.Instance.NextBookmarkPocketAttempt)
                    {
                        if (ESCache.Instance.Containers == null) return false;
                        Time.Instance.NextBookmarkPocketAttempt = DateTime.UtcNow.AddSeconds(Time.Instance.BookmarkPocketRetryDelay_seconds);
                        if (!Salvage.LootEverything && ESCache.Instance.Containers.Count() < Salvage.MinimumWreckCount)
                        {
                            Log.WriteLine("No bookmark created because the pocket has [" + ESCache.Instance.Containers.Count() +
                                          "] wrecks/containers and the minimum is [" +
                                          Salvage.MinimumWreckCount + "]");
                            return true;
                        }

                        Log.WriteLine("No bookmark created because the pocket has [" + ESCache.Instance.UnlootedContainers.Count() +
                                      "] wrecks/containers and the minimum is [" + Salvage.MinimumWreckCount + "]");
                        return true;
                    }

                    if (DebugConfig.DebugSalvage)
                        Log.WriteLine("Cache.Instance.NextBookmarkPocketAttempt is in [" +
                                      Time.Instance.NextBookmarkPocketAttempt.Subtract(DateTime.UtcNow).TotalSeconds +
                                      "sec] waiting");
                    return false;
                }

                // Do we already have a bookmark?
                List<DirectBookmark> bookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                if (bookmarks != null && bookmarks.Any())
                {
                    DirectBookmark bookmark = bookmarks.FirstOrDefault(b => ESCache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.OnGridWithMe);
                    if (bookmark != null)
                    {
                        Log.WriteLine("salvaging bookmark for this pocket is done [" + bookmark.Title + "]");
                        return true;
                    }

                    //
                    // if we have bookmarks but there is no bookmark on grid we need to continue and create the salvage bookmark.
                    //
                }

                // No, create a bookmark
                string label = string.Format("{0} {1:HHmm}", Settings.Instance.BookmarkPrefix, DateTime.UtcNow);
                //if (ESCache.Instance.CreateBookmark(label))
                //{
                //      Log.WriteLine("Bookmarking pocket for salvaging [" + label + "]");
                //      return true;
                //}
                //
                //return true;

                Log.WriteLine("CreateBookmark has been temporarily disabled: feel free to fix it");
                return true;
            }

            return true;
        }

        #endregion Methods
    }
}