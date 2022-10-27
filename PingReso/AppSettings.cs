using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingReso
{
    public class AppSettings
    {
        public string Domains { get; set; }
        public int MaxErrorCount { get; set; }

        public string ReceiveEmail { get; set; }
        public string SendEmail { get; set; }
        public string SendEmailPassword { get; set; }

        public int SleepsAfterPingAllDomain { get; set; }
        public int SleepBetweenPings { get; set; }
    }
}
