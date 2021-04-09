using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class notification
    {
        public int id { get; set; }
        public Nullable<DateTime> send_date { get; set; }
        public Guid guid_noti { get; set; }
        public Nullable<int> status { get; set; }
        public Nullable<int> category { get; set; }
        public string campaign_name { get; set; }
        //public Nullable<int> store { get; set; }
        //public Nullable<int> engineer { get; set; }
        public string description { get; set; }
        public string noti_status { get; set; }
        public DateTime create_date { get; set; }
        public int? wait { get; set; }
    }

    public class vali_noti
    {
        public string name { get; set; }
        public string text { get; set; }
    }

    public class firebase_noti
    {
        public string to { get; set; }
        public string priority { get; set; }
        //public List<string> registration_ids { get; set; }
        public Dictionary<string, string> notification { get; set; }
        public Dictionary<string, string> data { get; set; }
    }

    public class create_noti
    {
        public string campaign_name { get; set; }
        //public string category { get; set; }
        public string description { get; set; }
        public List<string> store { get; set; }
        public List<string> engineer { get; set; }
        public string send_date { get; set; }
        public string hour { get; set; }
        public string minute { get; set; }
        public int status { get; set; }
        public string title { get; set; }
        public string body { get; set; }
    }

    public class edit_noti
    {
        public int id { get; set; }
        public string campaign_name { get; set; }
        public string description { get; set; }
        //public int status { get; set; }
        public List<string> store { get; set; }
        public List<string> engineer { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string send_date { get; set; }
        public string hour { get; set; }
        public string minute { get; set; }
    }

    public class all_notification
    {
        public int id { get; set; }
        public DateTime create_date { get; set; }        
        public Nullable<int> is_read { get; set; }
        public string site_id { get; set; }
        public string user_update { get; set; }
        public string header { get; set; }
        public string detail { get; set; }
        public string noti_status { get; set; }
    }
}