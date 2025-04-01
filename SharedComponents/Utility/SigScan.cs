using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SharedComponents.Utility;

namespace Utility
{

    // sigScan is based on the FindPattern code written by
    // dom1n1k and Patrick at GameDeception.net
    //
    // Full credit to them for the purpose of this code. I, atom0s, simply
    // take credit for converting it to C#.
    //
    // [ USAGE ] ------------------------------------------------------------------------------
    //
    // Examples:
    //
    //      SigScan _sigScan = new SigScan();
    //      _sigScan.Process = someProc;
    //      _sigScan.Address = new IntPtr(0x123456);
    //      _sigScan.Size = 0x1000;
    //      IntPtr pAddr = _sigScan.FindPattern(new byte[]{ 0xFF, 0xFF, 0xFF, 0xFF, 0x51, 0x55, 0xFC, 0x11 }, "xxxx?xx?", 12);
    //
    //      SigScan _sigScan = new SigScan(someProc, new IntPtr(0x123456), 0x1000);
    //      IntPtr pAddr = _sigScan.FindPattern(new byte[]{ 0xFF, 0xFF, 0xFF, 0xFF, 0x51, 0x55, 0xFC, 0x11 }, "xxxx?xx?", 12);
    //
    // ----------------------------------------------------------------------------------------
    public class SigScan
    {

        public static IntPtr FindDxgiDesc1Offset(string mod = "dxgi.dll")
        {
            var module = mod;
            var size = 0;
            var currentProc = Process.GetCurrentProcess();
            var baseAddr = IntPtr.Zero;

            ProcessModuleCollection myProcessModuleCollection = currentProc.Modules;

            for (int i = 0; i < myProcessModuleCollection.Count; i++)
            {
                var myProcessModule = myProcessModuleCollection[i];
                if (myProcessModule.ModuleName.ToLower().Equals(module))
                {
                    size = myProcessModule.ModuleMemorySize;
                    baseAddr = myProcessModule.BaseAddress;
                }
            }

            Util.GlobalRemoteLog($"baseAddr [{baseAddr}] size [{size}] ");

            SigScan _sigScan = new SigScan
            {
                Process = currentProc,
                Address = baseAddr,
                Size = size
            };
            //IntPtr pAddr = _sigScan.FindPattern(new byte[] { 0x53, 0x0, 0x6F, 0x0, 0x66, 0x0, 0x74, 0x0, 0x77, 0x0, 0x61, 0x0, 0x72, 0x0, 0x65, 0x0, 0x20, 0x0, 0x41, 0x0, 0x64, 0x0, 0x61, 0x0, 0x70, 0x0, 0x74, 0x0, 0x65, 0x0, 0x72, 0x0 }, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 0x0);
            IntPtr pAddr = _sigScan.FindPattern(new byte[] { 0x45, 0x33, 0xC9, 0x48, 0x85, 0xD2, 0x0F, 0x84, 0xDE, 0xBD, 0x01, 0x00, 0x8B, 0x81, 0xC4, 0x02, 0x00, 0x00, 0x83, 0xF8, 0xFF, 0x0F, 0x84, 0xC3, 0xBD, 0x01, 0x00, 0x4C, 0x69, 0xC0, 0xC8, 0x01 }, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 0x0);
            //IntPtr pAddr = _sigScan.FindPattern(new byte[] { 0x53, 0x0, 0x6F, 0x0, 0x66, 0x0, 0x74, 0x0, 0x77, 0x0, 0x61, 0x0, 0x72, 0x0, 0x65, 0x0 }, "xxxxxxxxxxxxxxxx", 0);

            Util.GlobalRemoteLog($"pAddr [{pAddr}]");
            return pAddr;
        }

        /// <summary>
        /// ReadProcessMemory
        /// 
        ///     API import definition for ReadProcessMemory.
        /// </summary>
        /// <param name="hProcess">Handle to the process we want to read from.</param>
        /// <param name="lpBaseAddress">The base address to start reading from.</param>
        /// <param name="lpBuffer">The return buffer to write the read data to.</param>
        /// <param name="dwSize">The size of data we wish to read.</param>
        /// <param name="lpNumberOfBytesRead">The number of bytes successfully read.</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out()] byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead
            );

        /// <summary>
        /// m_vDumpedRegion
        /// 
        ///     The memory dumped from the external process.
        /// </summary>
        private byte[] m_vDumpedRegion;

        /// <summary>
        /// m_vProcess
        /// 
        ///     The process we want to read the memory of.
        /// </summary>
        private Process m_vProcess;

        /// <summary>
        /// m_vAddress
        /// 
        ///     The starting address we want to begin reading at.
        /// </summary>
        private IntPtr m_vAddress;

        /// <summary>
        /// m_vSize
        /// 
        ///     The number of bytes we wish to read from the process.
        /// </summary>
        private Int32 m_vSize;


        #region "sigScan Class Construction"
        /// <summary>
        /// SigScan
        /// 
        ///     Main class constructor that uses no params. 
        ///     Simply initializes the class properties and 
        ///     expects the user to set them later.
        /// </summary>
        public SigScan()
        {
            this.m_vProcess = null;
            this.m_vAddress = IntPtr.Zero;
            this.m_vSize = 0;
            this.m_vDumpedRegion = null;
        }
        /// <summary>
        /// SigScan
        /// 
        ///     Overloaded class constructor that sets the class
        ///     properties during construction.
        /// </summary>
        /// <param name="proc">The process to dump the memory from.</param>
        /// <param name="addr">The started address to begin the dump.</param>
        /// <param name="size">The size of the dump.</param>


