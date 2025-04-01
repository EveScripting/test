using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedComponents.Extensions;

namespace SharedComponents.EVE
{
    [Serializable]
    public class GPUDetail
    {
        public GPUDetail(string cardName, string deviceId, string vendorId, string revisionId, string driverVerison,
            string directXVersion, string manufacturer, string deviceIdentifier, string driverDate, string dedicatedMemoryMB)
        {
            this.CardName = cardName;
            this.DeviceId = deviceId;
            this.VendorId = vendorId;
            this.RevisionId = revisionId;
            this.DriverVersion = driverVerison;
            this.DirectXVersion = directXVersion;
            this.Manufacturer = manufacturer;
            this.DeviceIdentifier = deviceIdentifier;
            this.DriverDate = driverDate;
            this.DedicatedMemoryMB = dedicatedMemoryMB;
        }

        public GPUDetail()
        {

        }

        public bool AnyMemberEmpty()
        {
            return string.IsNullOrEmpty(CardName) ||
                string.IsNullOrEmpty(DeviceId) ||
                string.IsNullOrEmpty(VendorId) ||
                string.IsNullOrEmpty(RevisionId) ||
                string.IsNullOrEmpty(DriverVersion) ||
                string.IsNullOrEmpty(DirectXVersion) ||
                string.IsNullOrEmpty(Manufacturer) ||
                string.IsNullOrEmpty(DeviceIdentifier) ||
                string.IsNullOrEmpty(DedicatedMemoryMB) ||
                string.IsNullOrEmpty(DriverDate);
        }

        public string CardName { get; set; }
        public string DeviceId { get; set; }
        public string VendorId { get; set; }
        public string RevisionId { get; set; }
        public string DriverVersion { get; set; }
        public string DirectXVersion { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceIdentifier { get; set; }
        public string DriverDate { get; set; }

        public string DedicatedMemoryMB { get; set; }

        public DateTime? DriverDateTime
        {
            get
            {
                if (DriverDate.Contains("/"))
                {
                    var usCulture = "en-US";
                    if (DateTime.TryParse(DriverDate, new CultureInfo(usCulture, false), DateTimeStyles.AdjustToUniversal, out var dtx))
                    {
                        return dtx;
                    }

                    if (DateTime.TryParse(DriverDate, out dtx))
                    {
                        return dtx;
                    }

                    return null;
                }

                if (DateTime.TryParse(DriverDate, out var dt))
                    return dt;
                return DateTime.Today;
            }
        }

  

        public static void ParseGPUDetailsFromDxDiagFolder(string path)
        {
          

            var list = new List<GPUDetail>();
            foreach (string file in Directory.EnumerateFiles(path
                , "*.*", SearchOption.AllDirectories))
            {
                var ret = GPUDetail.ParseDXDiagFile(File.ReadAllText(file));
                if (ret != null)
                {
                    list.Add(ret);
                }
            }

            list = list.Distinct().ToList();
            list.RemoveAll(k => k == null || k.CardName.Contains("?") || k.CardName.ToLower().Contains("standard")
            || k.AnyMemberEmpty()
            || k.DriverDateTime.Value.Year < 2017
            || !(k.DirectXVersion.Contains("11") || k.DirectXVersion.Contains("12")));
            list = list.OrderBy(k => k.CardName).ToList();

            foreach (var k in list)
            {
                k.Print();
            }
        }


        public void Print()
        {
            Debug.WriteLine($"new GPUDetail(\"{CardName}\", \"{DeviceId}\", \"{VendorId}\", \"{RevisionId}\", \"{DriverVersion}\", \"{DirectXVersion}\", \"{Manufacturer}\", \"{DeviceIdentifier}\", \"{DriverDate}\", \"{DedicatedMemoryMB}\"),");
        }

        public static GPUDetail ParseDXDiagFile(string cont)
        {
            try
            {
                if (cont.Contains("Display Devices"))
                {
                    GPUDetail gpuDetail = new GPUDetail();

                    var strD = @"Display Devices";
                    var strA = @"Sound Devices";

                    var dispCont = cont.Substring(strD, strA);

                    var match = Regex.Match(dispCont, @"(?<=Card name: ).*");
                    if (match.Success)
                        gpuDetail.CardName = match.Value.Trim();

                    match = Regex.Match(dispCont, @"(?<=Driver Version: ).*");
                    if (match.Success)
                    {
                        gpuDetail.DriverVersion = match.Value.Trim();
                        gpuDetail.DriverVersion = gpuDetail.DriverVersion.Replace(" (English)", "");
                    }

                    match = Regex.Match(dispCont, @"(?<=Vendor ID: ).*");
                    if (match.Success)
                        gpuDetail.VendorId = Convert.ToInt32(match.Value.Trim(), 16).ToString();

                    match = Regex.Match(dispCont, @"(?<=Device ID: ).*");
                    if (match.Success)
                        gpuDetail.DeviceId = Convert.ToInt32(match.Value.Trim(), 16).ToString();

                    match = Regex.Match(dispCont, @"(?<=Revision ID: ).*");
                    if (match.Success)
                        gpuDetail.RevisionId = Convert.ToInt32(match.Value.Trim(), 16).ToString();


                    match = Regex.Match(dispCont, @"(?<=Device Identifier: ).*");
                    if (match.Success)
                        gpuDetail.DeviceIdentifier = match.Value.Trim().Replace("{", String.Empty).Replace("}", String.Empty).ToLower();

                    match = Regex.Match(dispCont, @"(?<=Dedicated Memory: ).*");
                    if (match.Success)
                        gpuDetail.DedicatedMemoryMB = match.Value.ToLower().Replace("mb", String.Empty).Trim();

                    match = Regex.Match(cont, @"(?<=DirectX Version: ).*");
                    if (match.Success)
                    {
                        gpuDetail.DirectXVersion = match.Value.Trim().Replace("{", String.Empty).Replace("}", String.Empty).ToLower();
                        if (gpuDetail.DirectXVersion.Contains("("))
                        {
                            gpuDetail.DirectXVersion = gpuDetail.DirectXVersion.Substring(0, gpuDetail.DirectXVersion.IndexOf("(")).Trim();
                        }
                        gpuDetail.DirectXVersion = gpuDetail.DirectXVersion.ToLower().Replace("directx", "").Replace(" ", "");
                    }

                    var matches = Regex.Matches(dispCont, @"(?<=Manufacturer: ).*").Cast<Match>();
                    if (matches.Any())
                    {
                        gpuDetail.Manufacturer = matches.Last().Value.Trim();
                    }

                    match = Regex.Match(dispCont, @"(?<=Driver Date/Size: ).*");
                    if (match.Success)
                    {
                        gpuDetail.DriverDate = match.Value.Trim().Split(',')[0];
                        if (gpuDetail.DriverDateTime == null)
                        {
                            Cache.Instance.Log("DriverDateTime is null");
                            return null;
                        }
                    }

                    return gpuDetail;
                }
                Cache.Instance.Log("Unable to find \"Display Devices\" in clipboard content");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ParseDXDiagFile Exception: " + ex);
                Cache.Instance.Log($"Exception was thrown trying to parse the content of your clipboard: {ex.StackTrace}");
                return null;
            }
        }
    }
}
