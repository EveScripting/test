/*
 * ---------------------------------------
 * User: duketwo
 * Date: 19.06.2016
 * Time: 17:03
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedComponents.WinApiUtil
{
    public static class Hooks
    {
        private static List<Hook> HhooksList;

        static Hooks()
        {
            HhooksList = new List<Hook>();
        }

        public static List<Hook> GetHooks => HhooksList.ToList();

        public static Hook CreateHook(HookType type, int threadId)
        {
            if (HhooksList.Any(h => h.Type == type))
                return HhooksList.FirstOrDefault(h => h.Type == type);

            var hook = new Hook(type, threadId);
            if (hook.Hhook == 0)
                throw new Exception("SetWindowsHookEx failed.");

            HhooksList.Add(hook);
            return hook;
        }

        public static void DisposeHook(HookType type)
        {
            if (!HhooksList.Any(h => h.Type == type))
                throw new Exception("Dispose failed.");

            if (!Pinvokes.UnhookWindowsHookEx(HhooksList.FirstOrDefault(h => h.Type == type).Hhook))
                throw new Exception("UnhookWindowsHookEx Failed");

            HhooksList.RemoveAll(h => h.Type == type);
        }

        public static void DisposeHook(Hook hook)
        {
            if (!HhooksList.Any(h => h == hook))
                throw new Exception("Dispose failed.");

            if (!Pinvokes.UnhookWindowsHookEx(HhooksList.FirstOrDefault(h => h == hook).Hhook))
                throw new Exception("UnhookWindowsHookEx Failed.");

            HhooksList.RemoveAll(h => h == hook);
        }


        public static void DisposeHooks()
        {
            foreach (var h in HhooksList)
                DisposeHook(h);
        }

        public class Hook
        {
            public delegate void HookProcEvent(int nCode, IntPtr wParam, IntPtr lParam);

            public int Hhook;
            public Pinvokes.HookProc HookProcedure;
            public int ThreadId;
            public HookType Type;

            public Hook(HookType type, int threadId)
            {
                ThreadId = threadId;
                HookProcedure = new Pinvokes.HookProc(HookProc);
                Hhook = Pinvokes.SetWindowsHookEx((int) type, HookProcedure, IntPtr.Zero, ThreadId);
                Type = type;
            }

            public event HookProcEvent OnHookProcEvent;

            public int HookProc(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (OnHookProcEvent != null)
                    OnHookProcEvent(nCode, wParam, lParam);


                return Pinvokes.CallNextHookEx(Hhook, nCode, wParam, lParam);
            }
        }
    }
}