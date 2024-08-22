Similar to https://github.com/AECX/FinTube but with more features and universal support(docker linux, macos, windows, ect).

Install:
1.Install yt-dlp on current jellyfin machine(docker, linux ect) with pip
2.Install jq example: ```brew install jq``` or ```sudo apt-get install jq``` or for windows ```choco install jq```
3.Add repo ```https://raw.githubusercontent.com/EnderRobber101/JellyTube/main/manifest.json```

Note:
When downloading the same video again, it will be replaced by the new quality. It might not show as the higher/lower quality version even after restarting Jellyfin. In that case, try clearing your browser cache.