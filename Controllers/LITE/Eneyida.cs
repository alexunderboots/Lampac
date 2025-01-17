﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web;
using Lampac.Engine;
using Lampac.Engine.CORE;
using Lampac.Models.LITE.Ashdi;
using System.Linq;

namespace Lampac.Controllers.LITE
{
    public class Eneyida : BaseController
    {
        [HttpGet]
        [Route("lite/eneyida")]
        async public Task<ActionResult> Index(string title, string original_title, int year, int t, int s = -1)
        {
            if (year == 0 || !AppInit.conf.Eneyida.enable)
                return Content(string.Empty);

            string content = await embed(memoryCache, title, original_title, year);
            if (content == null)
                return Content(string.Empty);

            bool firstjson = true;
            string html = "<div class=\"videos__line\">";

            if (!content.Contains("file:'[{"))
            {
                #region Фильм
                string hls = Regex.Match(content, "file:\"(https?://[^\"]+/index.m3u8)\"").Groups[1].Value;
                if (string.IsNullOrWhiteSpace(hls))
                    return Content(string.Empty);

                #region subtitle
                string subtitles = string.Empty;

                string subtitle = new Regex("\"subtitle\":\"([^\"]+)\"").Match(content).Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(subtitle);
                    while (match.Success)
                    {
                        if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string suburl = AppInit.conf.Eneyida.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{match.Groups[2].Value}" : match.Groups[2].Value;
                            subtitles += "{\"label\": \"" + match.Groups[1].Value + "\",\"url\": \"" + suburl + "\"},";
                        }

                        match = match.NextMatch();
                    }
                }

                subtitles = Regex.Replace(subtitles, ",$", "");
                #endregion

                hls = AppInit.conf.Eneyida.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{hls}" : hls;
                html += "<div class=\"videos__item videos__movie selector focused\" media=\"\" data-json='{\"method\":\"play\",\"url\":\"" + hls + "\",\"title\":\"" + (title ?? original_title) + "\", \"subtitles\": [" + subtitles + "]}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">По умолчанию</div></div>";
                #endregion
            }
            else
            {
                #region Сериал
                try
                {
                    var root = JsonConvert.DeserializeObject<List<Voice>>(new Regex("file:'([^\n\r]+)',").Match(content).Groups[1].Value);

                    #region Перевод
                    for (int i = 0; i < root.Count; i++)
                    {
                        string link = $"{AppInit.Host(HttpContext)}/lite/eneyida?title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&year={year}&t={i}";

                        html += "<div class=\"videos__button selector " + (t == i ? "active" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\"}'>" + root[i].title + "</div>";
                    }

                    html += "</div><div class=\"videos__line\">";
                    #endregion

                    if (s == -1)
                    {
                        for (int i = 0; i < root[t].folder.Count; i++)
                        {
                            string link = $"{AppInit.Host(HttpContext)}/lite/eneyida?title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&year={year}&t={t}&s={i}";

                            html += "<div class=\"videos__item videos__season selector " + (firstjson ? "focused" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\"}'><div class=\"videos__season-layers\"></div><div class=\"videos__item-imgbox videos__season-imgbox\"><div class=\"videos__item-title videos__season-title\">" + root[t].folder[i].title + "</div></div></div>";
                            firstjson = false;
                        }
                    }
                    else
                    {
                        string nameseason = Regex.Match(root[t].folder[s].title, "([0-9]+)$").Groups[1].Value;

                        foreach (var episode in root[t].folder[s].folder)
                        {
                            #region subtitle
                            string subtitles = string.Empty;

                            if (!string.IsNullOrWhiteSpace(episode.subtitle))
                            {
                                var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(episode.subtitle);
                                while (match.Success)
                                {
                                    if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                                    {
                                        string suburl = AppInit.conf.Eneyida.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{match.Groups[2].Value}" : match.Groups[2].Value;
                                        subtitles += "{\"label\": \"" + match.Groups[1].Value + "\",\"url\": \"" + suburl + "\"},";
                                    }

                                    match = match.NextMatch();
                                }
                            }

                            subtitles = Regex.Replace(subtitles, ",$", "");
                            #endregion

                            string file = AppInit.conf.Eneyida.streamproxy ? $"{AppInit.Host(HttpContext)}/proxy/{episode.file}" : episode.file;
                            html += "<div class=\"videos__item videos__movie selector " + (firstjson ? "focused" : "") + "\" media=\"\" s=\"" + nameseason + "\" e=\"" + Regex.Match(episode.title, "([0-9]+)$").Groups[1].Value + "\" data-json='{\"method\":\"play\",\"url\":\"" + file + "\",\"title\":\"" + (title ?? original_title) + "\", \"subtitles\": [" + subtitles + "]}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">" + episode.title + "</div></div>";
                            firstjson = false;
                        }
                    }
                }
                catch 
                {
                    return Content(string.Empty);
                }
                #endregion
            }

            return Content(html + "</div>", "text/html; charset=utf-8");
        }


        #region embed
        async static ValueTask<string> embed(IMemoryCache memoryCache, string title, string original_title, int year)
        {
            string memKey = $"eneyida:view:{title}:{original_title}:{year}";

            if (!memoryCache.TryGetValue(memKey, out string content))
            {
                System.Net.WebProxy proxy = null;
                if (AppInit.conf.Eneyida.useproxy)
                    proxy = HttpClient.webProxy();

                string search = await HttpClient.Post($"{AppInit.conf.Eneyida.host}/index.php?do=search", $"do=search&subaction=search&search_start=0&result_from=1&story={HttpUtility.UrlEncode(original_title ?? title)}", timeoutSeconds: 8, proxy: proxy);
                if (search == null)
                    return null;

                string link = null;
                foreach (string row in search.Split("<article ").Skip(1))
                {
                    if (row.Contains(">Анонс</div>") || row.Contains(">Трейлер</div>"))
                        continue;

                    if (Regex.Match(row, "/year/[0-9]+/?\">([0-9]{4})</a>").Groups[1].Value == year.ToString())
                    {
                        link = Regex.Match(row, "href=\"(https?://[^/]+/[^\"]+\\.html)\"").Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(link))
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(link))
                    return null;

                string news = await HttpClient.Get(link, timeoutSeconds: 8, proxy: proxy);
                if (news == null)
                    return null;

                string iframeUri = Regex.Match(news, "<iframe width=\"100%\" height=\"400\" src=\"(https?://[^/]+/[^\"]+/[0-9]+)\"").Groups[1].Value;
                if (string.IsNullOrWhiteSpace(iframeUri))
                    return null;

                content = await HttpClient.Get(iframeUri, timeoutSeconds: 8, proxy: proxy);
                if (content == null || !content.Contains("file:"))
                    return null;

                memoryCache.Set(memKey, content, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 40 : 10));
            }

            return content;
        }
        #endregion
    }
}
