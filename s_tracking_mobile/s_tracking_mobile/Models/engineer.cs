using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class engineer
    {
        public int id { get; set; }
        public Guid en_guid { get; set; }
        public string code_engineer { get; set; }
        public string en_name { get; set; }
        public string shopname { get; set; }
        public string en_province { get; set; }
        public string en_tel1 { get; set; }
        public int? count { get; set; }
    }

    public class edit_engineer
    {
        public int id { get; set; }
        public Guid en_guid { get; set; }
        public string en_name { get; set; }
        public string en_tel1 { get; set; }
        public string en_email { get; set; }
        public int? en_province { get; set; }
        public int? site_id { get; set; } 
    }

    public class api_firebase_auth
    {
        public string kind { get; set; }
        public string localId { get; set; }
        public string email { get; set; }
        public string displayName { get; set; }
        public string idToken { get; set; }
        public bool registered { get; set; }
        public string refreshToken { get; set; }
        public string expiresIn { get; set; }
    }

    public class getEngineer
    {
        public int id { get; set; }
        public string name { get; set; }
        public int? site_id { get; set; }
        public string code { get; set; }
        public string tel1 { get; set; }
        public string tel2 { get; set; }
        public string tel3 { get; set; }
        public string repair_type_info1 { get; set; }
        public string repair_type_info2 { get; set; }
        public string repair_type_info3 { get; set; }
    }
}