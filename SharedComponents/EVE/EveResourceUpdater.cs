//  credits https://github.com/CryoMyst

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace SharedComponents.EVE
{

    public enum EveResourceFileType
    {
        Res = 1,
        App = 2
    }

    public class EveResourceFile
    {
        public EveResourceFileType Type { get; set; }
        public string FilePath { get; set; }
        public string ResourcePath { get; set; }
        public string Hash { get; set; }
    }

    public class ProcessState
    {
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; }
        private readonly DateTime startTime;

        public ProcessState(int totalTasks)
        {
            TotalTasks = totalTasks;
            startTime = DateTime.Now;
        }

        public void TaskCompleted()
        {
            CompletedTasks++;
        }

        public double GetProgress()
        {
            return (double)CompletedTasks / TotalTasks;
        }

        public double GetElapsedTime()
        {
            return (DateTime.Now - startTime).TotalSeconds;
        }

        public async Task MonitorProgressAsync(IProgress<string> iProgress)
        {
            while (CompletedTasks < TotalTasks)
            {
                await Task.Delay(2000); // Check every 5 seconds
                double progress = GetProgress();
                double elapsedTime = GetElapsedTime();
                double estimatedTotalTime = elapsedTime / progress;
                double estimatedTimeRemaining = estimatedTotalTime - elapsedTime;

                iProgress.Report($"Progress: {progress * 100:F2}%. Estimated time remaining: {estimatedTimeRemaining:F2} seconds.");
            }

            iProgress.Report("All tasks completed!");
        }
    }

    public static class EveResourceUpdater
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly SemaphoreSlim downloadLimitSemaphore = new SemaphoreSlim(15);

        private static readonly string[] AdditionalResourceFiles =
        {
        "resfileindex.txt",
        "resfileindex_prefetch.txt",
        "resfileindex_Windows.txt"
    };

        private static async Task<int> GetCurrentVersionAsync(string server)
        {
            string url = $"https://binaries.eveonline.com/eveclient_{server.ToUpper()}.json";
            string responseBody = await client.GetStringAsync(url);

            // Define the regular expression pattern
            string pattern = @"\d+";

            // Get the matches
            MatchCollection matches = Regex.Matches(responseBody, pattern);
            return int.Parse(matches[0].Value);
        }

        private static EveResourceFile ParseResourceDataFromLine(string line)
        {
            // Examples:
            // res:/intromovie.txt,a9/a9d1721dd5cc6d54_e6bbb2df307e5a9527159a4c971034b5,e6bbb2df307e5a9527159a4c971034b5,9719,331
            // app:/bin64/GFSDK_Aftermath_Lib.x64.dll,d5/d5f922c8571bc487_b672d9fcbed94e5a7f89df362bf2179e,b672d9fcbed94e5a7f89df362bf2179e,1524736,535943,33206
            string[] members = line.Split(',');
            EveResourceFileType type = members[0].StartsWith("res") ? EveResourceFileType.Res : EveResourceFileType.App;
            string filePath = members[0].Replace("res:/", "").Replace("app:/", "");
            string resourcePath = members[1];
            string hash = members[2];

            return new EveResourceFile
            {
                Type = type,
                FilePath = filePath,
                ResourcePath = resourcePath,
                Hash = hash
            };
        }

        private static List<EveResourceFile> ParseResourceDatasFromLines(string[] lines)
        {
            List<EveResourceFile> resourceDatas = new List<EveResourceFile>();
            foreach (string line in lines)
            {
                resourceDatas.Add(ParseResourceDataFromLine(line));
            }
            return resourceDatas;
        }

        private static bool DoesFileExistWithHash(string filePath, string hash)
        {
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                string readableHash = BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(bytes)).Replace("-", "");
                if (string.Equals(readableHash, hash, StringComparison.CurrentCultureIgnoreCase))
                {
                    // file already exists with the correct hash, skip
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> DownloadRawAsync(string url, string outputPath, string hash, IProgress<string> iProgress)
        {
            const int MaxRetries = 3;
            const int RetryDelay = 5; // in seconds

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {

                if (DoesFileExistWithHash(outputPath, hash))
                {
                    //iProgress.Report("File already exists with correct hash, skipping");
                    return true;
                }

                iProgress.Report($"Attempt {attempt + 1} for downloading {url} to {outputPath}");

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                try
                {
                    await downloadLimitSemaphore.WaitAsync();
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    byte[] content = await response.Content.ReadAsByteArrayAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        File.WriteAllBytes(outputPath, content);
                    }

                    if (DoesFileExistWithHash(outputPath, hash))
                    {
                        return true;
                    }
                    else
                    {
                        iProgress.Report($"File {outputPath} does not have the correct hash after attempt {attempt + 1}");
                        await Task.Delay(TimeSpan.FromSeconds(RetryDelay));
                    }
                }
                catch (Exception e)
                {
                    iProgress.Report($"Error encountered while downloading {url}: {e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelay));
                }
                finally
                {
                    downloadLimitSemaphore.Release();
                }
            }

            iProgress.Report($"Failed to download {url} after {MaxRetries} attempts");
            return false;
        }

        private static async Task<bool> DownloadAppResourceAsync(string server, EveResourceFile resourceData, string outputDir, ProcessState state, IProgress<string> iProgress)
        {
            string url = resourceData.Type == EveResourceFileType.App
                ? $"https://binaries.eveonline.com/{resourceData.ResourcePath}"
                : $"https://resources.eveonline.com/{resourceData.ResourcePath}";

            string outputFilePath = resourceData.Type == EveResourceFileType.App
                ? Path.Combine(outputDir, server.ToLower(), resourceData.FilePath)
                : Path.Combine(outputDir, "ResFiles", resourceData.ResourcePath);

            bool success = await DownloadRawAsync(url, outputFilePath, resourceData.Hash, iProgress);
            state.TaskCompleted();

            //if (resourceData.Type == EveResourceFileType.Res && success)
            //{
            //    string unobfuscatedOutputPath = Path.Combine(outputDir, "Deobfuscated", resourceData.FilePath);
            //    Directory.CreateDirectory(Path.GetDirectoryName(unobfuscatedOutputPath));
            //    File.Copy(outputFilePath, unobfuscatedOutputPath, true);
            //}

            return success;
        }

        public static async Task<bool> DownloadAppResourcesAsync(int currentVersion, string server, string outputDir, IProgress<string> iProgress)
        {
            iProgress.Report($"Downloading app resources for version {currentVersion}");
            string resourceFileUrl = $"https://binaries.eveonline.com/eveonline_{currentVersion}.txt";
            string responseBody = await client.GetStringAsync(resourceFileUrl);
            string[] content = responseBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            iProgress.Report($"Found {content.Length} lines in {resourceFileUrl}");

            List<EveResourceFile> resourceDatas = ParseResourceDatasFromLines(content);
            ProcessState progressState = new ProcessState(resourceDatas.Count);
            Task monitorTask = progressState.MonitorProgressAsync(iProgress);
            iProgress.Report($"Downloading {resourceDatas.Count} app resources for version {currentVersion}");
            List<Task<bool>> tasks = resourceDatas.Select(rd => DownloadAppResourceAsync(server, rd, outputDir, progressState, iProgress)).ToList();
            bool[] results = await Task.WhenAll(tasks);
            if (!results.All(r => r))
            {
                return false;
            }
            await monitorTask;

            List<EveResourceFile> resResourcesDatas = new List<EveResourceFile>();
            foreach (string additionalResourceFile in AdditionalResourceFiles)
            {
                string resourceFilePath = Path.Combine(outputDir, server.ToLower(), additionalResourceFile);
                string[] lines = File.ReadAllLines(resourceFilePath);
                List<EveResourceFile> resourceDatasFromFile = ParseResourceDatasFromLines(lines);
                resResourcesDatas.AddRange(resourceDatasFromFile);
            }

            iProgress.Report($"Downloading res resources for version {currentVersion}");
            progressState = new ProcessState(resResourcesDatas.Count);
            monitorTask = progressState.MonitorProgressAsync(iProgress);
            tasks = resResourcesDatas.Select(rd => DownloadAppResourceAsync(server, rd, outputDir, progressState, iProgress)).ToList();
            results = await Task.WhenAll(tasks);
            if (!results.All(r => r))
            {
                return false;
            }
            await monitorTask;

            return true;
        }
    }
}
