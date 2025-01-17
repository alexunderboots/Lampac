﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Web;
using Lampac.Engine;
using Lampac.Engine.CORE;
using Lampac.Models.SISI;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace Lampac.Controllers.Xnxx
{
    public class ListController : BaseController
    {
        [HttpGet]
        [Route("xnx")]
        async public Task<JsonResult> Index(string search, int pg = 1)
        {
            if (!AppInit.conf.Xnxx.enable)
                return OnError("disable");

            string url = $"{AppInit.conf.Xnxx.host}/best/{DateTime.Today.ToString("yyyy-MM")}/{pg}";
            if (!string.IsNullOrWhiteSpace(search))
                url = $"{AppInit.conf.Xnxx.host}/search/{HttpUtility.UrlEncode(search)}/{pg}";

            string memKey = $"Xnxx:list:{search}:{pg}";
            if (!memoryCache.TryGetValue(memKey, out string html))
            {
                html = await HttpClient.Get(url, timeoutSeconds: 10, useproxy: AppInit.conf.Xnxx.useproxy);
                if (html == null || !html.Contains("<div id=\"video_"))
                    return OnError("html");

                memoryCache.Set(memKey, html, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 20 : 5));
            }

            var playlists = getTubes(html);
            if (playlists.Count == 0)
                return OnError("playlists");

            return new JsonResult(new
            {
                menu = new List<MenuItem>()
                {
                    new MenuItem()
                    {
                        title = "Поиск",
                        search_on = "search_on",
                        playlist_url = $"{AppInit.Host(HttpContext)}/xnx",
                    }
                },
                list = playlists
            });
        }


        #region getTubes
        List<PlaylistItem> getTubes(string html)
        {
            var playlists = new List<PlaylistItem>();

            foreach (string row in Regex.Split(Regex.Replace(html, "[\n\r\t]+", ""), "<div id=\"video_").Skip(1))
            {
                var g = new Regex($"<a href=\"/(video-[^\"]+)\" title=\"([^\"]+)\"", RegexOptions.IgnoreCase).Match(row).Groups;
                string quality = new Regex("<span class=\"superfluous\"> - </span>([^<]+)</span>", RegexOptions.IgnoreCase).Match(row).Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(g[1].Value) && !string.IsNullOrWhiteSpace(g[2].Value))
                {
                    string duration = new Regex("</span>([^<]+)<span class=\"video-hd\">", RegexOptions.IgnoreCase).Match(row).Groups[1].Value.Trim();
                    string img = new Regex("data-src=\"([^\"]+)\"", RegexOptions.IgnoreCase).Match(row).Groups[1].Value;
                    img = img.Replace(".THUMBNUM.", ".1.");

                    playlists.Add(new PlaylistItem()
                    {
                        name = g[2].Value,
                        video = $"{AppInit.Host(HttpContext)}/xnx/vidosik?goni={HttpUtility.UrlEncode(g[1].Value)}",
                        picture = AppInit.conf.Xnxx.streamproxy ? $"{AppInit.Host(HttpContext)}/proxyimg/{img}" : img,
                        time = duration,
                        quality = string.IsNullOrWhiteSpace(quality) ? null : quality,
                        json = true
                    });
                }
            }

            return playlists;
        }
        #endregion
    }
}
