using Jellyfin.Plugin.JellyTube.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Model.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System;

namespace Jellyfin.Plugin.JellyTube.Api;


[ApiController]
[Route("jellytube")]
public class JellyTubeActivityController : ControllerBase
{
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _config;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILibraryMonitor _libraryMonitor;

    public JellyTubeActivityController(
        IFileSystem fileSystem,
        IServerConfigurationManager config,
        IUserManager userManager,
        ILibraryManager libraryManager,
        ILibraryMonitor libraryMonitor)
    {
        _fileSystem = fileSystem;
        _config = config;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _libraryMonitor = libraryMonitor;
    }
    
    public class JellyTubeVideoData
    {
        public string VideoId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
        public string VideoResolution { get; set; } = "";
        // public bool FreeFormat { get; set; } = false;
        public bool M4A { get; set; } = false;
    }
    
    public class JellyTubePlaylistData
    {
        public string PlaylistId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
        public string VideoResolution { get; set; } = "";
        // public bool FreeFormat { get; set; } = false;
        public bool M4A { get; set; } = false;
        
        public int MaxDownloadCount { get; set; } = 0;
    }
    
    
    public static string runCommand(string command) {
        try {
            Process process = new Process();
            process.StartInfo.FileName = "bash";
            process.StartInfo.Arguments = $"-c \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        } catch (Exception e) {
            return "";
        }
    }
    
    

    [HttpGet("test")]
    public IActionResult JellyTubeTest()
    {
        PluginConfiguration? config = Plugin.Instance.Configuration;
        string responseText = $"this works!!!\n{config.DefaultDownloadFolder}\n{config.UseDefaultPath}";
        // Return the response as plain text
        return Content(responseText, "text/plain");
    }
    
