using Jellyfin.Plugin.JellyTube.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Model.IO;
using System.Diagnostics;
using System;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.JellyTube.Api;


[ApiController]
[Route("jellytube")]
public class JellyTubeActivityController : ControllerBase
{
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _config;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    public JellyTubeActivityController(
        IFileSystem fileSystem,
        IServerConfigurationManager config,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _fileSystem = fileSystem;
        _config = config;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }
    
    public class JellyTubeVideoData
    {
        public string VideoId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
        public string VideoResolution { get; set; } = "";
        public bool FreeFormat { get; set; } = false;
        public bool M4A { get; set; } = false;
    }
    
    public class JellyTubePlaylistData
    {
        public string PlaylistId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
        public string VideoResolution { get; set; } = "";
        public bool FreeFormat { get; set; } = false;
        public bool M4A { get; set; } = false;
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
            bool FreeFormat = data.FreeFormat;
            bool m4a = data.M4A;
            if(VideoResolution != "audio" && m4a == true) { m4a = false; }
            
            //Space in front
            string command = "yt-dlp";
            
            if(VideoResolution == "audio") {        //Audio
                command += " --format bestaudio";
                if (m4a) { command += " --audio-format m4a"; }
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
                if(!FreeFormat) { command += " --merge-output-format mp4 --recode-video mp4"; }
            }
            
            if (FreeFormat && !m4a) { command += " --prefer-free-formats"; }
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

            return "Downloaded";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
    
    
    
    
    [HttpPost("submit_Playlist_dl")]
    public async Task<string> JellyTubePlaylistDownload([FromBody] JellyTubePlaylistData data)
    {
        try {
            PluginConfiguration? config = Plugin.Instance.Configuration;
            string PlaylistId = data.PlaylistId;
            string DownloadFolder = data.DownloadFolder;
            string VideoResolution = data.VideoResolution;
            bool FreeFormat = data.FreeFormat;
            bool m4a = data.M4A;
            var apiKey = config.YouTubeAPIKey;
            
            //Check if youtube api key is set
            if(config.YouTubeAPIKey == "Not Set") 
            { return "Please set your YouTube API Key in settings"; }
            if (string.IsNullOrEmpty(PlaylistId))
            {
                return "Playlist ID is empty.";
            }

            // Initialize the YouTube service with the API key
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "JellyTube"
            });

            var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
            playlistItemsRequest.PlaylistId = PlaylistId;
            playlistItemsRequest.MaxResults = 50;

            var videoIds = new List<string>();

            while (true)
            {
                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                foreach (var playlistItem in playlistItemsResponse.Items)
                {
                    var videoId = playlistItem.Snippet.ResourceId.VideoId;
                    videoIds.Add(videoId);
                }

                if (string.IsNullOrEmpty(playlistItemsResponse.NextPageToken))
                {
                    break;
                }

                playlistItemsRequest.PageToken = playlistItemsResponse.NextPageToken;
            }

            // Download each video using yt-dlp
            foreach (var videoId in videoIds)
            {
                
            }
            
            
        } catch (Exception e) {
            return "";
        }
    }
    
}
