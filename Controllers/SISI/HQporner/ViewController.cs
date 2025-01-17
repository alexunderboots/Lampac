﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Lampac.Engine;
using Lampac.Engine.CORE;
using Microsoft.Extensions.Caching.Memory;

namespace Lampac.Controllers.HQporner
{
    public class ViewController : BaseController
    {
        [HttpGet]
        [Route("hqr/vidosik")]
        async public Task<ActionResult> Index(string goni)
        {
            if (!AppInit.conf.HQporner.enable)
                return OnError("disable");

            string memKey = $"HQporner:vidosik:{goni}";
            if (!memoryCache.TryGetValue(memKey, out string iframeHtml))
            {
                string html = await HttpClient.Get($"{AppInit.conf.HQporner.host}/{goni}", referer: AppInit.conf.HQporner.host, timeoutSeconds: 10, useproxy: AppInit.conf.HQporner.useproxy);
                if (html == null)
                    return OnError("html");

                string iframeUri = new Regex("<iframe [^>]+ src=\"//([^/]+/video/[^/]+/)\"").Match(html).Groups[1].Value;
                iframeHtml = await HttpClient.Get($"https://{iframeUri}");
                if (iframeHtml == null)
                    return OnError("iframeHtml");

                memoryCache.Set(memKey, iframeHtml, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 40 : 5));
            }

            var stream_links = new Dictionary<string, string>();
            var match = new Regex("src=\"//([^\"]+)\" title=\"([^\"]+)\"").Match(iframeHtml.Replace("\\", ""));
            while (match.Success)
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value) && !match.Groups[2].Value.Contains("Default"))
                {
                    string hls = "http://" + match.Groups[1].Value;
                    stream_links.TryAdd(match.Groups[2].Value, AppInit.conf.HQporner.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{hls}" : hls);
                }

                match = match.NextMatch();
            }

            if (stream_links.Count == 0)
                return OnError("stream_links");

            stream_links = stream_links.Reverse().ToDictionary(k => k.Key, v => v.Value);
            return Json(stream_links);
        }
    }
}
