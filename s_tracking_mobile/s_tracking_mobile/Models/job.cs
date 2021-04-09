using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class job
    {
        public string Id { get; set; }
        public string Engineer { get; set; }
        public string Store { get; set; }
        public string Serial { get; set; }
        public string Update { get; set; }
        public string table_error { get; set; }
    }

    public class error_import
    {
        public string text { get; set; }
    }

    //Category
    public class category
    {
        public string category_name { get; set; }
        public List<sub_data> data { get; set; }
    }

    public class sub_data
    {
        public string name { get; set; }
        public string code { get; set; }
    }

    //add Category
    public class cat_count
    {
        public int id { get; set; }
        public Guid? guid_category { get; set; }
        public int? count { get; set; }
        public string name { get; set; }
        public string code { get; set; }
    }

    //all data
    public class all_job
    {
        public int? id { get; set; }
        public Guid? job_guid { get; set; }
        public string job_no { get; set; }
        public string status { get; set; }
        public DateTime? duedate { get; set; }
        public DateTime? to_duedate { get; set; }
        public string customer_name { get; set; }
        public string job_type { get; set; }
        public string asset { get; set; }
        public string engineer_name { get; set; }
        public string mobile { get; set; }
        public string time { get; set; }
        public int count { get; set; }
        public DateTime? setDateOrder { get; set; }

        // 2 / 12 /2020  add more negative survey
        public int? negative { get; set; }
    }

    public class all_job_export
    {
        public string job_type { get; set; }
        public string job_no { get; set; }
        public string status { get; set; }
        public DateTime? duedate { get; set; }
        public DateTime? to_duedate { get; set; }
        public string customer_name { get; set; }
        public string customer_mobile { get; set; }
        public string customer_home { get; set; }
        public string customer_office { get; set; }
        public string customer_street { get; set; }
        public string customer_district { get; set; }
        public string customer_province { get; set; }
        public string customer_zipcode { get; set; }
        public string customer_lat { get; set; }
        public string customer_lng { get; set; }
        public string asset { get; set; }
        public string engineer_code { get; set; }
        public string engineer_name { get; set; }
        public string site_code { get; set; }
        public string site_name { get; set; }
        public string description_breakdown { get; set; }
        public string description_parts { get; set; }
        public DateTime? job_start { get; set; }
        public DateTime? job_repair { get; set; }
        public DateTime? job_end { get; set; }
        public string engineer_signature_path { get; set; }
        public string engineer_signature_name { get; set; }
        public string reason_cancel { get; set; }
        public DateTime? cancel_date { get; set; }
        public List<picture_voice> picture { get; set; }
        public List<picture_voice> voice { get; set; }
        public DateTime? customer_date { get; set; }
        public string customer_ip { get; set; }
        public List<CommonLib.tb_log> sms_send { get; set; }
        public DateTime? sms_send_new { get; set; }
        public DateTime? customer_prefer_date { get; set; }
        public string customer_perfer_time { get; set; }
    }

    public class picture_voice
    {
        public string path { get; set; }
        public string name { get; set; }
        public int? sort { get; set; }
    }

    //edit job
    public class edit_job
    {
        public int Id { get; set; }
        public Guid Job_guid { get; set; }
        public string CreateBy { get; set; }
        public string JobNumber { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Phone_home { get; set; }
        public string Phone_mobile { get; set; }
        public string Phone_office { get; set; }
        public string Home_address { get; set; }
        public string Village { get; set; }
        public string Moo { get; set; }
        public string Street { get; set; }
        public int? Sub_district { get; set; }
        public int? District { get; set; }
        public int? Province { get; set; }
        public int? Zipcode { get; set; }
        public int? Category { get; set; }
        public int? Sub_category { get; set; }
        public string Model { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public int? Store { get; set; }
        public int? Engineer { get; set; }
        public int Status_job { get; set; }
        public int? Gender { get; set; }
        public DateTime Appointment_datetime { get; set; }
        public DateTime Appointment_to_datetime { get; set; }
        public string customer_lat { get; set; }
        public string customer_long { get; set; }
    }

    public class log_job
    {
        public DateTime? Appointment_datetime { get; set; }
        public DateTime? Appointment_to_datetime { get; set; }
        public string status_job { get; set; }
        public DateTime? job_start { get; set; }
        public DateTime? job_repair { get; set; }
        public DateTime? job_end { get; set; }
        public string reason_cancel { get; set; }
        public DateTime? cencel_date { get; set; }
    }


    //MAP
    public class api_map
    {
        public List<Result> results { get; set; }
        public string status { get; set; }
    }

    public class AddressComponent
    {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public List<string> types { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Result
    {
        public List<AddressComponent> address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public bool partial_match { get; set; }
        public string place_id { get; set; }
        public List<string> types { get; set; }
    }

}