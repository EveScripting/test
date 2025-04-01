using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public class WindowRecorder : IDisposable
    {
        private List<WindowRecorderSession> _sessions = new List<WindowRecorderSession>();

        // If ffmpeg is installed, we can use it to record the window.
        public static string FfmpegPath { get; } = Util.GetNextToAssemblyPath("ffmpeg.exe") ?? Util.GetInPath("ffmpeg.exe");

        public WindowRecorderSession Start(
            string fileName,
            string windowName,
            WindowRecorderOptions windowRecorderOptions)
        {
            // If ffmpeg is not installed, we can't record the window.
            if (FfmpegPath == null) return null;

            var session = new WindowRecorderSession(fileName, windowName, windowRecorderOptions);
            session.Start();
            _sessions.Add(session);
            return session;
        }

        public void Dispose()
        {
            foreach (var session in _sessions)
            {
                session.Dispose();
            }
            _sessions.Clear();
        }
    }

    public class WindowRecorderOptions
    {
        public int Framerate { get; set; } = 30;
        public RecorderEncoderSetting EncoderSetting { get; set; } = RecorderEncoderSetting.NV;
        public static readonly WindowRecorderOptions Default = new WindowRecorderOptions();
    }

    [Serializable]
    public enum RecorderEncoderSetting
    {
        CPU,
        NV,
    }

    public class WindowRecorderSession : IDisposable
    {
        private readonly string _fileName;
        private readonly string _windowName;
        private readonly WindowRecorderOptions _windowRecorderOptions;

        private bool _recording = false;
        private Process _process = null;

        internal WindowRecorderSession(
            string fileName,
            string windowName,
            WindowRecorderOptions windowRecorderOptions)
        {
            _fileName = fileName;
            _windowName = windowName;
            _windowRecorderOptions = windowRecorderOptions;
        }

        public bool Start()
        {
            if (_recording) return false;
            if (_process != null) return false;

            var fileDirectory = Path.GetDirectoryName(_fileName);
            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            // Need to test on non passthrough AMD hardware,  hevc_amf did not work in a vm.

            // NVIDIA .\ffmpeg.exe -f gdigrab -fflags nobuffer -framerate 10 -i title="XD" -c:v hevc_nvenc -preset fast test.mp4
            // CPU/AMD .\ffmpeg.exe -f gdigrab -fflags nobuffer -framerate 10 -i title="XD" test.mp4

            var args = new StringBuilder();

            
            // Use hardware acceleration
            args.Append("-f gdigrab").Append(' ');
            args.Append("-fflags nobuffer").Append(' ');
            args.Append("-framerate " + _windowRecorderOptions.Framerate).Append(' ');
            // This does not work
            // args.Append("-preset ultrafast").Append(' '); // Set the preset to ultrafast.
            args.Append($"-i title=\"{_windowName}\"").Append(' ');


            if (_windowRecorderOptions.EncoderSetting == RecorderEncoderSetting.CPU)
            {
                //  empty for now
            }

            if (_windowRecorderOptions.EncoderSetting == RecorderEncoderSetting.NV)
            {
                args.Append("-c:v hevc_nvenc").Append(' ');
                args.Append("-preset fast").Append(' ');
            }

            args.Append($"\"{_fileName}\"");

            var startInfo = new ProcessStartInfo(WindowRecorder.FfmpegPath)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(WindowRecorder.FfmpegPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = args.ToString(),
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                RedirectStandardInput = true,
            };

            var process = new Process()
            {
                EnableRaisingEvents = false,
                StartInfo = startInfo,
            };

            process.ErrorDataReceived += (sender, eventArgs) => { };
            process.OutputDataReceived += (sender, eventArgs) => { };

            if (!process.Start())
            {
                return false;
            }

            _recording = true;
            _process = process;

            return true;
        }

        public bool Stop()
        {
            if (!_recording) return false;
            if (_process == null) return false;

            _process.StandardInput.Write('q');
            _process.StandardInput.Flush();
            _process = null;
            _recording = false;
            return true;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