        public SigScan(Process proc, IntPtr addr, int size)
        {
            this.m_vProcess = proc;
            this.m_vAddress = addr;
            this.m_vSize = size;
        }
        #endregion

        #region "sigScan Class Private Methods"
        /// <summary>
        /// DumpMemory
        /// 
        ///     Internal memory dump function that uses the set class
        ///     properties to dump a memory region.
        /// </summary>
        /// <returns>Boolean based on RPM results and valid properties.</returns>
        private bool DumpMemory()
        {
            try
            {
                // Checks to ensure we have valid data.
                if (this.m_vProcess == null)
                    return false;
                if (this.m_vProcess.HasExited == true)
                    return false;
                if (this.m_vAddress == IntPtr.Zero)
                    return false;
                if (this.m_vSize == 0)
                    return false;

                // Create the region space to dump into.
                this.m_vDumpedRegion = new byte[this.m_vSize];

                bool bReturn = false;
                int nBytesRead = 0;

                // Dump the memory.
                bReturn = ReadProcessMemory(
                    this.m_vProcess.Handle, this.m_vAddress, this.m_vDumpedRegion, this.m_vSize, out nBytesRead
                    );

                // Validation checks.
                if (bReturn == false || nBytesRead != this.m_vSize)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// MaskCheck
        /// 
        ///     Compares the current pattern byte to the current memory dump
        ///     byte to check for a match. Uses wildcards to skip bytes that
        ///     are deemed unneeded in the compares.
        /// </summary>
        /// <param name="nOffset">Offset in the dump to start at.</param>
        /// <param name="btPattern">Pattern to scan for.</param>
        /// <param name="strMask">Mask to compare against.</param>
        /// <returns>Boolean depending on if the pattern was found.</returns>
        private bool MaskCheck(int nOffset, byte[] btPattern, string strMask)
        {
            StringBuilder sb = new StringBuilder();
            // Loop the pattern and compare to the mask and dump.
            for (int x = 0; x < btPattern.Length; x++)
            {
                sb.Append(this.m_vDumpedRegion[nOffset + x].ToString("X2"));
                // If the mask char is a wildcard, just continue.
                if (strMask[x] == '?')
                    continue;

                // If the mask char is not a wildcard, ensure a match is made in the pattern.
                if ((strMask[x] == 'x') && (btPattern[x] != this.m_vDumpedRegion[nOffset + x]))
                {
                    //if (sb.Length > 4)
                    //    Util.GlobalRemoteLog($"{sb}");
                    return false;
                }
            }

            //Util.GlobalRemoteLog($"Pattern found.");
            // The loop was successful so we found the pattern.
            return true;
        }
        #endregion

        public IntPtr FindPatternInByArray(byte[] source, byte[] btPattern, string strMask, int nOffset)
        {
            this.m_vDumpedRegion = source;
            return FindPattern(btPattern, strMask, nOffset);
        }

        #region "sigScan Class Public Methods"
        /// <summary>
        /// FindPattern
        /// 
        ///     Attempts to locate the given pattern inside the dumped memory region
        ///     compared against the given mask. If the pattern is found, the offset
        ///     is added to the located address and returned to the user.
        /// </summary>
        /// <param name="btPattern">Byte pattern to look for in the dumped region.</param>
        /// <param name="strMask">The mask string to compare against.</param>
        /// <param name="nOffset">The offset added to the result address.</param>
        /// <returns>IntPtr - zero if not found, address if found.</returns>
        public IntPtr FindPattern(byte[] btPattern, string strMask, int nOffset)
        {
            try
            {
                // Dump the memory region if we have not dumped it yet.
                if (this.m_vDumpedRegion == null || this.m_vDumpedRegion.Length == 0)
                {
                    if (!this.DumpMemory())
                    {
                        Util.GlobalRemoteLog("FindPattern::DumpMemory could not be executed.");
                        return IntPtr.Zero;
                    }
                }

                // Ensure the mask and pattern lengths match.
                if (strMask.Length != btPattern.Length)
                {
                    Util.GlobalRemoteLog("strMask.Length != btPattern.Length");
                    return IntPtr.Zero;
                }

                // Loop the region and look for the pattern.
                for (int x = 0; x < this.m_vDumpedRegion.Length; x++)
                {
                    if (this.MaskCheck(x, btPattern, strMask))
                    {
                        // The pattern was found, return it.
                        Util.GlobalRemoteLog($"Patter found. m_vAddress {m_vAddress} x {x} nOffset {nOffset} ");
                        var ret = new IntPtr(((Int64)x + (Int64)nOffset));
                        return ret;
                    }
                }

                // Pattern was not found.
                Util.GlobalRemoteLog("Pattern was not found.");
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Util.GlobalRemoteLog($"FindPattern::Exception: {ex}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// ResetRegion
        /// 
        ///     Resets the memory dump array to nothing to allow
        ///     the class to redump the memory.
        /// </summary>
        public void ResetRegion()
        {
            this.m_vDumpedRegion = null;
        }
        #endregion

        #region "sigScan Class Properties"
        public Process Process
        {
            get { return this.m_vProcess; }
            set { this.m_vProcess = value; }
        }
        public IntPtr Address
        {
            get { return this.m_vAddress; }
            set { this.m_vAddress = value; }
        }
        public Int32 Size
        {
            get { return this.m_vSize; }
            set { this.m_vSize = value; }
        }
        #endregion
    }
}

