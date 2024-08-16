using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System;
using Jellyfin.Plugin.JellyTube.Configuration;

namespace Jellyfin.Plugin.JellyTube.Api;


[ApiController]
[Route("jellytube")]
public class JellyTubeActivityController : ControllerBase
{
    public class JellyTubeData
    {
        public string VideoId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
        public string VideoResolution { get; set; } = "";
        public bool FreeFormat { get; set; } = false;
        public bool M4A { get; set; } = false;
    }
    
    [HttpGet("test")]
    public string JellyTubeTest()
    {
        
        return "this works!!!";
    }
    
    [HttpPost("submit_dl")]
    public string JellyTubeDownload([FromBody] JellyTubeData data)
    {
        try
            {
                string VideoId = data.VideoId;
                string DownloadFolder = data.DownloadFolder;
                string VideoResolution = data.VideoResolution;
                bool FreeFormat = data.FreeFormat;
                bool m4a = data.M4A;
                if(VideoResolution != "audio" && m4a == true) {
                    m4a = false;
                }
                
                //Space in front
                string command = "yt-dlp";
                
                if(VideoResolution == "audio") {        //Audio
                    command += " --format bestaudio";
                    if (m4a)
                    {
                        command += " --audio-format m4a";
                    }
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
                    if(!FreeFormat) {
                        command += " --merge-output-format mp4 --recode-video mp4";
                    }
                }
                
                if (FreeFormat && !m4a) {
                    command += " --prefer-free-formats";
                }
                
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
}
