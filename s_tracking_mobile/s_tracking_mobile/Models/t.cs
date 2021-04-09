using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CommonLib;

namespace s_tracking_mobile.Models
{
    public class t
    {
    }

    public class index
    {
        public string phone_mobile { get; set; }
        public string phone_office { get; set; }
        public string service_order_no { get; set; }
        public DateTime? appointment_datetime { get; set; }
        public DateTime? appointment_to_datetime { get; set; }
        public string customer_lat { get; set; }
        public string customer_long { get; set; }
        public string customer_fullname { get; set; }
        public string customer_mobile { get; set; }        
        public tb_engineer engi { get; set; }
        public int? status_job { get; set; }
    }

    public class edit_loca
    {
        public string order_no { get; set; }
        public double new_lati { get; set; }
        public double new_longti { get; set; }
    }
}