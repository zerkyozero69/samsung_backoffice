using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class report
    {
        public string site_name { get; set; }
        public string site_code { get; set; }
        public int? count_job { get; set; }
        public int? all_done_job { get; set; }
        public int? all_cancel_job { get; set; }
        public int? all_perfer { get; set; }
        public int? all_delay { get; set; }
        public int? all_pending { get; set; }
        public List<report_engineer> list_engineer { get; set; }
        public int? count { get; set; }
        public int? all_sms_engineer { get; set; }
        public int? all_sms_customer { get; set; }
    }

    public class report_engineer
    {
        public string engineer_name { get; set; }
        public string engineer_code { get; set; }
        public int? all_job { get; set; }
        public int? done_job { get; set; }
        public int? cancel_job { get; set; }
        public int? perfer_date { get; set; }
        public int? delay_job { get; set; }
        public int? peding_job { get; set; }
        public int? sms_engineer { get; set; }
        public int? sms_customer { get; set; }
    }
}