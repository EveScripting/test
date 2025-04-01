/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.10.2016
 * Time: 18:05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace SharedComponents.Extensions
{
    /// <summary>
    ///     Description of ProcessExtensions.
    /// </summary>
    public static class ProcessExtensions
    {
        public static IEnumerable<Process> GetChildProcesses(this Process process)
        {
            var children = new List<Process>();
            var mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));

            foreach (ManagementObject mo in mos.Get())
                children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));

            return children;
        }
    }
}