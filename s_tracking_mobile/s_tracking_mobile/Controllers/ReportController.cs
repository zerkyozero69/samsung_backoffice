using CommonLib;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    public class ReportController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        // GET: Report
        public ActionResult Index()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["All-Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
            ViewData["All-Category"] = db.tb_jobsl_category.Where(w => w.parent_id == 0).ToList();

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(-7).Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);
            
            var s_start_date = start_date.ToString("dd/MM/yyyy");
            var s_end_date = end_date.ToString("dd/MM/yyyy");

            var getName = User.Identity.Name;
            if (User.IsInRole("admin") || (getName != "" && getName != null))
            {
                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = ViewBag.roleUser != true ? db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault() : 0;

                ViewBag.id_store = idStore;
                GetData(true, "", 0, 0, 99, s_start_date, s_end_date,1,false);
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            return View();
        }

        public ActionResult chart()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["All-Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
            ViewData["All-Category"] = db.tb_jobsl_category.Where(w => w.parent_id == 0).ToList();
            ViewBag.roleUser = User.IsInRole("admin");
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(-7).Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);

            var s_start_date = start_date.ToString("dd/MM/yyyy");
            var s_end_date = end_date.ToString("dd/MM/yyyy");


            var getName = User.Identity.Name;
            if (User.IsInRole("admin") || (getName != "" && getName != null))
            {
                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = ViewBag.roleUser != true ? db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault() : 0;

                ViewBag.id_store = idStore;
                GetData(true, "", 0, 0, 99, s_start_date, s_end_date, 1,true);
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            return View();
        }

        [HttpGet]
        public object GetData(bool first, string search, int site, int category , int status, string s_start_date , string s_end_date, int page,bool chart)
        {
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            skipPage = chart ? 0 : skipPage;
            var take = chart ? 10000 : 20;

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            List<string> tem_id = new List<string>();
            if (search != "")
            {
                var data_id = (from j in db.tb_jobs
                               where (from e in db.tb_engineer
                                      where e.engineer_name.Contains(search.Trim())
                                            && j.engineer_id == e.id
                                            && j.appointment_datetime != null 
                                            && j.appointment_to_datetime != null
                                            && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                      select e.id).Count() > 0
                               group j.store_id by j.store_id into j
                               select new { id = j.Key}).ToList();


                foreach(var add_data in data_id)
                {
                    tem_id.Add(add_data.id.ToString());
                }
            }

            var filter_data_site = new List<CommonLib.tb_store>();
            var filter_data_job = new List<CommonLib.tb_jobs>();
            var filter_data_engineer = new List<CommonLib.tb_engineer>();
            var filter_data_logSMS = new List<CommonLib.tb_log>();

            if (site == 0 && category == 0 && status == 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j. is_delete == 0 && j.engineer_id == en.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if(site != 0 && category == 0 && status == 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if(site != 0 && category != 0 && status == 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if(site != 0 && category != 0 && status != 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if (site == 0 && category != 0 && status != 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if (site == 0 && category == 0 && status != 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if (site == 0 && category != 0 && status == 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }
            else if (site != 0 && category == 0 && status != 99)
            {
                filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).Skip(skipPage).Take(20).ToList();
                filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
            }

            var data = new List<report>();
            if (User.IsInRole("admin"))
            {
                //
                data = search == "" ? (from s in filter_data_site
                                       orderby s.site_name
                                       select new report()
                                       {
                                           site_name = s.site_name,
                                           site_code = s.code_store,
                                           count_job = filter_data_job.Where(w => w.store_id == s.id && (w.status_job == 3 || w.status_job == 2 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                           all_done_job = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 3).Count(),
                                           all_cancel_job = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 4).Count(),
                                           all_perfer = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 3
                                                                                                         && w.customer_prefer_date != null
                                                                                                         && w.job_end <= w.customer_prefer_date).Count(),
                                           all_delay = filter_data_job.Where(w => w.store_id == s.id 
                                                                                  && w.job_start != null && w.appointment_datetime != null
                                                                                  && w.job_start > w.appointment_datetime).Count(),
                                           all_pending = filter_data_job.Where(w => w.store_id == s.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                           all_sms_customer = filter_data_job.Where(w => w.store_id == s.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                           all_sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.store_id == s.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                           list_engineer = (from en in filter_data_engineer
                                                            where en.site_id == s.id
                                                                  && (from j in filter_data_job
                                                                      where j.engineer_id == en.id
                                                                      select j.id).Count() > 0
                                                            orderby en.engineer_name
                                                            select new report_engineer()
                                                            {
                                                                engineer_name = en.engineer_name,
                                                                engineer_code = en.code_engineer,
                                                                all_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 3 || w.status_job == 2 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                                done_job = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.engineer_id == en.id).Count(),
                                                                cancel_job = filter_data_job.Where(w => w.status_job == 4
                                                                                             && w.engineer_id == en.id).Count(),
                                                                perfer_date = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.customer_prefer_date != null
                                                                                             && w.job_end <= w.customer_prefer_date
                                                                                             && w.engineer_id == en.id).Count(),
                                                                delay_job = filter_data_job.Where(w => w.job_start != null && w.appointment_datetime != null
                                                                                                      && w.job_start > w.appointment_datetime
                                                                                                      && w.engineer_id == en.id).Count(),
                                                                sms_customer = filter_data_job.Where(w => w.engineer_id == en.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                                                sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.engineer_id == en.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                                                peding_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count()
                                                            }).ToList()
                                       }).Skip(skipPage).Take(take).ToList()
                                      :
                                      (from s in filter_data_site
                                       where tem_id.Contains(s.id.ToString())
                                       orderby s.site_name
                                       select new report()
                                       {
                                           site_name = s.site_name,
                                           site_code = s.code_store,
                                           count_job = (from j in filter_data_job where j.store_id == s.id && (j.status_job == 3 || j.status_job == 4 || j.status_job == 0 || j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_done_job = (from j in filter_data_job where j.store_id == s.id && j.status_job == 3 && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_cancel_job = (from j in filter_data_job where j.store_id == s.id && j.status_job == 4 && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_perfer = (from j in filter_data_job where j.store_id == s.id && j.status_job == 3 && j.customer_prefer_date != null && j.job_end != null && j.job_end <= j.customer_prefer_date && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_delay = (from j in filter_data_job where j.store_id == s.id && j.job_start != null && j.appointment_datetime != null && j.job_start > j.appointment_datetime && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_pending = (from j in filter_data_job where j.store_id == s.id && (j.status_job == 0 || j.status_job == 2 || j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_sms_customer = (from j in filter_data_job where j.store_id == s.id && j.date_customer != null && (j.date_customer >= start_date && j.date_customer <= end_date) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                           all_sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.store_id == s.id && j.id == l.job_id && (from e in filter_data_engineer where e.id == j.engineer_id && e.engineer_name.Contains(search.Trim()) select e.id).Count() > 0 select j.id).Count() > 0 select l.id).Count(),
                                           list_engineer = (from en in filter_data_engineer
                                                            where en.site_id == s.id
                                                                  && en.engineer_name.Contains(search.Trim())
                                                                  && (from j in filter_data_job
                                                                      where j.engineer_id == en.id
                                                                      select j.id).Count() > 0
                                                            orderby en.engineer_name
                                                            select new report_engineer()
                                                            {
                                                                engineer_name = en.engineer_name,
                                                                engineer_code = en.code_engineer,
                                                                all_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 3 || w.status_job == 2 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                                done_job = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.engineer_id == en.id).Count(),
                                                                cancel_job = filter_data_job.Where(w => w.status_job == 4
                                                                                             && w.engineer_id == en.id).Count(),
                                                                perfer_date = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.customer_prefer_date != null
                                                                                             && w.job_end <= w.customer_prefer_date
                                                                                             && w.engineer_id == en.id).Count(),
                                                                delay_job = filter_data_job.Where(w => w.job_start != null && w.appointment_datetime != null && w.job_start > w.appointment_datetime
                                                                                             && w.engineer_id == en.id).Count(),
                                                                sms_customer = filter_data_job.Where(w => w.engineer_id == en.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                                                sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.engineer_id == en.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                                                peding_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count()
                                                            }).ToList()
                                       }).Skip(skipPage).Take(take).ToList();

                if (first)
                {
                    ViewData["Count"] = data.Count();

                    ViewData["Data"] = data;
                    return data;
                }
                else
                {
                    if (chart != true)
                    {
                        if (search == "")
                        {
                            data.Add(new report()
                            {
                                site_name = null,
                                count = (from s in filter_data_site
                                         select s.id).Count()
                            });
                        }
                        else
                        {
                            data.Add(new report()
                            {
                                site_name = null,
                                count = (from s in filter_data_site
                                         where tem_id.Contains(s.id.ToString())
                                         select s.id).Count()
                            });
                        }
                    }
                    //

                    string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    return new ContentResult()
                    {
                        Content = jsonString,
                        ContentType = "application/json"
                    };
                }
                //count
            }
            else
            {
                var getName = User.Identity.Name;
                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid.ToString() == CheckUser.ToString()).Select(s => s.site_id).FirstOrDefault();

                //
                data = search == "" ? (from s in filter_data_site
                                       where s.id == idStore
                                       orderby s.site_name
                                       select new report()
                                       {
                                           site_name = s.site_name,
                                           site_code = s.code_store,
                                           count_job = filter_data_job.Where(w => w.store_id == s.id && (w.status_job == 3 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                           all_done_job = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 3).Count(),
                                           all_cancel_job = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 4).Count(),
                                           all_perfer = filter_data_job.Where(w => w.store_id == s.id && w.status_job == 3
                                                                                                         && w.customer_prefer_date != null
                                                                                                         && w.job_end <= w.customer_prefer_date).Count(),
                                           all_delay = filter_data_job.Where(w => w.store_id == s.id
                                                                                  && w.job_start != null && w.appointment_datetime != null
                                                                                  && w.job_start > w.appointment_datetime).Count(),
                                           all_pending = filter_data_job.Where(w => w.store_id == s.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                           all_sms_customer = filter_data_job.Where(w => w.store_id == s.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                           all_sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.store_id == s.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                           list_engineer = (from en in filter_data_engineer
                                                            where en.site_id == s.id
                                                                  && (from j in filter_data_job
                                                                      where j.engineer_id == en.id
                                                                      select j.id).Count() > 0
                                                            orderby en.engineer_name
                                                            select new report_engineer()
                                                            {
                                                                engineer_name = en.engineer_name,
                                                                engineer_code = en.code_engineer,
                                                                all_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 3 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                                done_job = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.engineer_id == en.id).Count(),
                                                                cancel_job = filter_data_job.Where(w => w.status_job == 4
                                                                                             && w.engineer_id == en.id).Count(),
                                                                perfer_date = filter_data_job.Where(w => w.status_job == 3
                                                                                             && w.customer_prefer_date != null
                                                                                             && w.job_end <= w.customer_prefer_date
                                                                                             && w.engineer_id == en.id).Count(),
                                                                delay_job = filter_data_job.Where(w => w.job_start != null && w.appointment_datetime != null && w.job_start > w.appointment_datetime
                                                                                             && w.engineer_id == en.id).Count(),
                                                                sms_customer = filter_data_job.Where(w => w.engineer_id == en.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                                                sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.engineer_id == en.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                                                peding_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count()
                                                            }).ToList()
                                       }).Skip(skipPage).Take(take).ToList()
                                     :
                                     (from s in filter_data_site
                                      where s.id == idStore && tem_id.Contains(idStore.ToString())
                                      orderby s.site_name
                                      select new report()
                                      {
                                          site_name = s.site_name,
                                          site_code = s.code_store,
                                          count_job = (from j in filter_data_job where j.store_id == s.id && (j.status_job == 3 || j.status_job == 4 || j.status_job == 0 || j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_done_job = (from j in filter_data_job where j.store_id == s.id && j.status_job == 3 && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_cancel_job = (from j in filter_data_job where j.store_id == s.id && j.status_job == 4 && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_perfer = (from j in filter_data_job where j.store_id == s.id && j.status_job == 3 && j.customer_prefer_date != null && j.job_end != null && j.job_end <= j.customer_prefer_date && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_delay = (from j in filter_data_job where j.store_id == s.id && j.job_start != null && j.appointment_datetime != null && j.job_start > j.appointment_datetime && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_pending = (from j in filter_data_job where j.store_id == s.id && (j.status_job == 0 || j.status_job == 2 || j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_sms_customer = (from j in filter_data_job where j.store_id == s.id && j.date_customer != null && (j.date_customer >= start_date && j.date_customer <= end_date) && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
                                          all_sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.store_id == s.id && j.id == l.job_id && (from e in filter_data_engineer where e.id == j.engineer_id && e.engineer_name.Contains(search.Trim()) select e.id).Count() > 0 select j.id).Count() > 0 select l.id).Count(),
                                          list_engineer = (from en in filter_data_engineer
                                                           where en.site_id == s.id
                                                                 && en.engineer_name.Contains(search.Trim())
                                                                 && (from j in filter_data_job
                                                                     where j.engineer_id == en.id
                                                                     select j.id).Count() > 0
                                                           orderby en.engineer_name
                                                           select new report_engineer()
                                                           {
                                                               engineer_name = en.engineer_name,
                                                               engineer_code = en.code_engineer,
                                                               all_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 3 || w.status_job == 4 || w.status_job == 0 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                               done_job = filter_data_job.Where(w => w.status_job == 3
                                                                                            && w.engineer_id == en.id).Count(),
                                                               cancel_job = filter_data_job.Where(w => w.status_job == 4
                                                                                            && w.engineer_id == en.id).Count(),
                                                               perfer_date = filter_data_job.Where(w => w.status_job == 3
                                                                                            && w.customer_prefer_date != null
                                                                                            && w.job_end <= w.customer_prefer_date
                                                                                            && w.engineer_id == en.id).Count(),
                                                               delay_job = filter_data_job.Where(w => w.job_start != null && w.appointment_datetime != null && w.job_start > w.appointment_datetime
                                                                                            && w.engineer_id == en.id).Count(),
                                                               sms_customer = filter_data_job.Where(w => w.engineer_id == en.id && w.date_customer != null && (w.date_customer >= start_date && w.date_customer <= end_date)).Count(),
                                                               sms_engineer = (from l in filter_data_logSMS where (from j in filter_data_job where j.engineer_id == en.id && j.id == l.job_id select j.id).Count() > 0 select l.id).Count(),
                                                               peding_job = filter_data_job.Where(w => w.engineer_id == en.id && (w.status_job == 0 || w.status_job == 2 || w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count()
                                                           }).ToList()
                                      }).Skip(skipPage).Take(take).ToList();

                if (first)
                {
                    ViewData["Count"] = data.Count();

                    ViewData["Data"] = data;
                    return data;
                }
                else
                {
                    if (chart != true)
                    {
                        if (search == "")
                        {
                            data.Add(new report()
                            {
                                site_name = null,
                                count = (from s in filter_data_site
                                         where s.id == idStore
                                         select s.id).Count()
                            });
                        }
                        else
                        {
                            data.Add(new report()
                            {
                                site_name = null,
                                count = (from s in filter_data_site
                                         where tem_id.Contains(idStore.ToString())
                                         select s.id).Count()
                            });
                        }
                    }
                    //

                    string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    return new ContentResult()
                    {
                        Content = jsonString,
                        ContentType = "application/json"
                    };
                }
               
            }
        }


    }
}