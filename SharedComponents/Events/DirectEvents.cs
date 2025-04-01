/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.08.2016
 * Time: 20:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace SharedComponents.Events
{
    /// <summary>
    ///     Description of DirectEvents.
    /// </summary>
    public enum DirectEvents
    {
        PRIVATE_CHAT_RECEIVED = -1,
        DOCK_JUMP_ACTIVATE = -2,
        LOCK_TARGET = -3,
        WARP = -4,
        UNDOCK = -5,
        ACCEPT_MISSION = -6,
        DECLINE_MISSION = -7,
        COMPLETE_MISSION = -8,
        PANIC = -9,
        CAPSULE = -10,
        MISSION_INVADED = -11,
        LOCKED_BY_PLAYER = -12,
        CALLED_LOCALCHAT = -13, // events with negative value will be ignored
        ERROR = -14,
        NOTICE = -15,
        KEEP_ALIVE = 45, // client will exit if KEEP_ALIVE event hasn't been received within the last 45 minutes
        
    }
}