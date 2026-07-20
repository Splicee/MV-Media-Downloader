using System;
using System.ComponentModel;
using System.IO;

namespace MVMediaStudio.Core
{
    internal sealed class DownloadOptions
    {
        public string Preset;
        public string Quality;
        public string RateLimit;
        public string OutputDirectory;
        public bool Playlist;
        public bool Subtitles;
        public bool CookiesFromBrowser;
        public string CookieBrowserSpec;
        public bool NoOverwrite;
        public string ExtraArguments;
    }

    internal sealed class ConversionOptions
    {
        public string InputPath;
        public string OutputDirectory;
        public string Format;
        public string Codec;
        public string RateControl;
        public string Crf;
        public string VideoBitrate;
        public string AudioBitrate;
    }

    internal sealed class MediaInfo
    {
        public string Codec = "—";
        public string Profile = "";
        public int Width;
        public int Height;
        public long Bitrate;
        public double DurationSeconds;

        public string TechnicalSummary
        {
            get
            {
                string resolution = Width > 0 && Height > 0 ? Width + " × " + Height : "rozlišení neznámé";
                string bitrate = Bitrate > 0 ? FormatBitrate(Bitrate) : "bitrate neznámý";
                return Codec + "  •  " + resolution + "  •  " + bitrate;
            }
        }

        private static string FormatBitrate(long value)
        {
            if (value >= 1000000)
                return (value / 1000000d).ToString("0.0") + " Mb/s";
            return (value / 1000d).ToString("0") + " kb/s";
        }
    }

    internal sealed class ConversionJob : INotifyPropertyChanged
    {
        private string status;
        private double progress;
        private string codecDetails;

        public ConversionJob(string sourcePath)
        {
            SourcePath = sourcePath;
            status = "Připraveno";
            codecDetails = "Čeká na analýzu";
        }

        public string SourcePath { get; private set; }
        public string FileName { get { return Path.GetFileName(SourcePath); } }
        public MediaInfo Media { get; set; }

        public string Status
        {
            get { return status; }
            set { status = value; Notify("Status"); }
        }

        public double Progress
        {
            get { return progress; }
            set { progress = value; Notify("Progress"); Notify("ProgressText"); }
        }

        public string ProgressText
        {
            get { return Progress.ToString("0") + " %"; }
        }

        public string CodecDetails
        {
            get { return codecDetails; }
            set { codecDetails = value; Notify("CodecDetails"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }
    }

    internal sealed class ToolState
    {
        public string YtDlpPath;
        public string YtDlpVersion;
        public string FfmpegPath;
        public string FfmpegVersion;
        public string FfprobePath;
        public string JsRuntimePath;
        public string JsRuntimeName;
        public string JsRuntimeVersion;
        public string PluginDirectory;

        public bool HasYtDlp { get { return !string.IsNullOrWhiteSpace(YtDlpPath); } }
        public bool HasFfmpeg { get { return !string.IsNullOrWhiteSpace(FfmpegPath); } }
        public bool HasFfprobe { get { return !string.IsNullOrWhiteSpace(FfprobePath); } }
        public bool HasJsRuntime { get { return !string.IsNullOrWhiteSpace(JsRuntimePath); } }
    }
}
