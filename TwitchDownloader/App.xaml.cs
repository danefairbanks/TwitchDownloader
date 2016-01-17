using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace TwitchDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] GetTwitchChunkUrls(string input, out string uid)
        {
            var result = new List<string>();
            var url = new Uri(input);
            var home = new Uri(string.Format("{0}://{1}{2}", url.Scheme, url.Host, string.Join(string.Empty, url.Segments.Take(url.Segments.Length - 1))));

            byte[] data;
            using (var wc = new WebClient())
            {
                data = wc.DownloadData(url);
            }

            using (var md5 = MD5.Create())
            {
                uid = new Guid(md5.ComputeHash(data)).ToString("n");
            }

            using (var ms = new MemoryStream(data))
            {
                using (var sr = new StreamReader(ms))
                {
                    string file = null, start = null, end = null;
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                        {
                            var pathAndQuery = line.Split('?');
                            var path = pathAndQuery[0];
                            var query = pathAndQuery[1].Split('&');
                            if (string.IsNullOrEmpty(file) || file != path)
                            {
                                if (!string.IsNullOrEmpty(file))
                                {
                                    result.Add(string.Format("{0}{1}?start_offset={2}&end_offset={3}", home, file, start, end));
                                }
                                file = path;
                                start = query[0].Split('=')[1];
                            }
                            end = query[1].Split('=')[1];
                        }
                    }
                    if (!string.IsNullOrEmpty(file))
                    {
                        result.Add(string.Format("{0}{1}?start_offset={2}&end_offset={3}", home, file, start, end));
                    }
                }
            }
            return result.ToArray();
        }

        public static string[] GetTwitchVideoUrls(string input)
        {
            var result = new List<string>();
            var url = new Uri(input);
            var vodId = url.Segments[url.Segments.Length - 1];
            var accessUrl = new Uri(string.Format("https://api.twitch.tv/api/vods/{0}/access_token", vodId));
            using (var wc = new WebClient())
            {
                Debug.WriteLine(format: "Downloading from {0}", args: new object[] { accessUrl.ToString() });
                var accessResult = JObject.Parse(wc.DownloadString(accessUrl));
                var playlistURL = new Uri(string.Format("http://usher.twitch.tv/vod/{0}?nauthsig={1}&nauth={2}", vodId, accessResult["sig"].Value<string>(), accessResult["token"].Value<string>()));
                Debug.WriteLine(format: "Downloading from {0}", args: new object[] { playlistURL.ToString() });
                using (var ms = new MemoryStream(wc.DownloadData(playlistURL)))
                {
                    using (var sr = new StreamReader(ms))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            Uri uri;
                            if (Uri.TryCreate(line, UriKind.Absolute, out uri))
                            {
                                result.Add(uri.ToString());
                            }
                        }
                    }
                }
            }
            return result.ToArray();
        }
    }
}
