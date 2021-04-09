using CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace s_tracking_mobile.Models
{
    public class Result_Dashboard_Job
    {
        public string site { get; set; }
        public int all_job { get; set; }
        public int done_job { get; set; }
        public int cancel_job { get; set; }
    }


    public class Dashboard_Survey
    {
        public int id { get; set; }
        public Guid job_guid { get; set; }
        public int? site { get; set; }
        public int survey { get; set; }
        public int positive { get; set; }
        public int negative { get; set; }

    }

    public class Result_Dashboard_Survey
    {

        public string site { get; set; }
        public int? job { get; set; }
        public int? survey { get; set; }
        public int? positive { get; set; }
        public int? negative { get; set; }
    }


    public class Survey_Export
    {
        public int id { get; set; }
        public Guid job_guid { get; set; }
        public string service_no { get; set; }
        public string engineer { get; set; }
        public string engineer_code { get; set; }
        public string customer { get; set; }
        public string customer_phone { get; set; }
        public decimal? total_score { get; set; }
        public DateTime survey_date { get; set; }
        public string comment { get; set; }
        public string feedback_note { get; set; }
        public int? is_feedback { get; set; }
        public DateTime? feedback_date { get; set; }
        public string user_feedback { get; set; }
        public string sitecode { get; set; }
        public string sitename { get; set; }
        public int? is_negative { get; set; }
        public List<QnA_Export> qna { get; set; }

    }

    public class QnA_Export
    {
        public int id { get; set; }
        public int? setorder { get; set; }
        public string quest { get; set; }
        public string answer { get; set; }
        public decimal? score { get; set; }
        public int? negative { get; set; }
        public string cate { get; set; }
        public int sub_type { get; set; }
        public int? job_cate { get; set; }
    }

    //public class NegativeForm
    //{
    //    public string service_no { get; set; }
    //    public string engineer_name { get; set; }
    //    public string engineer_code { get; set; }
    //    public string customer { get; set; }
    //    public string customer_phone { get; set; }
    //    public decimal? total_score { get; set; }
    //    public DateTime survey_date { get; set; }
    //    public List<QnA_Negative> qna { get; set; }
    //}

    //public class QnA_Negative
    //{
    //    public int setorder { get; set; }
    //    public string quest { get; set; }
    //    public string answer { get; set; }
    //    public decimal? score { get; set; }
    //}


    public class survey_all
    {
        public Guid job_guid { get; set; }
        public string service_no { get; set; }
        public string engineer_name { get; set; }
        public string site { get; set; }
        public decimal? score { get; set; }
        public bool negative { get; set; }
        public bool is_feedback { get; set; }
        public DateTime cre_date { get; set; }
        public int count { get; set; }
    }

    public class survey_job_edit
    {
        public Guid job_guid { get; set; }
        public int? is_feedback { get; set; }
        public string feedback_note { get; set; }
        public DateTime? feedback_date { get; set; }
        public string feedback_user { get; set; }
        public decimal? score { get; set; }
        public int survey_status { get; set; }
        public int? negative { get; set; }
        public string comment { get; set; }
        public DateTime cre_date { get; set; }
        public DateTime? open_link { get; set; }
  

        public List<tb_survey_item> qna { get; set; }
    }
}