    [HttpPost("submit_dl")]
    public string JellyTubeDownload([FromBody] JellyTubeVideoData data)
    {
        try {
            string VideoId = data.VideoId;
            string DownloadFolder = data.DownloadFolder;
            string VideoResolution = data.VideoResolution;
            // bool FreeFormat = data.FreeFormat;
            bool m4a = data.M4A;
            if(VideoResolution != "audio" && m4a == true) { m4a = false; }
            
            //Space in front
            string command = "yt-dlp";
            
            if(VideoResolution == "audio") {        //Audio
                command += " --format bestaudio";
                if (m4a) { command += " --recode-video m4a"; }
                else { command += " --merge-output-format mp4 --recode-video mp4"; }
            } else {
                if(VideoResolution == "max") {          //Max resolution
                    command += " --format 'bestvideo+bestaudio'";
                } else if(VideoResolution == "min") {   //Min resolution
                    command += " --format 'worstvideo+bestaudio'";
                } else {                        //Video resolution
                    if (int.TryParse(VideoResolution, out int resolution))
                    {
                        command += $" --format 'bestvideo[height<={resolution}]+bestaudio'";
                    }
                }
                // if(!FreeFormat) { command += " --merge-output-format mp4 --recode-video mp4"; }
                command += " --merge-output-format mp4 --recode-video mp4";
            }
            
            // if (FreeFormat && !m4a) { command += " --prefer-free-formats"; }
            command += " --embed-thumbnail";
            
            if(!string.IsNullOrEmpty(DownloadFolder)) {
                command += " --output '" + DownloadFolder + "/%(title)s[%(id)s].%(ext)s'";
            } else {return "Download folder directory is empty"; }
            
            if(!string.IsNullOrEmpty(VideoId)) {
                command += " https://www.youtube.com/watch?v=" + VideoId;
            } else {return "Video ID is empty";}
            Console.WriteLine("Running command: \n" + command);
            //Run command
            Process process = new Process();

            // Configure the process start information
            process.StartInfo.FileName = "bash"; // Use "sh" if bash isn't available
            process.StartInfo.Arguments = $"-c \"{command}\""; // "-c" tells bash to execute the command and then terminate
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true; // Avoid showing the command window
            // Start the process
            process.Start();
            // Read the output
            string output = process.StandardOutput.ReadToEnd();
            // Wait for the process to finish
            process.WaitForExit();
            // Display the output
            Console.WriteLine("Output:");
            Console.WriteLine(output);
            
            // Notify Jellyfin that the filesystem has changed
            _libraryMonitor.ReportFileSystemChanged(DownloadFolder);
            
            return "Downloaded";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
    
    
    [HttpPost("submit_playlist_dl")]
    public string JellyTubePlaylistDownload([FromBody] JellyTubePlaylistData data)
    {
        try
        {
            PluginConfiguration? config = Plugin.Instance.Configuration;
            string PlaylistId = data.PlaylistId;
            string DownloadFolder = data.DownloadFolder;
            string VideoResolution = data.VideoResolution;
            // bool FreeFormat = data.FreeFormat;
            bool m4a = data.M4A;
            int MaxDownloadCount = data.MaxDownloadCount;

            if (string.IsNullOrEmpty(PlaylistId))
            {
                return "Playlist ID is empty.";
            }

            // Get video IDs using yt-dlp and jq
            string fetchIdCommand = $"yt-dlp --flat-playlist -J 'https://www.youtube.com/playlist?list={PlaylistId}' | jq -r '.entries[].id'";
            Console.WriteLine("Running command: \n" + fetchIdCommand);

            // Run command
            string output = runCommand(fetchIdCommand);
            string[] videoIds = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (MaxDownloadCount == 0) { MaxDownloadCount = videoIds.Length; }
            MaxDownloadCount = Math.Min(MaxDownloadCount, videoIds.Length);
            
            
            string[] sizedVideoIds = new string[MaxDownloadCount];
            Array.Copy(videoIds, sizedVideoIds, MaxDownloadCount);
            
            //get video titles
            string fetchTitleCommand = $"yt-dlp --flat-playlist -J 'https://www.youtube.com/playlist?list={PlaylistId}' | jq -r '.entries[].title'";
            Console.WriteLine("Running command: \n" + fetchTitleCommand);
            
            output = runCommand(fetchTitleCommand);
            string[] videoTitles = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (MaxDownloadCount == 0) { MaxDownloadCount = videoTitles.Length; }
            MaxDownloadCount = Math.Min(MaxDownloadCount, videoTitles.Length);
            
            // return string.Join(" ",videoIds) + "<br>" + string.Join(" ",videoTitles);
            
            string playlistTitle = runCommand($"yt-dlp 'https://www.youtube.com/playlist?list={PlaylistId}' --skip-download --print playlist_title --no-warning -I 1:1");
            
            string[] playlistPaths = new string[MaxDownloadCount];
            
            for(int i = 0; i < MaxDownloadCount; i++) {
                playlistPaths[i] = $"{DownloadFolder}/{videoTitles[i]}[{sizedVideoIds[i]}].mp4";
                try {
                    if (System.IO.File.Exists(playlistPaths[i]))
                    {
                        System.IO.File.Delete(playlistPaths[i]); 
                        Console.WriteLine("deleted " + playlistPaths[i]);
                    }
                } catch(Exception e) {
                   continue;
                }
            }
            
            //Create xml
            var xml = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Item",
                    new XElement("Added", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")),
                    new XElement("LockData", "false"),
                    new XElement("LocalTitle", playlistTitle),
                    new XElement("PlaylistItems",
                        playlistPaths.Select(path => 
                            new XElement("PlaylistItem",
                                new XElement("Path", path)
                            )
                        )
                    ),
                    new XElement("Shares"),
                    new XElement("PlaylistMediaType", "Video")
                )
            );
            if(config.ConfigFolder == "Not Set") { return "ConfigFolder is not set"; }
            if(Directory.Exists(config.ConfigFolder) == false) { return "ConfigFolder is invalid"; }
            string directoryPath = $"{config.ConfigFolder}/data/playlists/{playlistTitle}/";
            string filePath = Path.Combine(directoryPath, "playlist.xml");
            
            // Ensure the directory exists
            if (!Directory.Exists(directoryPath))
            { Directory.CreateDirectory(directoryPath); }
            xml.Save(filePath);
            
            
            
            // Adjust format flags based on settings
            string commandTemplate = "yt-dlp";
            if (VideoResolution == "audio")
            {
                commandTemplate += " --format bestaudio";
                if (m4a) { commandTemplate += " --recode-video m4a"; }
                else { commandTemplate += " --merge-output-format mp4 --recode-video mp4"; }
            }
            else
            {
                if (VideoResolution == "max")
                { commandTemplate += " --format 'bestvideo+bestaudio'"; }
                else if (VideoResolution == "min")
                { commandTemplate += " --format 'worstvideo+bestaudio'"; }
                else if (int.TryParse(VideoResolution, out int resolution))
                { commandTemplate += $" --format 'bestvideo[height<={resolution}]+bestaudio'"; }

                // if (!FreeFormat) { commandTemplate += " --merge-output-format mp4 --recode-video mp4"; }
                commandTemplate += " --merge-output-format mp4 --recode-video mp4";
            }

            // if (FreeFormat && !m4a) { commandTemplate += " --prefer-free-formats"; }
            commandTemplate += " --embed-thumbnail";

            if (!string.IsNullOrEmpty(DownloadFolder))
            { commandTemplate += $" --output '{DownloadFolder}/%(title)s[%(id)s].%(ext)s'"; }
            else 
            { return "Download folder directory is empty"; }

            // Download videos
            foreach (string VideoId in sizedVideoIds)
            {
                if (!string.IsNullOrEmpty(VideoId))
                {
                    string videoCommand = $"{commandTemplate} https://www.youtube.com/watch?v={VideoId}";
                    runCommand(videoCommand);
                    Console.WriteLine("Running command: \n" + videoCommand);
                }
            }
            
            // Notify Jellyfin that the filesystem has changed
            _libraryMonitor.ReportFileSystemChanged(DownloadFolder);
            _libraryMonitor.ReportFileSystemChanged(config.ConfigFolder + "/data/playlists");
            Console.WriteLine("Done");
            return "Downloaded";

        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
    
}
