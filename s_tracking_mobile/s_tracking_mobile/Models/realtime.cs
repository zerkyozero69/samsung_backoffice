using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class realtime
    {
        public int? store_id { get; set; }
        public Guid? store_guid { get; set; }
        public string store_name { get; set; }
        public string store_province { get; set; }
        public int? all_job { get; set; }
        public string store_contact { get; set; }
        public string store_tel { get; set; }
        public DateTime? store_appointment { get; set; }
        public string store_close { get; set; }
        public string  store_date_open1 { get; set; }
        public string store_to_date_open1 { get; set; }
        public string store_time_open1 { get; set; }
        public string store_to_time_open1 { get; set; }
        public string store_date_open2 { get; set; }
        public int count_jobDelay { get; set; }
        public string store_to_date_open2 { get; set; }
        public string store_time_open2 { get; set; }
        public string store_to_time_open2 { get; set; }
        public int? store_job_status { get; set; }
        public List<jobs_realtime> jobs { get; set; }
        public int? count { get; set; }
        public List<getEngineer> engineer { get; set; }
    }
    public class jobs_realtime
    {
        public int? job_id { get; set; }
        public Guid? job_guid { get; set; }
        public string job_number { get; set; }
        public string en_name { get; set; }
        public string job_status { get; set; }
        public string compare_status { get; set; }
        public string customer_name { get; set; }
        public string en_code { get; set; }
        public int? en_id { get; set; }
        public DateTime? app_time { get; set; }
        public DateTime? app_to_time { get; set; }
    }

    public class countJob
    {
        public int? all_job { get; set; }
        public int? repair { get; set; }
        public int? pending { get; set; }
        public int? delay_job { get; set; }
        public int? completed { get; set; }
        public int? cancel { get; set; }
        public int? waiting { get; set; }
    }

    public class map_data
    {
        public List<map_engineer> engineer { get; set; }
        public List<CommonLib.tb_engineer> list_engineer { get; set; }
    }

    public class map_engineer
    {
        public int en_id { get; set; }
        public Guid en_guid { get; set; }
        public string en_code { get; set; }
        public string en_name { get; set; }
        public string en_site { get; set; }
        public string en_tel { get; set; }
        public int en_count_alljob { get; set; }
        public int en_count_pending { get; set; }
        public int en_count_completed { get; set; }
        public int en_count_cencel { get; set; }
        public string en_lat { get; set; }
        public string en_long { get; set; }
        public List<map_job> jobs { get; set; }
    }

    public class map_job
    {
        public int job_id { get; set; }
        public Guid job_guid { get; set; }
        public string job_number { get; set; }
        public string job_status { get; set; }
        public int? job_status_int { get; set; }
        public string cus_name { get; set; }
        public string assets { get; set; }
        public string cus_lat { get; set; }
        public string cus_long { get; set; }
        public string cus_tel { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
    }
}