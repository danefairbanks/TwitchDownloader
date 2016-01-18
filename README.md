# TwitchDownloader
Downloads twitch.tv past broadcasts and downloads.

Input twitch past broadcast or higlight URL into the textbox and click download.  It gets access tokens and a list of available chunked playlist, as detailed [https://github.com/fonsleenaars/twitch-hls-vods]. After choosing the proper chunked playlist, it starts a background worker process to download 5 chunks at a time.  After completion of downloads, it uses ffmpeg to combine the chunks into one file.  The scratch files are deleted upon completion.

Here is a compiled copy [http://danefairbanks.com/downloads/apps/twitchdownloader.zip]

# Licenses
This software uses libraries from the FFmpeg project under the LGPLv2.1
