using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharedComponents.Utility
{
    public class MemoryOptimizer
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll")]
        private static extern bool HeapCompact(IntPtr hHeap, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);
        // Create a single Random instance (shared for all calls)
        private static readonly Random _random = new Random();

        public static void OptimizeMemory()
        {
            try
            {
                IntPtr hProcess = GetCurrentProcess();

                // Generate a new random memory limit 
                long targetMemory = _random.Next(750, 850) * 1024L * 1024L; // Convert MB to Bytes

                // 1️⃣ Trigger Garbage Collection (for managed memory)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect(); // Run twice to clean up objects

                // 2️⃣ Compact the Heap (reduce fragmentation)
                HeapCompact(GetProcessHeap(), 0);

                // 3️⃣ Trim the Working Set to the New Random Target (750MB - 900MB)
                SetProcessWorkingSetSize(hProcess, (IntPtr)targetMemory, (IntPtr)targetMemory);

                // 4️⃣ Remove Unused Pages
                EmptyWorkingSet(hProcess);

                Debug.WriteLine($"Memory optimization completed. Target: {targetMemory / (1024 * 1024)}MB.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Memory optimization failed: " + ex.Message);
            }
        }
    }
}
