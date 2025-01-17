﻿namespace Lampac.Models.LITE
{
    public class OnlinesSettings
    {
        public OnlinesSettings(string host, bool useproxy = false, string token = null, bool enable = true)
        {
            this.host = host;
            this.enable = enable;
            this.token = token;
            this.useproxy = useproxy;
        }


        public string host { get; set; }

        public bool enable { get; set; }

        public string token { get; set; }

        public bool useproxy { get; set; }

        public bool streamproxy { get; set; }
    }
}
