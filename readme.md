Similar to https://github.com/AECX/FinTube but with more features and universal support(docker linux, macos, windows, ect).

Install:
1.Install yt-dlp on current jellyfin machine(docker, linux ect) with pip
2.Install jq example: ```brew install jq``` or ```sudo apt-get install jq``` or for windows ```choco install jq```
3.Add repo ```https://raw.githubusercontent.com/EnderRobber101/JellyTube/main/manifest.json```

Note:
When downloading the same video again, it will be replaced by the new quality. It might not show as the higher/lower quality version even after restarting Jellyfin. In that case, try clearing your browser cache.



Important!!!!
When you are updating the version, you need to go into the file system and manually delete the original/previous version before installing the new version due to some error. Sorry for the incontinence.









--embed-thumbnail
--embed-chapters
--embed-subs
--sub-langs "all"
--embed-metadata
--embed-info-json 

--merge-output-format mp4 --recode-video mp4

yt-dlp -f 133 --embed-thumbnail --write-description --embed-subs --embed-metadata --merge-output-format mp4 --recode-video mp4 ''

yt-dlp "https://www.youtube.com/watch?v=KkCXLABwHP0" --embed-chapters --embed-subs --embed-info-json  --embed-metadata --merge-output-format mp4 --recode-video mp4 --format 'bestvideo[height<=720]+bestaudio' --sub-langs "all"
