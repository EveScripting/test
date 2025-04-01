extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class ReduceGraphicLoad
    {
        #region Fields

        #endregion Fields

        #region Properties

        public static bool ZoomLevelAlreadyProcessed { get; set; } = true;
        public static bool IsPaused { get; set; }

        #endregion Properties

        #region Methods

        public static void ClearPerPocketCache()
        {
            ClearPerSystemCache();
        }

        public static void ClearPerSystemCache()
        {
            ZoomLevelAlreadyProcessed = false;
        }

        public static void ProcessState()
        {
            if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: ProcessState()");

            //if (ESCache.Instance.EveAccount.UseFleetMgr && ESCache.Instance.ActiveShip.IsAssaultShip)
            //    return;

            //if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
            //    return;

            //if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
            //    return;

            //Note: this doesnt work because SessionIsReady = false and thus no controller run...
            if (ESCache.Instance.DirectEve.Session.InJump)
            {
                if (!DirectEve.Interval(400))
                    return;

                if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: InJump: Disable3D [" + Settings.Instance.Disable3D + "] Disable3dInStation [" + Settings.Instance.Disable3dInStation + "] Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] SceneName [" + ESCache.Instance.DirectEve.SceneName + "]");

                if (ESCache.Instance.DirectEve.Rendering3D && Settings.Instance.Disable3D)
                {
                    //true(on) != false(disable3d)
                    Log.WriteLine("ReduceGraphicsLoad: Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] Disable3D is [" + Settings.Instance.Disable3D + "] - turn rendering off");
                    ESCache.Instance.DirectEve.Rendering3D = false;
                }
                else if (!ESCache.Instance.DirectEve.Rendering3D && !Settings.Instance.Disable3D)
                {
                    //true(on) != false(disable3d)
                    Log.WriteLine("ReduceGraphicsLoad: Rendering3D [" + !Settings.Instance.Disable3D + "] Disable3D is [" + Settings.Instance.Disable3D + "] - turn rendering on");
                    ESCache.Instance.DirectEve.Rendering3D = true;
                }
            }

            if (!ESCache.Instance.DirectEve.Session.IsReady)
                return;

            if (ESCache.Instance.DirectEve.Rendering3D)
            {
                if (!DirectEve.Interval(500))
                    return;
            }
            else if (!DirectEve.Interval(8000))
                return;

            if (!DirectEve.Interval(20000, 35000) && !ESCache.Instance.EveAccount.DoneLaunchingEveInstance)
            {
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.DoneLaunchingEveInstance), true);
                Log.WriteLine("ReduceGraphicsLoad: DoneLaunchingEveInstance [" + ESCache.Instance.EveAccount.DoneLaunchingEveInstance + "]");
            }

            //
            // Graphics related changes / optimizations should processed while in space and in station!
            //
            if (ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: InSpace: Disable3D [" + Settings.Instance.Disable3D + "] Disable3dInStation [" + Settings.Instance.Disable3dInStation + "] Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] SceneName [" + ESCache.Instance.DirectEve.SceneName + "]");
                //
                // if paused enabled 3d, otherwise set 3d based on the state of the Disable3D setting
                //
                if (ESCache.Instance.Paused)
                {
                    if (IsPaused)
                    {
                        if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Paused");
                        if (!ESCache.Instance.DirectEve.Rendering3D)
                        {
                            Log.WriteLine("ReduceGraphicsLoad: Paused: Enable 3D");
                            ESCache.Instance.DirectEve.Rendering3D = true;
                        }
                    }
                }
                else
                {
                    if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] Disable3D [" + Settings.Instance.Disable3D + "]");
                    if (ESCache.Instance.DirectEve.Rendering3D && Settings.Instance.Disable3D)
                    {
                        //true(on) != false(disable3d)
                        Log.WriteLine("ReduceGraphicsLoad: Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] Disable3D is [" + Settings.Instance.Disable3D + "] - turn rendering off");
                        ESCache.Instance.DirectEve.Rendering3D = false;
                    }

                    if (!ESCache.Instance.DirectEve.Rendering3D && !Settings.Instance.Disable3D)
                    {
                        //true(on) != false(disable3d)
                        Log.WriteLine("ReduceGraphicsLoad: Rendering3D [" + !Settings.Instance.Disable3D + "] Disable3D is [" + Settings.Instance.Disable3D + "] - turn rendering on");
                        ESCache.Instance.DirectEve.Rendering3D = true;
                    }
                }
            }
            else if (ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: InStation: Disable3D [" + Settings.Instance.Disable3D + "] Disable3dInStation [" + Settings.Instance.Disable3dInStation + "] Rendering3D [" + ESCache.Instance.DirectEve.Rendering3D + "] SceneName [" + ESCache.Instance.DirectEve.SceneName + "]");
                //
                // if paused enabled 3d, otherwise set 3d based on the state of the Disable3D setting
                //

                if (Settings.Instance.Disable3dInStation && ESCache.Instance.DirectEve.Rendering3D)
                {
                    //true(on) != false(disable3d)
                    Log.WriteLine("ReduceGraphicsLoad: Disable3dInStation!");
                    ESCache.Instance.DirectEve.Rendering3D = false;
                }
                else if (!Settings.Instance.Disable3dInStation && !ESCache.Instance.DirectEve.Rendering3D)
                {
                    //true(on) != false(disable3d)
                    Log.WriteLine("ReduceGraphicsLoad: Enable 3d In Station!");
                    ESCache.Instance.DirectEve.Rendering3D = true;
                }
            }

            //
            // Zoom
            //
            if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom");
            if (ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace");
                //if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: InSpace");
                if (ESCache.Instance.SelectedController == EveAccount.AvailableControllers.CombatDontMoveController.ToString())
                    return;

                if (ESCache.Instance.SelectedController == EveAccount.AvailableControllers.WspaceSiteController.ToString())
                    return;

                if (ESCache.Instance.InAbyssalDeadspace)
                    return;

                if (ESCache.Instance.DirectEve.Session.IsVoidSpace)
                    return;

                if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace2");

                if (DateTime.UtcNow > Time.Instance.LastDockAction.AddSeconds(10) &&
                    DateTime.UtcNow > Time.Instance.LastJumpAction.AddSeconds(10) &&
                    DateTime.UtcNow > Time.Instance.NextStartupAction)
                {
                    try
                    {
                        if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace3");
                        //
                        // if a station is on grid dont bother to zoom out as the camera will reset when we warp away
                        //
                        if (ESCache.Instance.ClosestDockableLocation != null && ESCache.Instance.ClosestDockableLocation.Distance < (double)Distances.OnGridWithMe)
                        {
                            if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: ClosestDockableLocation [" + ESCache.Instance.ClosestDockableLocation.Name + "] Distance [" + Math.Round(ESCache.Instance.ClosestDockableLocation.Distance / 1000, 0) + "k]");
                            return;
                        }

                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: ClosestStargate [" + ESCache.Instance.ClosestStargate.Name + "] Distance [" + Math.Round(ESCache.Instance.ClosestStargate.Distance / 1000, 0) + "k]");
                            return;
                        }

                        if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000))
                        {
                            if(DebugConfig.DebugReduceGraphicsController) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace4");
                            ESCache.Instance.DirectEve.SceneManager.LogCurrentCameraZoomLevel();
                        }

                        if (!ZoomLevelAlreadyProcessed) //only adjust zoom level if we have not already done so
                        {
                            if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000))
                            {
                                Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: if (!ZoomLevelAlreadyProcessed)");
                            }

                            if (.75 > ESCache.Instance.DirectEve.SceneManager.CurrentCameraZoomLevel)
                            {
                                if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: if (.75 > ESCache.Instance.DirectEve.SceneManager.CurrentCameraZoomLevel)");

                                if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdZoomOut))
                                {
                                    if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: CmdZoomOut");
                                    ESCache.Instance.DirectEve.SceneManager.LogCurrentCameraZoomLevel();
                                }
                            }
                            else
                            {
                                if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: CurrentCameraZoomLevel [" + ESCache.Instance.DirectEve.SceneManager.CurrentCameraZoomLevel + "] > .75");
                                ZoomLevelAlreadyProcessed = true;
                            }
                        }
                        else if (DebugConfig.DebugReduceGraphicsController && DirectEve.Interval(5000)) Log.WriteLine("ReduceGraphicsLoad: Zoom: InSpace: ZoomLevelAlreadyProcessed Set true");
                        //ESCache.Instance.DirectEve.SceneManager.EnableZoomHack();

                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }
            }
        }

        #endregion Methods
    }
}