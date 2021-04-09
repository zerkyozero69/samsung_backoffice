using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class site
    {
        public int id { get; set; }
        public Guid store_guid { get; set; }
        public string store_name { get; set; }
        public string store_address { get; set; }
        public string store_village { get; set; }
        public string store_moo { get; set; }
        public string store_street { get; set; }
        public string store_sub_district { get; set; }
        public string store_district { get; set; }
        public int? store_postcode { get; set; }
        public string store_province { get; set; }
        public string store_contact1 { get; set; }
        public string store_contact2 { get; set; }
        public string store_contact3 { get; set; }
        public string store_tel1 { get; set; }
        public string store_tel2 { get; set; }
        public string store_tel3 { get; set; }
        public string store_email1 { get; set; }
        public string store_email12 { get; set; }
        public string store_email13 { get; set; }
        public int? count { get; set; }

        //extra 
        public string code_store { get; set; }
    }

    public class times
    {
        public int id { get; set; }
        public string time { get; set; }
    }

    public class date
    {
        public int id { get; set; }
        public string day { get; set; }
    }

    public class validate_all
    {
        public string name_div { get; set; }
        public string text { get; set; }
    }

    public class getSite
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}