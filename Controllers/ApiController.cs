﻿using Microsoft.AspNetCore.Mvc;
using Lampac.Engine;
using Lampac.Engine.CORE;
using System.Threading.Tasks;

namespace Lampac.Controllers
{
    public class ApiController : BaseController
    {
        [Route("/")]
        public ActionResult Index()
        {
            if (!System.IO.File.Exists("wwwroot/lampa-main/index.html"))
                return Content("api work", contentType: "text/plain; charset=utf-8");

            return LocalRedirect("/lampa-main/index.html");
        }

        [HttpGet]
        [Route("lampainit.js")]
        public ActionResult LamInit()
        {
            string file = System.IO.File.ReadAllText("lampainit.js");
            file = file.Replace("{localhost}", AppInit.Host(HttpContext));
            file = file.Replace("{jachost}", AppInit.Host(HttpContext).Replace("https://", "").Replace("http://", ""));

            return Content(file, contentType: "application/javascript; charset=utf-8");
        }

        [HttpGet]
        [Route("msx/start.json")]
        public ActionResult MSX()
        {
            string file = System.IO.File.ReadAllText("msx.json");
            file = file.Replace("{localhost}", AppInit.Host(HttpContext));

            return Content(file, contentType: "application/json; charset=utf-8");
        }

        [HttpGet]
        [Route("sisi.js")]
        public ActionResult Sisi()
        {
            string file = System.IO.File.ReadAllText("sisi.js");
            file = file.Replace("{localhost}", $"{AppInit.Host(HttpContext)}/sisi");

            return Content(file, contentType: "application/javascript; charset=utf-8");
        }

        [HttpGet]
        [Route("lite.js")]
        public ActionResult Lite()
        {
            string file = System.IO.File.ReadAllText("lite.js");

            string addonline = string.Empty;

            if (!string.IsNullOrWhiteSpace(AppInit.conf.Bazon.token))
                addonline += "{name:'Bazon',url:'{localhost}/bazon'},";

            if (!string.IsNullOrWhiteSpace(AppInit.conf.Alloha.token))
                addonline += "{name:'Alloha',url:'{localhost}/alloha'},";

            if (!string.IsNullOrWhiteSpace(AppInit.conf.Kodik.token))
                addonline += "{name:'Kodik',url:'{localhost}/kodik'},";

            if (!string.IsNullOrWhiteSpace(AppInit.conf.HDVB.token))
                addonline += "{name:'HDVB',url:'{localhost}/hdvb'},";

            if (!string.IsNullOrWhiteSpace(AppInit.conf.IframeVideo.token))
                addonline += "{name:'IframeVideo',url:'{localhost}/iframevideo'},";

            file = file.Replace("{addonline}", addonline);
            file = file.Replace("{localhost}", $"{AppInit.Host(HttpContext)}/lite");

            return Content(file, contentType: "application/javascript; charset=utf-8");
        }

        [HttpGet]
        [Route("online.js")]
        async public Task<ActionResult> Online()
        {
            string cachekey = "online.js";
            var cachetxt = await HtmlCache.Read(cachekey);

            if (!cachetxt.cache)
            {
                string txt = await HttpClient.Get("https://pastebin.com/raw/3VubfYPR");
                if (txt == null || !txt.Contains("Lampa.Reguest()"))
                    txt = await HttpClient.Get("http://jin.energy/newonline.js?v=1667137689");

                if (txt != null && txt.Contains("Lampa.Reguest()"))
                {
                    cachetxt.html = txt;
                    await HtmlCache.Write(cachekey, txt);
                }

                if (cachetxt.html == null)
                    return OnError("cachetxt");
            }

            return Content(cachetxt.html, contentType: "application/javascript; charset=utf-8");
        }
    }
}
