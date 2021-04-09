using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class user
    {
        public string name { get; set; }
        public DateTime? createDate { get; set; }
        public DateTime? lastlogin { get; set; }
        public List<string> role { get; set; }
        public int count { get; set; }
    }

    public class get_Member
    {
        public string name { get; set; }
        public DateTime create_date { get; set; }
        public DateTime last_login { get; set; }
        public List<string> roles { get; set; }
        public object providerUserKey { get; set; }
        public bool IsLockedOut { get; set; }
        public int? count { get; set; }
    }

    public class get_roleMember
    {
        public string name { get; set; }
    }
}