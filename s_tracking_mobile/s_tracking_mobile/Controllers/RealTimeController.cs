using CommonLib;
using Newtonsoft.Json;
using RestSharp;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class RealTimeController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        // GET: RealTime
        [Authorize]
        public ActionResult index()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);

            var con_start_date = start_date.ToString("dd/MM/yyyy");
            var con_end_date = end_date.ToString("dd/MM/yyyy");

            var getName = User.Identity.Name;
            if (getName != "")
            {
                var data = GetDataJob(0, con_start_date, con_end_date, true, "", 1); //getdata
                var count = Count_job(con_start_date, con_end_date, true, ""); //getcount
                ViewData["Count-Job"] = count;
                ViewData["Data"] = data;

                //string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                //string base64EncodedExternalAccount = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));
                ////jsonString = jsonString.Replace(" ", "+");
                //var de_token = byi_common.encryption.Decrypt(base64EncodedExternalAccount);

                //string en_json = byi_common.encryption.Encrypt(de_token);


                ////test
                //byte[] inputArray = UTF8Encoding.UTF8.GetBytes(jsonString);
                //TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
                //tripleDES.Key = UTF8Encoding.UTF8.GetBytes("sblw-3hn8-sqoy19");
                //tripleDES.Mode = CipherMode.ECB;
                //tripleDES.Padding = PaddingMode.PKCS7;
                //ICryptoTransform cTransform = tripleDES.CreateEncryptor();
                //byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
                //tripleDES.Clear();
                //var test = Convert.ToBase64String(resultArray, 0, resultArray.Length);




                //byte[] inputArray2 = Convert.FromBase64String(test);
                //TripleDESCryptoServiceProvider tripleDES2 = new TripleDESCryptoServiceProvider();
                //tripleDES.Key = UTF8Encoding.UTF8.GetBytes("sblw-3hn8-sqoy19");
                //tripleDES.Mode = CipherMode.ECB;
                //tripleDES.Padding = PaddingMode.PKCS7;
                //ICryptoTransform cTransform2 = tripleDES.CreateDecryptor();
                //byte[] resultArray2 = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
                //tripleDES.Clear();
                //var etst2 = UTF8Encoding.UTF8.GetString(resultArray2);

                //ViewData["DataDecode"] = test;
           }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }

            return View();
        }

        //getdata
        [HttpGet]
        public object GetDataJob(int status, string s_start_date, string s_end_date, bool first, string job_number, int page, int engineer = 0)
        {
            //var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            var limit = 20;

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year , end_date.Month , end_date.Day, 0 ,0 ,0);
            
            //----------------
            List<string> job_num_array = new List<string>();
            string[] multi_number = job_number.Split(',');
            bool first_add = true;

            if (multi_number.Count() > 1)
            {
                foreach (var i in multi_number)
                {
                    List<string> one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    if (first_add)
                    {
                        job_num_array.AddRange(one_job);
                        first_add = false;
                    }
                    else
                    {
                        List<string> check_duplicate = new List<string>();
                        foreach (string j in one_job)
                        {
                            int dupli = 0;
                            foreach (string k in job_num_array)
                            {
                                if (j == k) dupli = 1;
                            }
                            if (dupli == 0) check_duplicate.Add(j);
                        }
                        job_num_array.AddRange(check_duplicate);
                    }
                }
            }
            else
            {
                foreach (var i in multi_number)
                {
                    var one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    job_num_array.AddRange(one_job);
                }
            }
            //----------------
            //get id store 
            var getName = User.Identity.Name;
            var CheckUser = Membership.GetUser(getName).ProviderUserKey;
            Guid convertGuid = new Guid(CheckUser.ToString());
            var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
            ViewBag.id_store = idStore;
            var filter_data_job = new List<CommonLib.tb_jobs>();
            var count_filter_data_job = new List<CommonLib.tb_jobs>();
            var filter_data_shop = new List<CommonLib.tb_store>();

            if (status == 0)
            {
                filter_data_job = (from j in db.tb_jobs
                                   where j.is_delete == 0
                                         && (j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2)
                                         && j.appointment_datetime != null
                                         && j.appointment_to_datetime != null
                                         && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                   select j).ToList();

                count_filter_data_job = (from j in db.tb_jobs
                                        where j.is_delete == 0
                                              && (j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2)
                                              && j.appointment_datetime != null
                                              && j.appointment_to_datetime != null
                                              && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                        select j).ToList();

                filter_data_shop = (from s in db.tb_store
                                    where s.is_delete == 0
                                          && (from j in db.tb_jobs
                                              where j.is_delete == 0
                                                    && j.store_id == s.id
                                                    && (j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2)
                                                    && j.appointment_datetime != null
                                                    && j.appointment_to_datetime != null
                                                    && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                              select j).Count() > 0
                                    orderby s.site_name
                                    select s).ToList();
            }
            else
            {
                filter_data_job = (from j in db.tb_jobs
                                   where j.is_delete == 0
                                         && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                         : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                         : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                         : j.status_job == status)
                                                         && j.appointment_datetime != null
                                                         && j.appointment_to_datetime != null
                                                         && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                   select j).ToList();

                count_filter_data_job = (from j in db.tb_jobs
                                        where j.is_delete == 0
                                              && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                              : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                              : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                              : j.status_job == status)
                                                              && j.appointment_datetime != null
                                                              && j.appointment_to_datetime != null
                                                              && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                        select j).ToList();

                filter_data_shop = (from s in db.tb_store
                                    where s.is_delete == 0
                                            && (from j in db.tb_jobs
                                                where j.is_delete == 0
                                                      && j.store_id == s.id
                                                      && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                                      : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                                      : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                                      : j.status_job == status)
                                                      && j.appointment_datetime != null
                                                      && j.appointment_to_datetime != null
                                                      && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                select j.id).Count() > 0
                                    orderby s.site_name
                                    select s).ToList();
            }

            if(engineer != 0)
            {
                var check_site = 0;
                if (engineer != 0) check_site = db.tb_engineer.Where(w => w.is_delete == 0 && w.id == engineer).Select(s => s.site_id).FirstOrDefault();
                filter_data_job = filter_data_job.Where(w => w.engineer_id == engineer).ToList();
                filter_data_shop = filter_data_shop.Where(w => w.id == check_site).ToList();
            }

            var TemData = new List<realtime>();
            var count_temdata = new List<jobs_realtime>();
            var count_site = 0;

            if (User.IsInRole("admin"))
            {
                var TemData1 = job_number == "" ? (from store in filter_data_shop
                                                   where (from j in filter_data_job
                                                          where j.store_id == store.id
                                                          select j.id).Count() > 0
                                                   orderby store.site_name
                                                   select new realtime()
                                                   {
                                                       store_id = store.id,
                                                       store_guid = store.store_guid,
                                                       store_name = store.site_name,
                                                       store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                                       all_job = filter_data_job.Where(w => w.store_id == store.id).Count(),
                                                       store_contact = store.contact1,
                                                       store_tel = store.tel1,
                                                       count_jobDelay = filter_data_job.Where(w => w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                       store_date_open1 = store.store_opendate1,
                                                       store_to_date_open1 = store.store_to_opendate1,
                                                       store_time_open1 = store.store_opentime1,
                                                       store_to_time_open1 = store.store_to_opentime1,
                                                       store_date_open2 = store.store_opendate2,
                                                       store_to_date_open2 = store.store_to_opendate2,
                                                       store_time_open2 = store.store_opentime2,
                                                       store_to_time_open2 = store.store_to_opentime2,
                                                       store_close = store.store_close,
                                                       jobs = (from jobs in filter_data_job
                                                               where jobs.store_id == store.id
                                                               orderby jobs.appointment_datetime
                                                               select new jobs_realtime()
                                                               {
                                                                   job_id = jobs.id,
                                                                   job_guid = jobs.job_guid,
                                                                   job_number = jobs.service_order_no,
                                                                   en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                                   job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                                   customer_name = jobs.customer_fullname,
                                                                   en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                                   en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                                   app_time = jobs.appointment_datetime,
                                                                   app_to_time = jobs.appointment_to_datetime
                                                               }).ToList()
                                                   })
                               :
                               (from store in filter_data_shop
                                where (from j in filter_data_job
                                       where (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code)) && j.store_id == store.id
                                       select j.id).Count() > 0
                                orderby store.site_name
                                select new realtime()
                                {
                                    store_id = store.id,
                                    store_guid = store.store_guid,
                                    store_name = store.site_name,
                                    store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                    all_job = filter_data_job.Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == store.id).Count(),
                                    store_contact = store.contact1,
                                    store_tel = store.tel1,
                                    count_jobDelay = filter_data_job.Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                    store_date_open1 = store.store_opendate1,
                                    store_to_date_open1 = store.store_to_opendate1,
                                    store_time_open1 = store.store_opentime1,
                                    store_to_time_open1 = store.store_to_opentime1,
                                    store_date_open2 = store.store_opendate2,
                                    store_to_date_open2 = store.store_to_opendate2,
                                    store_time_open2 = store.store_opentime2,
                                    store_to_time_open2 = store.store_to_opentime2,
                                    store_close = store.store_close,
                                    jobs = (from jobs in filter_data_job
                                            where jobs.store_id == store.id
                                                  && (job_num_array.Contains(jobs.service_order_no) || job_num_array.Contains(jobs.engineer_code))
                                            orderby jobs.appointment_datetime
                                            select new jobs_realtime()
                                            {
                                                job_id = jobs.id,
                                                job_guid = jobs.job_guid,
                                                job_number = jobs.service_order_no,
                                                en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                customer_name = jobs.customer_fullname,
                                                en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                app_time = jobs.appointment_datetime,
                                                app_to_time = jobs.appointment_to_datetime
                                            }).ToList()
                                });

                var datatest = TemData1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                TemData = TemData1.OrderByDescending(w => w.store_name).Skip(skipPage).Take(limit).ToList();

                count_temdata = job_number == "" ? (from jobs in count_filter_data_job
                                                       orderby jobs.appointment_datetime
                                                       select new jobs_realtime()
                                                       {
                                                           job_id = jobs.id,
                                                           job_guid = jobs.job_guid,
                                                           job_number = jobs.service_order_no,
                                                           en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                           job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                           customer_name = jobs.customer_fullname,
                                                           en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                           en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                           app_time = jobs.appointment_datetime,
                                                           app_to_time = jobs.appointment_to_datetime
                                                       }).ToList()
                               :
                              (from jobs in count_filter_data_job
                               where (job_num_array.Contains(jobs.service_order_no) || job_num_array.Contains(jobs.engineer_code))
                               orderby jobs.appointment_datetime
                               select new jobs_realtime()
                               {
                                   job_id = jobs.id,
                                   job_guid = jobs.job_guid,
                                   job_number = jobs.service_order_no,
                                   en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                   job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                   customer_name = jobs.customer_fullname,
                                   en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                   en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                   app_time = jobs.appointment_datetime,
                                   app_to_time = jobs.appointment_to_datetime
                               }).ToList();

                count_site = job_number == "" ? (from store in filter_data_shop where (from j in filter_data_job where j.store_id == store.id select j.id).Count() > 0 select store.id ).Count()
                                              : (from store in filter_data_shop where (from j in filter_data_job where (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code)) && j.store_id == store.id select j.id).Count() > 0 select store).Count();
            }
            else
            {
                var TemData1 = job_number == "" ? (from store in filter_data_shop
                                                   where store.id == idStore
                                                           && (from j in filter_data_job
                                                               where j.store_id == store.id
                                                               select j.id).Count() > 0
                                                   orderby store.site_name
                                                   select new realtime()
                                                   {
                                                       store_id = store.id,
                                                       store_guid = store.store_guid,
                                                       store_name = store.site_name,
                                                       store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                                       all_job = filter_data_job.Where(w => w.store_id == store.id).Count(),
                                                       store_contact = store.contact1,
                                                       store_tel = store.tel1,
                                                       count_jobDelay = filter_data_job.Where(w => w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                       store_date_open1 = store.store_opendate1,
                                                       store_to_date_open1 = store.store_to_opendate1,
                                                       store_time_open1 = store.store_opendate1,
                                                       store_to_time_open1 = store.store_to_opentime1,
                                                       store_date_open2 = store.store_opendate2,
                                                       store_to_date_open2 = store.store_to_opendate2,
                                                       store_time_open2 = store.store_opentime2,
                                                       store_to_time_open2 = store.store_to_opentime2,
                                                       store_close = store.store_close,
                                                       jobs = (from jobs in filter_data_job
                                                               where jobs.store_id == store.id
                                                               orderby jobs.appointment_datetime
                                                               select new jobs_realtime()
                                                               {
                                                                   job_id = jobs.id,
                                                                   job_guid = jobs.job_guid,
                                                                   job_number = jobs.service_order_no,
                                                                   en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                                   job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                                   customer_name = jobs.customer_fullname,
                                                                   en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                                   en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                                   app_time = jobs.appointment_datetime,
                                                                   app_to_time = jobs.appointment_to_datetime
                                                               }).ToList()
                                                   })
                                                   :
                                                   (from store in filter_data_shop
                                                    where store.id == idStore
                                                            && (from j in filter_data_job
                                                                where (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                                      && j.store_id == store.id
                                                                select j.id).Count() > 0
                                                    orderby store.site_name
                                                    select new realtime()
                                                    {
                                                        store_id = store.id,
                                                        store_guid = store.store_guid,
                                                        store_name = store.site_name,
                                                        store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                                        all_job = filter_data_job.Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == store.id).Count(),
                                                        store_contact = store.contact1,
                                                        store_tel = store.tel1,
                                                        count_jobDelay = filter_data_job.Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                                        store_date_open1 = store.store_opendate1,
                                                        store_to_date_open1 = store.store_to_opendate1,
                                                        store_time_open1 = store.store_opendate1,
                                                        store_to_time_open1 = store.store_to_opentime1,
                                                        store_date_open2 = store.store_opendate2,
                                                        store_to_date_open2 = store.store_to_opendate2,
                                                        store_time_open2 = store.store_opentime2,
                                                        store_to_time_open2 = store.store_to_opentime2,
                                                        store_close = store.store_close,
                                                        jobs = (from jobs in filter_data_job
                                                                where jobs.store_id == store.id
                                                                      && (job_num_array.Contains(jobs.service_order_no) || job_num_array.Contains(jobs.engineer_code))
                                                                orderby jobs.appointment_datetime
                                                                select new jobs_realtime()
                                                                {
                                                                    job_id = jobs.id,
                                                                    job_guid = jobs.job_guid,
                                                                    job_number = jobs.service_order_no,
                                                                    en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                                    job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                                    customer_name = jobs.customer_fullname,
                                                                    en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                                    en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                                    app_time = jobs.appointment_datetime,
                                                                    app_to_time = jobs.appointment_to_datetime
                                                                }).ToList()
                                                    });

                var datatest = TemData1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                TemData = TemData1.OrderByDescending(w => w.store_name).Skip(skipPage).Take(limit).ToList();

                count_temdata = job_number == "" ? (from jobs in count_filter_data_job
                                                       where jobs.store_id == idStore
                                                       orderby jobs.appointment_datetime
                                                       select new jobs_realtime()
                                                       {
                                                           job_id = jobs.id,
                                                           job_guid = jobs.job_guid,
                                                           job_number = jobs.service_order_no,
                                                           en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                           job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                           customer_name = jobs.customer_fullname,
                                                           en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                           en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                           app_time = jobs.appointment_datetime,
                                                           app_to_time = jobs.appointment_to_datetime
                                                       }).ToList()
                               :
                              (from jobs in count_filter_data_job
                               where jobs.store_id == idStore
                                     && (job_num_array.Contains(jobs.service_order_no) || job_num_array.Contains(jobs.engineer_code))
                               orderby jobs.appointment_datetime
                               select new jobs_realtime()
                               {
                                   job_id = jobs.id,
                                   job_guid = jobs.job_guid,
                                   job_number = jobs.service_order_no,
                                   en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                   job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                   customer_name = jobs.customer_fullname,
                                   en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                   en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                   app_time = jobs.appointment_datetime,
                                   app_to_time = jobs.appointment_to_datetime
                               }).ToList();

                count_site = job_number == "" ? (from store in filter_data_shop where store.id == idStore && (from j in filter_data_job where j.store_id == store.id select j.id).Count() > 0 select store.id).Count()
                                              : (from store in filter_data_shop where store.id == idStore && (from j in filter_data_job where (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code)) && j.store_id == store.id select j.id).Count() > 0 select store.id).Count();
            }

            var all_engineer = new List<getEngineer>();
            var check_engineer = "";
            foreach (var job in (List<jobs_realtime>)count_temdata)
            {
                if(job.en_code != null)
                {
                    if (check_engineer.IndexOf(job.en_code) == -1)
                    {
                        //var db_engineer = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer.ToString() == job.en_code.ToString()).FirstOrDefault();
                        var db_engineer2 = (from e in db.tb_engineer where e.is_delete == 0 && e.code_engineer.ToString() == job.en_code.ToString() select new getEngineer() { id = e.id, name = e.engineer_name, site_id = e.site_id, code = e.code_engineer }).FirstOrDefault();
                        all_engineer.Add(new getEngineer()
                        {
                            id = db_engineer2.id,
                            name = db_engineer2.name,
                            site_id = db_engineer2.site_id,
                            code = db_engineer2.code
                        });
                        check_engineer += check_engineer == "" ? job.en_code.ToString() : "," + job.en_code.ToString();
                    }
                }
            }

            ViewData["all_Engineer"] = all_engineer;
            if (first) {
                ViewData["pagination"] = count_site;
                return TemData;
            }
            else
            {
                
                TemData.Add(new realtime()
                {
                    store_id = null,
                    engineer = all_engineer
                });

                TemData.Add(new realtime()
                {
                    store_id = null,
                    count = count_site
                });


                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(TemData);
                return new ContentResult()
                {
                    Content = jsonString,
                    ContentType = "application/json"
                };
            }
        }

        //countdata
        [HttpGet]
        public object Count_job(string s_start_date, string s_end_date, bool first, string job_number , int engineer = 0)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);
            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);
            //----------------
            List<string> job_num_array = new List<string>();
            string[] multi_number = job_number.Split(',');
            bool first_add = true;

            if (multi_number.Count() > 1)
            {
                foreach (var i in multi_number)
                {
                    List<string> one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    if (first_add)
                    {
                        job_num_array.AddRange(one_job);
                        first_add = false;
                    }
                    else
                    {
                        List<string> check_duplicate = new List<string>();
                        foreach (string j in one_job)
                        {
                            int dupli = 0;
                            foreach (string k in job_num_array)
                            {
                                if (j == k)
                                {
                                    dupli = 1;
                                }
                            }
                            if (dupli == 0)
                            {
                                check_duplicate.Add(j);
                            }
                        }
                        job_num_array.AddRange(check_duplicate);
                    }
                }
            }
            else
            {
                foreach (var i in multi_number)
                {
                    var one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    job_num_array.AddRange(one_job);
                }
            }
            //----------------
            //get id store
            var getName = User.Identity.Name;

            var CheckUser = Membership.GetUser(getName).ProviderUserKey;
            Guid convertGuid = new Guid(CheckUser.ToString());
            var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

            var filter_data_job = new List<CommonLib.tb_jobs>();
            filter_data_job = (from j in db.tb_jobs
                               where j.is_delete == 0
                                     && j.appointment_datetime != null
                                     && j.appointment_to_datetime != null
                                     && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                               select j).ToList();

            if(engineer != 0)
            {
                filter_data_job = filter_data_job.Where(w => w.engineer_id == engineer).ToList();
            }

            var count = new List<countJob>();
            if (User.IsInRole("admin"))
            {
                if(job_number == "")
                {
                    count.Add(new countJob()
                    {
                        all_job = filter_data_job
                                .Where(w => (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                        repair = filter_data_job
                            .Where(w => (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        pending = filter_data_job
                            .Where(w => w.status_job == 2).Count(),
                        delay_job = filter_data_job
                            .Where(w => (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        completed = filter_data_job
                            .Where(w => w.status_job == 3).Count(),
                        cancel = filter_data_job
                            .Where(w => (w.status_job == 4)).Count(),
                        waiting = filter_data_job
                            .Where(w => w.status_job == 5).Count()
                    });
                }
                else
                {
                    count.Add(new countJob()
                    {
                        all_job = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                        repair = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        pending = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.status_job == 2).Count(),
                        delay_job = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        completed = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.status_job == 3).Count(),
                        cancel = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && (w.status_job == 4)).Count(),
                        waiting = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.status_job == 5).Count()
                    });
                }
            }
            else
            {
                if (job_number == "")
                {
                    count.Add(new countJob()
                    {
                        all_job = filter_data_job
                            .Where(w => w.store_id == idStore && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                        repair = filter_data_job
                            .Where(w => w.store_id == idStore && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        pending = filter_data_job
                            .Where(w => w.store_id == idStore && w.status_job == 2).Count(),
                        delay_job = filter_data_job
                            .Where(w => w.store_id == idStore && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        completed = filter_data_job
                            .Where(w => w.store_id == idStore && w.status_job == 3).Count(),
                        cancel = filter_data_job
                            .Where(w => w.store_id == idStore && (w.status_job == 4)).Count(),
                        waiting = filter_data_job
                            .Where(w => w.store_id == idStore && w.status_job == 5).Count()
                    });
                }
                else
                {
                    count.Add(new countJob()
                    {
                        all_job = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                        repair = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        pending = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && w.status_job == 2).Count(),
                        delay_job = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                        completed = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && w.status_job == 3).Count(),
                        cancel = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && (w.status_job == 4)).Count(),
                        waiting = filter_data_job
                            .Where(w => (job_num_array.Contains(w.service_order_no) || job_num_array.Contains(w.engineer_code)) && w.store_id == idStore && w.status_job == 5).Count()
                    });
                }
            }

            if (first) {
                return count;
            }
            else
            {
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(count);

                return new ContentResult()
                {
                    Content = jsonString,
                    ContentType = "application/json"
                };
            }
        }

        [HttpGet]
        public object DataMap(string s_start_date, string s_end_date, int status, string job_number, int engineer = 0)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);
            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);
            //----------------
            List<string> job_num_array = new List<string>();
            string[] multi_number = job_number.Split(',');
            bool first_add = true;

            if (multi_number.Count() > 1)
            {
                foreach (var i in multi_number)
                {
                    List<string> one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    if (first_add)
                    {
                        job_num_array.AddRange(one_job);
                        first_add = false;
                    }
                    else
                    {
                        List<string> check_duplicate = new List<string>();
                        foreach (string j in one_job)
                        {
                            int dupli = 0;
                            foreach (string k in job_num_array)
                            {
                                if (j == k)
                                {
                                    dupli = 1;
                                }
                            }
                            if (dupli == 0)
                            {
                                check_duplicate.Add(j);
                            }
                        }
                        job_num_array.AddRange(check_duplicate);
                    }
                }
            }
            else
            {
                foreach (var i in multi_number)
                {
                    var one_job = db.tb_jobs.Where(w => w.service_order_no.Contains(i.Trim()) || w.engineer_code.Contains(i.Trim()) && w.appointment_datetime != null && w.appointment_datetime >= start_date && w.appointment_datetime <= end_date).Select(s => s.service_order_no).ToList();
                    job_num_array.AddRange(one_job);
                }
            }
            //----------------

            //get id store 
            var getName = User.Identity.Name;
            var CheckUser = Membership.GetUser(getName).ProviderUserKey;
            Guid convertGuid = new Guid(CheckUser.ToString());
            var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

            var filter_data_job = new List<CommonLib.tb_jobs>();
            var filter_data_shop = new List<CommonLib.tb_store>();
            var filter_data_engineer = new List<CommonLib.tb_engineer>();

            var count_filter_data_engineer = new List<CommonLib.tb_engineer>();


            if (status == 0)
            {
                filter_data_job = (from j in db.tb_jobs
                                       where j.is_delete == 0
                                         && j.appointment_datetime != null
                                         && j.appointment_to_datetime != null
                                         && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                       select j).ToList();

                filter_data_shop = (from s in db.tb_store
                                        where s.is_delete == 0
                                              && (from j in db.tb_jobs
                                                  where j.store_id == s.id
                                                             && j.appointment_datetime != null
                                                             && j.appointment_to_datetime != null
                                                             && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                  select j.store_id).Count() > 0
                                        select s).ToList();

                filter_data_engineer = (from e in db.tb_engineer
                                            where e.is_delete == 0
                                                  && (from j in db.tb_jobs
                                                      where j.is_delete == 0
                                                             && j.engineer_id == e.id
                                                             && j.appointment_datetime != null
                                                             && j.appointment_to_datetime != null
                                                             && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                      select j.store_id).Count() > 0
                                            select e).ToList();

                count_filter_data_engineer = (from e in db.tb_engineer
                                              where e.is_delete == 0
                                                    && (from j in db.tb_jobs
                                                        where j.is_delete == 0
                                                               && j.engineer_id == e.id
                                                               && j.appointment_datetime != null
                                                               && j.appointment_to_datetime != null
                                                               && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                        select j.store_id).Count() > 0
                                              select e).ToList();
            }
            else
            {

                filter_data_job = (from j in db.tb_jobs
                                   where j.is_delete == 0
                                          && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                         : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                         //: status == 2 ? j.status_job == 2
                                                         //: status == 4 ? j.status_job == 4 
                                                         : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                         : j.status_job == status)
                                          && j.appointment_datetime != null
                                          && j.appointment_to_datetime != null
                                          && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                   select j).ToList();

                filter_data_shop = (from s in db.tb_store
                                    where s.is_delete == 0
                                          && (from j in db.tb_jobs
                                              where j.is_delete == 0
                                                     && j.store_id == s.id
                                                     && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                         : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                         //: status == 2 ? j.status_job == 2
                                                         //: status == 4 ? j.status_job == 4 
                                                         : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11 
                                                         : j.status_job == status)
                                                     && j.appointment_datetime != null
                                                     && j.appointment_to_datetime != null
                                                     && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                              select j.store_id).Count() > 0
                                    select s).ToList();

                filter_data_engineer = (from e in db.tb_engineer
                                        where e.is_delete == 0
                                              && (from j in db.tb_jobs
                                                  where j.is_delete == 0
                                                         && j.engineer_id == e.id
                                                         && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                             : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                             //: status == 2 ? j.status_job == 2
                                                             //: status == 4 ? j.status_job == 4
                                                             : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                             : j.status_job == status)
                                                         && j.appointment_datetime != null
                                                         && j.appointment_to_datetime != null
                                                         && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                  select j.store_id).Count() > 0
                                        select e).ToList();

                count_filter_data_engineer = (from e in db.tb_engineer
                                              where e.is_delete == 0
                                                    && (from j in db.tb_jobs
                                                        where j.is_delete == 0
                                                               && j.engineer_id == e.id
                                                               && (status == 1 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                                   : status == 0 ? j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2
                                                                   //: status == 2 ? j.status_job == 2
                                                                   //: status == 4 ? j.status_job == 4
                                                                   : status == 6 ? j.status_job == 9 || j.status_job == 10 || j.status_job == 11
                                                                   : j.status_job == status)
                                                               && j.appointment_datetime != null
                                                               && j.appointment_to_datetime != null
                                                               && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                                        select j.store_id).Count() > 0
                                              select e).ToList();
            }

            if(engineer != 0)
            {
                var check_site = 0;
                check_site = db.tb_engineer.Where(w => w.is_delete == 0 && w.id == engineer).Select(s => s.site_id).FirstOrDefault();
                filter_data_job = filter_data_job.Where(w => w.engineer_id == engineer).ToList();
                filter_data_shop = filter_data_shop.Where(w => w.id == check_site).ToList();
                filter_data_engineer = filter_data_engineer.Where(w => w.id == engineer).ToList();
            }

            var count_temdata = new List<map_engineer>();
            var data = new List<map_data>();

            if (User.IsInRole("admin"))
            {
                data = job_number == "" ? (from s in filter_data_shop
                                           where s.is_delete == 0 && (from j in filter_data_job
                                                                      where j.store_id == s.id
                                                                      select j.id).Count() > 0
                                           orderby s.site_name
                                           select new map_data()
                                           {
                                               engineer = (from e in filter_data_engineer
                                                           where e.is_delete == 0 && e.site_id == s.id
                                                           select new map_engineer()
                                                           {
                                                               en_id = e.id,
                                                               en_guid = e.en_guid,
                                                               en_code = e.code_engineer,
                                                               en_name = e.engineer_name,
                                                               en_site = s.site_name.ToString(),
                                                               en_tel = e.tel1,
                                                               en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                                               en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                                               en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                                               en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                                               en_lat = e.lag,
                                                               en_long = e.@long,
                                                               jobs = (from j in filter_data_job
                                                                       where j.engineer_id == e.id
                                                                       orderby j.appointment_datetime
                                                                       select new map_job()
                                                                       {
                                                                           job_id = j.id,
                                                                           job_guid = j.job_guid,
                                                                           job_number = j.service_order_no,
                                                                           job_status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                                           job_status_int = j.status_job,
                                                                           cus_lat = j.customer_lat,
                                                                           cus_long = j.customer_long,
                                                                           cus_name = j.customer_fullname,
                                                                           assets = (from c in db.tb_jobsl_category where c.id == j.sub_category_id select c.name).FirstOrDefault(),
                                                                           cus_tel = j.phone_home,
                                                                           startdate = j.appointment_datetime,
                                                                           enddate = j.appointment_to_datetime
                                                                       }).ToList()
                                                           }).ToList()
                                           }).ToList()
                        :
                        (from s in filter_data_shop
                         where s.is_delete == 0 && (from j in filter_data_job
                                                    where j.store_id == s.id 
                                                          && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                    select j.id).Count() > 0
                         orderby s.site_name
                         select new map_data()
                         {
                             engineer = (from e in filter_data_engineer
                                         where e.is_delete == 0 && e.site_id == s.id && (from j in filter_data_job
                                                                                         where j.engineer_id == e.id
                                                                                               && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                                                         select j.id).Count() > 0
                                         select new map_engineer()
                                         {
                                             en_id = e.id,
                                             en_guid = e.en_guid,
                                             en_code = e.code_engineer,
                                             en_name = e.engineer_name,
                                             en_site = s.site_name.ToString(),
                                             en_tel = e.tel1,
                                             en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                             en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                             en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                             en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                             en_lat = e.lag,
                                             en_long = e.@long,
                                             jobs = (from j in filter_data_job
                                                     where j.engineer_id == e.id
                                                     orderby j.appointment_datetime
                                                     select new map_job()
                                                     {
                                                         job_id = j.id,
                                                         job_guid = j.job_guid,
                                                         job_number = j.service_order_no,
                                                         job_status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                         job_status_int = j.status_job,
                                                         cus_lat = j.customer_lat,
                                                         cus_long = j.customer_long,
                                                         cus_name = j.customer_fullname,
                                                         assets = (from c in db.tb_jobsl_category where c.id == j.sub_category_id select c.name).FirstOrDefault(),
                                                         cus_tel = j.phone_home,
                                                         startdate = j.appointment_datetime,
                                                         enddate = j.appointment_to_datetime
                                                     }).ToList()
                                         }).ToList()
                         }).ToList();

                count_temdata = job_number == "" ? (from e in count_filter_data_engineer
                                                    where e.is_delete == 0
                                                    select new map_engineer()
                                                    {
                                                        en_id = e.id,
                                                        en_guid = e.en_guid,
                                                        en_code = e.code_engineer,
                                                        en_name = e.engineer_name,
                                                        en_tel = e.tel1,
                                                        en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                                        en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                                        en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                                        en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                                        en_lat = e.lag,
                                                        en_long = e.@long,
                                                    }).ToList()
                        :
                       (from e in count_filter_data_engineer
                        where e.is_delete == 0 && (from j in filter_data_job
                                                   where j.engineer_id == e.id
                                                         && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                   select j.id).Count() > 0
                        select new map_engineer()
                        {
                            en_id = e.id,
                            en_guid = e.en_guid,
                            en_code = e.code_engineer,
                            en_name = e.engineer_name,
                            en_tel = e.tel1,
                            en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                            en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                            en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                            en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                            en_lat = e.lag,
                            en_long = e.@long,
                        }).ToList();
            }
            else
            {
                data = job_number == "" ? (from s in filter_data_shop
                                           where s.is_delete == 0
                                                 && s.id == idStore
                                                 && (from j in filter_data_job
                                                     where j.store_id == s.id
                                                     select j.id).Count() > 0
                                           orderby s.site_name
                                           select new map_data()
                                           {
                                               engineer = (from e in filter_data_engineer
                                                           where e.is_delete == 0 && e.site_id == s.id
                                                           select new map_engineer()
                                                           {
                                                               en_id = e.id,
                                                               en_guid = e.en_guid,
                                                               en_code = e.code_engineer,
                                                               en_name = e.engineer_name,
                                                               en_site = s.site_name.ToString(),
                                                               en_tel = e.tel1,
                                                               en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                                               en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                                               en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                                               en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                                               en_lat = e.lag,
                                                               en_long = e.@long,
                                                               jobs = (from j in filter_data_job
                                                                       where j.engineer_id == e.id
                                                                       orderby j.appointment_datetime
                                                                       select new map_job()
                                                                       {
                                                                           job_id = j.id,
                                                                           job_guid = j.job_guid,
                                                                           job_number = j.service_order_no,
                                                                           job_status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                                           job_status_int = j.status_job,
                                                                           cus_lat = j.customer_lat,
                                                                           cus_long = j.customer_long,
                                                                           cus_name = j.customer_fullname,
                                                                           assets = (from c in db.tb_jobsl_category where c.id == j.sub_category_id select c.name).FirstOrDefault(),
                                                                           cus_tel = j.phone_home,
                                                                           startdate = j.appointment_datetime,
                                                                           enddate = j.appointment_to_datetime
                                                                       }).ToList()
                                                           }).ToList()
                                           }).ToList()
                        :
                        (from s in filter_data_shop
                         where s.is_delete == 0
                               && s.id == idStore
                               && (from j in filter_data_job
                                   where j.store_id == s.id
                                         && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                   select j.id).Count() > 0
                         orderby s.site_name
                         select new map_data()
                         {
                             engineer = (from e in filter_data_engineer
                                         where e.is_delete == 0 && e.site_id == s.id && (from j in filter_data_job
                                                                                            where j.engineer_id == e.id
                                                                                                  && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                                                            select j.id).Count() > 0
                                         select new map_engineer()
                                         {
                                             en_id = e.id,
                                             en_guid = e.en_guid,
                                             en_code = e.code_engineer,
                                             en_name = e.engineer_name,
                                             en_site = s.site_name.ToString(),
                                             en_tel = e.tel1,
                                             en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                             en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                             en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                             en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                             en_lat = e.lag,
                                             en_long = e.@long,
                                             jobs = (from j in filter_data_job
                                                     where j.engineer_id == e.id
                                                     orderby j.appointment_datetime
                                                     select new map_job()
                                                     {
                                                         job_id = j.id,
                                                         job_guid = j.job_guid,
                                                         job_number = j.service_order_no,
                                                         job_status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                         job_status_int = j.status_job,
                                                         cus_lat = j.customer_lat,
                                                         cus_long = j.customer_long,
                                                         cus_name = j.customer_fullname,
                                                         assets = (from c in db.tb_jobsl_category where c.id == j.sub_category_id select c.name).FirstOrDefault(),
                                                         cus_tel = j.phone_home,
                                                         startdate = j.appointment_datetime,
                                                         enddate = j.appointment_to_datetime
                                                     }).ToList()
                                         }).ToList()
                         }).ToList();

                count_temdata = job_number == "" ? (from e in count_filter_data_engineer
                                                    where e.is_delete == 0
                                                    select new map_engineer()
                                                    {
                                                        en_id = e.id,
                                                        en_guid = e.en_guid,
                                                        en_code = e.code_engineer,
                                                        en_name = e.engineer_name,
                                                        en_tel = e.tel1,
                                                        en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                                                        en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                                                        en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                                                        en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                                                        en_lat = e.lag,
                                                        en_long = e.@long,

                                                    }).ToList()
                        :
                        (from e in count_filter_data_engineer
                         where e.is_delete == 0 && (from j in filter_data_job
                                                    where j.engineer_id == e.id
                                                          && (job_num_array.Contains(j.service_order_no) || job_num_array.Contains(j.engineer_code))
                                                    select j.id).Count() > 0
                         select new map_engineer()
                         {
                             en_id = e.id,
                             en_guid = e.en_guid,
                             en_code = e.code_engineer,
                             en_name = e.engineer_name,
                             en_tel = e.tel1,
                             en_count_alljob = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 6 || w.status_job == 7 || w.status_job == 8 || w.status_job == 9 || w.status_job == 10 || w.status_job == 11 || w.status_job == 3 || w.status_job == 4 || w.status_job == 2)).Count(),
                             en_count_pending = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 2).Count(),
                             en_count_completed = filter_data_job.Where(w => w.engineer_id == e.id && w.status_job == 3).Count(),
                             en_count_cencel = filter_data_job.Where(w => w.engineer_id == e.id && (w.status_job == 4)).Count(),
                             en_lat = e.lag,
                             en_long = e.@long,

                         }).ToList();
            }

            var all_engineer = new List<CommonLib.tb_engineer>();
            var check_engineer = "";
            foreach (var job in (List<map_engineer>)count_temdata)
            {
                if(job.en_code != null)
                {
                    if (check_engineer.IndexOf(job.en_code) == -1)
                    {
                        var db_engineer = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer.ToString() == job.en_code.ToString()).FirstOrDefault();
                        all_engineer.Add(db_engineer);
                        check_engineer += check_engineer == "" ? job.en_code.ToString() : "," + job.en_code.ToString();
                    }
                }
            }

            data.Add(new map_data()
            {
                list_engineer = all_engineer
            });

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [HttpPost]
        public object getdate_ClickStatus()
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date.AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            var skipPage = 0;
            var limit = 20;

            //get id store 
            var getName = User.Identity.Name;
            var CheckUser = Membership.GetUser(getName).ProviderUserKey;
            Guid convertGuid = new Guid(CheckUser.ToString());
            var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

            var filter_data_job = new List<CommonLib.tb_jobs>();
            var filter_data_shop = new List<CommonLib.tb_store>();
            var TemData = new List<realtime>();

            filter_data_job = (from j in db.tb_jobs
                               where j.is_delete == 0
                                     && (j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2)
                                     && j.appointment_datetime != null
                                     && j.appointment_to_datetime != null
                                     && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                               select j).ToList();

            filter_data_shop = (from s in db.tb_store
                                where s.is_delete == 0
                                      && (from j in db.tb_jobs
                                          where j.is_delete == 0
                                                && j.store_id == s.id
                                                && (j.status_job == 6 || j.status_job == 7 || j.status_job == 8 || j.status_job == 9 || j.status_job == 10 || j.status_job == 11 || j.status_job == 3 || j.status_job == 4 || j.status_job == 2)
                                                && j.appointment_datetime != null
                                                && j.appointment_to_datetime != null
                                                && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date)
                                          select j).Count() > 0
                                orderby s.site_name
                                select s).ToList();

            if (User.IsInRole("admin"))
            {
                var TemData1 = (from store in filter_data_shop
                                where (from j in filter_data_job
                                       where j.store_id == store.id
                                       select j.id).Count() > 0
                                orderby store.site_name
                                select new realtime()
                                {
                                    store_id = store.id,
                                    store_guid = store.store_guid,
                                    store_name = store.site_name,
                                    store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                    all_job = filter_data_job.Where(w => w.store_id == store.id).Count(),
                                    store_contact = store.contact1,
                                    store_tel = store.tel1,
                                    count_jobDelay = filter_data_job.Where(w => w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                    store_date_open1 = store.store_opendate1,
                                    store_to_date_open1 = store.store_to_opendate1,
                                    store_time_open1 = store.store_opentime1,
                                    store_to_time_open1 = store.store_to_opentime1,
                                    store_date_open2 = store.store_opendate2,
                                    store_to_date_open2 = store.store_to_opendate2,
                                    store_time_open2 = store.store_opentime2,
                                    store_to_time_open2 = store.store_to_opentime2,
                                    store_close = store.store_close,
                                    jobs = (from jobs in filter_data_job
                                            where jobs.store_id == store.id
                                            orderby jobs.appointment_datetime
                                            select new jobs_realtime()
                                            {
                                                job_id = jobs.id,
                                                job_guid = jobs.job_guid,
                                                job_number = jobs.service_order_no,
                                                en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                customer_name = jobs.customer_fullname,
                                                en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                app_time = jobs.appointment_datetime,
                                                app_to_time = jobs.appointment_to_datetime
                                            }).ToList()
                                });

                var datatest = TemData1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                TemData = TemData1.OrderByDescending(w => w.store_name).Skip(skipPage).Take(limit).ToList();
            }
            else
            {
                var TemData1 = (from store in filter_data_shop
                                where store.id == idStore
                                        && (from j in filter_data_job
                                            where j.store_id == store.id
                                            select j.id).Count() > 0
                                orderby store.site_name
                                select new realtime()
                                {
                                    store_id = store.id,
                                    store_guid = store.store_guid,
                                    store_name = store.site_name,
                                    store_province = db.tb_provinces.Where(w => w.province_id == store.province).Select(s => s.province_name).FirstOrDefault(),
                                    all_job = filter_data_job.Where(w => w.store_id == store.id).Count(),
                                    store_contact = store.contact1,
                                    store_tel = store.tel1,
                                    count_jobDelay = filter_data_job.Where(w => w.store_id == store.id && (w.status_job == 9 || w.status_job == 10 || w.status_job == 11)).Count(),
                                    store_date_open1 = store.store_opendate1,
                                    store_to_date_open1 = store.store_to_opendate1,
                                    store_time_open1 = store.store_opendate1,
                                    store_to_time_open1 = store.store_to_opentime1,
                                    store_date_open2 = store.store_opendate2,
                                    store_to_date_open2 = store.store_to_opendate2,
                                    store_time_open2 = store.store_opentime2,
                                    store_to_time_open2 = store.store_to_opentime2,
                                    store_close = store.store_close,
                                    jobs = (from jobs in filter_data_job
                                            where jobs.store_id == store.id
                                            orderby jobs.appointment_datetime
                                            select new jobs_realtime()
                                            {
                                                job_id = jobs.id,
                                                job_guid = jobs.job_guid,
                                                job_number = jobs.service_order_no,
                                                en_name = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.engineer_name).FirstOrDefault(),
                                                job_status = ((CommonLib.Status_job)jobs.status_job).ToString(),
                                                customer_name = jobs.customer_fullname,
                                                en_code = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.code_engineer).FirstOrDefault(),
                                                en_id = (from e in db.tb_engineer where e.is_delete == 0 && e.id == jobs.engineer_id select e.id).FirstOrDefault(),
                                                app_time = jobs.appointment_datetime,
                                                app_to_time = jobs.appointment_to_datetime
                                            }).ToList()
                                });

                var datatest = TemData1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                TemData = TemData1.OrderByDescending(w => w.store_name).Skip(skipPage).Take(limit).ToList();
            }

            //var data = GetDataJob(0, con_start_date, con_end_date, true, "", 1); //getdata
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(TemData);

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

    }
}