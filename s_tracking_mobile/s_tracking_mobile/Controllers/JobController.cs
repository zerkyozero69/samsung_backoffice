using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using CommonLib;
using OfficeOpenXml;
using System.Data.Entity.Migrations;
using System.Web.Security;
using byi_common;
using System.Net.Http;
using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Text.RegularExpressions;

namespace s_tracking_mobile.Controllers
{
    public class JobController : Controller
    {
        // GET: Job
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public void GetDataFromDb()
        {
            ViewBag.name = User.Identity.Name;
            ViewBag.roleUser = User.IsInRole("admin"); //get tole

            if (ViewBag.name != "" && ViewBag.name != null)
            {
                var CheckUser = Membership.GetUser(ViewBag.name).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = ViewBag.roleUser != true ? db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault() : 0;

                ViewBag.id_store = idStore;
            }
        }

        [Authorize]
        public ActionResult all()
        {
            GetDataFromDb();
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(-7).Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(1).Date, zone);
            var data = get_allData(1);
            ViewData["all-Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
            var get_engineer = (from e in db.tb_engineer where e.is_delete == 0 select new getEngineer() { id = e.id, name = e.engineer_name, site_id = e.site_id }).ToList();
            ViewData["all-Engineer"] = get_engineer;
            return View();
        }

        [HttpGet]
        public object GetFilterEngineer()
        {

            var get_engineer = (from e in db.tb_engineer where e.is_delete == 0 select new getEngineer() { id = e.id, name = e.engineer_name, tel1 = e.tel1, tel2 = e.tel2, tel3 = e.tel3, repair_type_info1 = e.repair_type_info1, repair_type_info2 = e.repair_type_info2, repair_type_info3 = e.repair_type_info3, site_id = e.site_id }).ToList();
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(get_engineer);
            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [Authorize]
        public ActionResult import()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            return View();
        }

        [Authorize]
        public ActionResult edit(string id)
        {
            GetDataFromDb();
            var attachment = db.tb_attachment.ToList();
            Guid check_Guid;
            bool isValid = Guid.TryParse(id, out check_Guid);
            var sms = "";
            if (isValid)
            {
                if (User.IsInRole("admin"))
                {
                    Guid newGuid = Guid.Parse(id);
                    var data = db.tb_jobs.Where(w => w.is_delete == 0 && w.job_guid == newGuid).FirstOrDefault();
                    if (data != null)
                    {
                        var data_log = (from log_j in db.tb_jobs_worklog
                                        where log_j.is_delete == 0
                                              && log_j.jobs_id == data.id
                                              && log_j.appointment_datetime != null
                                              && log_j.appointment_datetime != data.appointment_datetime
                                        orderby log_j.appointment_datetime descending
                                        select new log_job()
                                        {
                                            Appointment_datetime = log_j.appointment_datetime,
                                            Appointment_to_datetime = log_j.appointment_to_datetime,
                                            status_job = ((CommonLib.Status_job)log_j.status_job).ToString(),
                                            job_start = log_j.job_start,
                                            job_repair = log_j.job_repair,
                                            job_end = log_j.job_end,
                                            reason_cancel = log_j.reason_cancel,
                                            cencel_date = log_j.create_date
                                        }).ToList();

                        var list_sms = db.tb_log.Where(w => w.job_id != null && w.job_id == data.id).ToList();
                        foreach (var item in list_sms)
                        {
                            if (sms == "")
                            {
                                sms = item.send_date.ToString();
                            }
                            else
                            {
                                if (DateTime.Parse(sms.ToString()) < item.send_date)
                                {
                                    sms = item.send_date.ToString();
                                }
                            }
                        }
                        // ค้นหา couponid 
                        if (data.id_coupon.HasValue != false)
                        {
                            //linq หาคูปอง จาก tbcoupon
                            var objcoupon = (from p in db.tb_coupon
                                             join j in db.tb_jobs on p.id equals j.id_coupon into ps
                                             from r in ps.DefaultIfEmpty()
                                             where p.id == data.id_coupon.Value
                                             select new coupon_job()
                                             {
                                                 code_coupon = p.code_coupon,
                                                 is_delete = p.is_delete,
                                                 detail = p.detail,
                                                 type_coupon = p.type_coupon,
                                                 wty = p.wty,
                                                 status_coupon = p.status_coupon,
                                                 coupon_start_date = p.coupon_start_date,
                                                 coupon_end_date = p.coupon_end_date,
                                                 is_used = p.is_used,
                                                 used_coupon_date = p.used_coupon_date,
                                                 req_date = r.req_coupon_date,
                                                 req_used = r.used_coupon_date
                                             }
                                             // select  p คือ sql *  from  ||| select new เลือกคอลั่มที่จะดึง
                                             ).FirstOrDefault();
                            ViewData["Coupon"] = objcoupon;
                        }


                        ViewData["sms_send"] = sms == "" ? null : sms;
                        ViewData["Data"] = data;
                        ViewData["Logs-Job"] = data_log;
                        ViewData["Picture"] = attachment.Where(w => w.is_delete == 0 && w.file_type == 1 && w.job_id == data.id).ToList();
                        ViewData["Voice"] = attachment.Where(w => w.is_delete == 0 && w.file_type == 2 && w.job_id == data.id).ToList();
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "job/all";
                        Response.Redirect(url);
                    }
                }
                else
                {
                    var getName = User.Identity.Name;
                    Guid newGuid = Guid.Parse(id);
                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var data = db.tb_jobs.Where(w => w.is_delete == 0 && w.job_guid == newGuid && w.store_id == idStore).FirstOrDefault();

                    if (data != null)
                    {
                        var data_log = (from log_j in db.tb_jobs_worklog
                                        where log_j.is_delete == 0
                                              && log_j.jobs_id == data.id
                                              && log_j.appointment_datetime != null
                                              && log_j.appointment_datetime != data.appointment_datetime
                                        orderby log_j.appointment_datetime descending
                                        select new log_job()
                                        {
                                            Appointment_datetime = log_j.appointment_datetime,
                                            Appointment_to_datetime = log_j.appointment_to_datetime,
                                            status_job = ((CommonLib.Status_job)log_j.status_job).ToString(),
                                            job_start = log_j.job_start,
                                            job_repair = log_j.job_repair,
                                            job_end = log_j.job_end,
                                            reason_cancel = log_j.reason_cancel,
                                            cencel_date = log_j.create_date
                                        }).ToList();

                        var list_sms = db.tb_log.Where(w => w.job_id != null && w.job_id == data.id).ToList();
                        foreach (var item in list_sms)
                        {
                            if (sms == "")
                            {
                                sms = item.send_date.ToString();
                            }
                            else
                            {
                                if (DateTime.Parse(sms.ToString()) < item.send_date)
                                {
                                    sms = item.send_date.ToString();
                                }
                            }
                        }
                
                        ViewData["sms_send"] = db.tb_log.Where(w => w.job_id != null && w.job_id == data.id).Select(s => s.send_date).FirstOrDefault();
                        ViewData["Data"] = data;
                        ViewData["Logs-Job"] = data_log;
                        ViewData["Picture"] = attachment.Where(w => w.is_delete == 0 && w.file_type == 1 && w.job_id == data.id).ToList();
                        ViewData["Voice"] = attachment.Where(w => w.is_delete == 0 && w.file_type == 2 && w.job_id == data.id).ToList();
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "job/all";
                        Response.Redirect(url);
                    }
                }
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "job/all";
                Response.Redirect(url);
            }

            // survey

            var obj_survey = (from s in db.tb_survey
                              join i in db.tb_survey_item on s.id equals i.survey_id
                              where s.is_delete == 0 && i.is_delete == 0 && s.job_guid == check_Guid
                              select new survey_job_edit
                              {
                                  job_guid = s.job_guid,
                                  is_feedback = s.is_feedback,
                                  feedback_note = s.feedback_note,
                                  feedback_date = s.feedback_create_date,
                                  feedback_user = s.feeback_user_update,
                                  score = s.source,
                                  survey_status = s.survey_status,
                                  negative = s.is_negative,
                                  comment = s.comment_1,
                                  qna = db.tb_survey_item.Where(w => w.is_delete == 0 && w.survey_id == s.id).OrderBy(s => s.jobs_category_id).ToList(),
                                  cre_date = s.create_date,
                              }).FirstOrDefault();
            ViewData["Survey"] = obj_survey;
            //

            StandardController service_con = new StandardController();
            string key_azure = service_con.GetKeyAzure();
            var category = db.tb_jobsl_category.ToList();
            ViewData["Key_Azure"] = key_azure;
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["Provinces"] = db.tb_provinces.ToList();
            ViewData["Districtes"] = db.tb_districts.ToList();
            ViewData["Sub_Districtes"] = db.tb_amphures.ToList();
            ViewData["Category"] = category.Where(w => w.is_delete == 0 && w.parent_id == 0).ToList();
            ViewData["Sub-Category"] = category.Where(w => w.is_delete == 0 && w.parent_id != 0).ToList();
            ViewData["Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
            var get_engineer = (from e in db.tb_engineer where e.is_delete == 0 select new getEngineer() { id = e.id, name = e.engineer_name, tel1 = e.tel1, tel2 = e.tel2, tel3 = e.tel3, repair_type_info1 = e.repair_type_info1, repair_type_info2 = e.repair_type_info2, repair_type_info3 = e.repair_type_info3, site_id = e.site_id }).ToList();
            ViewData["Engineer"] = get_engineer;
            ViewData["Azure_Path"] = ConfigurationManager.AppSettings["Azure_url"];

            return View();
        }

        //maybe delete
        [HttpGet]
        [Authorize]
        public object get_allData(int page)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(-7).Date, zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            DateTime querydate = DateTime.Now.AddYears(10); //Add 10 year
            var skip_page = page - 1;
            var skipPage = skip_page == 0 ? (skip_page * 10) : (skip_page * 10) + 10;
            var data = new List<all_job>();

            if (User.IsInRole("admin"))
            {
                var data1 = (from j in db.tb_jobs
                             join s in db.tb_survey on j.job_guid equals s.job_guid
                             where ((j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) || j.appointment_datetime == null || j.appointment_to_datetime == null) && j.is_delete == 0
                             select new all_job()
                             {
                                 id = j.id,
                                 job_guid = j.job_guid,
                                 job_no = j.service_order_no,
                                 status = ((CommonLib.Status_job)j.status_job).ToString(),
                                 duedate = j.appointment_datetime,
                                 to_duedate = j.appointment_to_datetime,
                                 customer_name = j.customer_fullname,
                                 job_type = j.service_type,
                                 asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                 engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                 mobile = j.phone_mobile,
                                 setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                 negative = s.is_negative,
                                 req_used = j.is_used_coupon
                             });
                data = data1.OrderByDescending(w => w.setDateOrder.Value).Skip(skipPage).Take(20).ToList();
                ViewData["Count-Data"] = data1.Count();
            }
            else
            {
                var getName = User.Identity.Name;
                if (getName != "")
                {
                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var data1 = (from j in db.tb_jobs
                                 join s in db.tb_survey on j.job_guid equals s.job_guid
                                 where ((j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) || j.appointment_datetime == null || j.appointment_to_datetime == null) && j.is_delete == 0 && j.store_id == idStore
                                 select new all_job()
                                 {
                                     id = j.id,
                                     job_guid = j.job_guid,
                                     job_no = j.service_order_no,
                                     status = ((CommonLib.Status_job)j.status_job).ToString(),
                                     duedate = j.appointment_datetime,
                                     to_duedate = j.appointment_to_datetime,
                                     customer_name = j.customer_fullname,
                                     job_type = j.service_type,
                                     asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                     engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                     mobile = j.phone_mobile,
                                     setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                     negative = s.is_negative,
                                     req_used = j.is_used_coupon
                                 });

                    data = data1.OrderByDescending(w => w.setDateOrder.Value).Skip(skipPage).Take(20).ToList();
                    ViewData["Count-Data"] = data1.Count();
                }
                else
                {
                    //wait redirect
                    var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                    Response.Redirect(url);
                }
            }

            ViewData["data-job"] = data;

            return data;

        }

        [HttpPost]
        public object import_file()
        {
            var excelfile = Request.Files[0];
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            var save_job = new List<job>();
            if (excelfile == null || excelfile.ContentLength == 0)
            {
                ViewBag.Error = "Please select a excel file";
                return View();
            }
            else
            {
                if (excelfile.FileName.EndsWith("xlsx"))
                {
                    Guid nameFile = Guid.NewGuid();

                    string path = Server.MapPath("~/import/" + nameFile.ToString() + excelfile.FileName);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    excelfile.SaveAs(path);
                    FileInfo excel2 = new FileInfo(Server.MapPath("~/import/" + nameFile.ToString() + excelfile.FileName));
                    using (var package = new ExcelPackage(excel2))
                    {
                        var workbook = package.Workbook;
                        var worksheet = workbook.Worksheets.First();
                        int totalRows = worksheet.Dimension.End.Row;
                        int totalColumn = worksheet.Dimension.End.Column;
                        int useColumn = totalColumn - 71;
                        var toDay = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone);
                        toDay = new DateTime(toDay.Year, toDay.Month, toDay.Day, 0, 0, 0);
                        var getName = User.Identity.Name;

                        var error_serial = "";

                        var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                        Guid convertGuid = new Guid(CheckUser.ToString());
                        var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
                        var col_code_store = 0; var col_service_order = 0; var col_code_engineer = 0; var col_code_category = 0; var col_code_model = 0;
                        var col_serial_no = 0; var col_status_code = 0; var col_status = 0; var col_reason_code = 0; var col_reason = 0;
                        var col_customer_fullname = 0; var col_street = 0; var col_city = 0; var col_district = 0; var col_zipcode = 0;
                        var col_phone_home = 0; var col_phone_office = 0; var col_phone_mobile = 0; var col_service_type = 0; var col_warranty_flag = 0;
                        var col_remark = 0; var col_repair_description = 0; var col_defect_description = 0; var col_status_comment = 0; var col_customer_comment = 0;
                        var col_store_name = 0; var col_engineer_name = 0; var col_name_category = 0; var col_request_date = 0; var col_purchase_date = 0;
                        var col_getParts = 0; var col_1st_appointment_date = 0; var col_1st_appointment_time = 0; var col_last_appointment_date = 0;
                        var col_customer_date = 0; var col_customer_time = 0;
                        for (int i = 1; i <= totalColumn; i++)
                        {
                            var text = worksheet.Cells[1, i];
                            //if(text.Value != null)
                            // {
                            switch (text.Value.ToString().Trim())
                            {
                                case "ASC Code": col_code_store = i; break;
                                case "Service Order No.": col_service_order = i; break;
                                case "ASC Name": col_store_name = i; break;
                                case "Engineer Code": col_code_engineer = i; break;
                                case "Engineer Name": col_engineer_name = i; break;
                                case "Service Product Code": col_code_category = i; break;
                                case "Service Product  Description": col_name_category = i; break;
                                case "Model": col_code_model = i; break;
                                case "Serial No": col_serial_no = i; break;
                                case "Status Code": col_status_code = i; break;
                                case "Status": col_status = i; break;
                                case "Reason Code": col_reason_code = i; break;
                                case "Reason": col_reason = i; break;
                                case "Customer Name": col_customer_fullname = i; break;
                                case "Street": col_street = i; break;
                                case "City": col_city = i; break;
                                case "District": col_district = i; break;
                                case "Zip Code": col_zipcode = i; break;
                                case "Phone No(home)": col_phone_home = i; break;
                                case "Phone No(office)": col_phone_office = i; break;
                                case "Phone No(mobile)": col_phone_mobile = i; break;
                                case "Service Type": col_service_type = i; break;
                                case "In Out Warranty Flag": col_warranty_flag = i; break;
                                case "Remark": col_remark = i; break;
                                case "Repair Description": col_repair_description = i; break;
                                case "Defect Description": col_defect_description = i; break;
                                case "Status comment": col_status_comment = i; break;
                                case "Customer Comment": col_customer_comment = i; break;
                                case "Request Date": col_request_date = i; break;
                                case "Purchase Date": col_purchase_date = i; break;
                                case "Parts Repair location 01": col_getParts = i; break;
                                case "ASC 1st Appointment Date": col_1st_appointment_date = i; break;
                                case "ASC 1st Appointment Time": col_1st_appointment_time = i; break;
                                case "ASC Last Appointment Date": col_last_appointment_date = i; break;
                                case "Customer Preferred Date": col_customer_date = i; break;
                                case "Customer Preferred Time": col_customer_time = i; break;
                            }
                            // }
                        }

                        if (col_code_store == 0 || col_service_order == 0 || col_store_name == 0 || col_code_engineer == 0 || col_engineer_name == 0 || col_code_category == 0 || col_name_category == 0 || col_code_model == 0 || col_serial_no == 0 || col_status_code == 0 || col_status == 0 ||
                            col_reason_code == 0 || col_reason == 0 || col_customer_fullname == 0 || col_street == 0 || col_city == 0 || col_district == 0 || col_zipcode == 0 || col_phone_home == 0 || col_phone_office == 0 || col_phone_mobile == 0 || col_service_type == 0 ||
                            col_warranty_flag == 0 || col_remark == 0 || col_repair_description == 0 || col_defect_description == 0 || col_status_comment == 0 || col_customer_comment == 0 || col_request_date == 0 || col_purchase_date == 0 || col_getParts == 0 ||
                            col_1st_appointment_date == 0 || col_1st_appointment_time == 0 || col_last_appointment_date == 0 || col_customer_date == 0 || col_customer_time == 0)
                        {
                            var return_data = new List<error_import>();
                            var text = new List<string>();

                            if (col_code_store == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Code");
                            if (col_service_order == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Order No.");
                            if (col_store_name == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Name");
                            if (col_code_engineer == 0) text.Insert(0, "ไม่พบคอลัมน์ Engineer Code");
                            if (col_engineer_name == 0) text.Insert(0, "ไม่พบคอลัมน์ Engineer Name");
                            if (col_code_category == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Product Code");
                            //if (col_name_category == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Product  Description");
                            if (col_code_model == 0) text.Insert(0, "ไม่พบคอลัมน์ Model");
                            if (col_serial_no == 0) text.Insert(0, "ไม่พบคอลัมน์ Serial No");
                            if (col_status_code == 0) text.Insert(0, "ไม่พบคอลัมน์ Status Code");
                            if (col_status == 0) text.Insert(0, "ไม่พบคอลัมน์ Status");
                            if (col_reason_code == 0) text.Insert(0, "ไม่พบคอลัมน์ Reason Code");
                            if (col_reason == 0) text.Insert(0, "ไม่พบคอลัมน์ Reason");
                            if (col_customer_fullname == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Name");
                            if (col_street == 0) text.Insert(0, "ไม่พบคอลัมน์ Street");
                            if (col_city == 0) text.Insert(0, "ไม่พบคอลัมน์ City");
                            if (col_district == 0) text.Insert(0, "ไม่พบคอลัมน์ District");
                            if (col_zipcode == 0) text.Insert(0, "ไม่พบคอลัมน์ Zip Code");
                            if (col_phone_home == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(home)");
                            if (col_phone_office == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(office)");
                            if (col_phone_mobile == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(mobile)");
                            if (col_service_type == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Type");
                            if (col_warranty_flag == 0) text.Insert(0, "ไม่พบคอลัมน์ In Out Warranty Flag");
                            if (col_remark == 0) text.Insert(0, "ไม่พบคอลัมน์ Remark");
                            if (col_repair_description == 0) text.Insert(0, "ไม่พบคอลัมน์ Repair Description");
                            if (col_defect_description == 0) text.Insert(0, "ไม่พบคอลัมน์ Defect Description");
                            if (col_status_comment == 0) text.Insert(0, "ไม่พบคอลัมน์ Status comment");
                            if (col_customer_comment == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Comment");
                            if (col_request_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Request Date");
                            if (col_purchase_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Purchase Date");
                            if (col_getParts == 0) text.Insert(0, "ไม่พบคอลัมน์ Parts Repair location 01");
                            if (col_1st_appointment_date == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC 1st Appointment Date");
                            if (col_1st_appointment_time == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC 1st Appointment Time");
                            if (col_last_appointment_date == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Last Appointment Date");
                            if (col_customer_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Preferred Date");
                            if (col_customer_time == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Preferred Time");

                            foreach (string value in text)
                            {
                                return_data.Add(new error_import
                                {
                                    text = value
                                });
                            }

                            //return_data.Add()

                            string jsonString2 = Newtonsoft.Json.JsonConvert.SerializeObject(return_data);
                            return new ContentResult()
                            {
                                Content = jsonString2,
                                ContentType = "application/json"
                            };
                        }

                        var isNew = false;
                        for (int row = 2; row <= totalRows; row++)
                        {
                            try
                            {
                                if (worksheet.Cells[row, col_service_order].Text != "")
                                {
                                    var col = worksheet.Column(row);
                                    Guid new_Guid = Guid.NewGuid();
                                    CommonLib.tb_jobs j = new CommonLib.tb_jobs();
                                    var text_error = "";
                                    //Get data from Excel
                                    string code_store = worksheet.Cells[row, col_code_store].Text == "" ? null : worksheet.Cells[row, col_code_store].Text;
                                    text_error += code_store != null ? "" : "ไม่พบข้อมูล คอลัมน์ ASC Code";
                                    string code_engineer = worksheet.Cells[row, col_code_engineer].Text == "" ? null : worksheet.Cells[row, col_code_engineer].Text;
                                    text_error += code_engineer != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Engineer Code" : " , ไม่พบข้อมูล คอลัมน์ Engineer Code";
                                    var get_service_order = worksheet.Cells[row, col_service_order].Text == "" ? null : worksheet.Cells[row, col_service_order].Text.Trim();
                                    text_error += get_service_order != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Service Order No." : " , ไม่พบข้อมูล คอลัมน์ Service Order No.";
                                    var code_category = worksheet.Cells[row, col_code_category].Text == "" ? null : worksheet.Cells[row, col_code_category].Text;
                                    text_error += code_category != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Service Product Code" : " , ไม่พบข้อมูล คอลัมน์ Service Product Code";
                                    var code_model = worksheet.Cells[row, col_code_model].Text == "" ? null : worksheet.Cells[row, col_code_model].Text;
                                    text_error += code_model != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Model" : " , ไม่พบข้อมูล คอลัมน์ Model";
                                    var ex_userUpdate = getName;
                                    var ex_serial_no = worksheet.Cells[row, col_serial_no].Text == "" ? null : worksheet.Cells[row, col_serial_no].Text;
                                    text_error += ex_serial_no != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Serial No" : " , ไม่พบข้อมูล คอลัมน์ Serial No";
                                    var ex_status_code = worksheet.Cells[row, col_status_code].Text;
                                    var ex_status = worksheet.Cells[row, col_status].Text;
                                    var ex_reason_code = worksheet.Cells[row, col_reason_code].Text;
                                    var ex_reason = worksheet.Cells[row, col_reason].Text;
                                    var ex_customer_fullname = worksheet.Cells[row, col_customer_fullname].Text == "" ? null : worksheet.Cells[row, col_customer_fullname].Text;
                                    text_error += ex_customer_fullname != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Customer Name" : " , ไม่พบข้อมูล คอลัมน์ Customer Name";
                                    var ex_street = worksheet.Cells[row, col_street].Text;
                                    var ex_city = worksheet.Cells[row, col_city].Text;
                                    var ex_district = worksheet.Cells[row, col_district].Text;
                                    int ex_zipcode = worksheet.Cells[row, col_zipcode].Text != "" ? int.Parse(worksheet.Cells[row, col_zipcode].Text) : 0;
                                    var ex_phone_home = worksheet.Cells[row, col_phone_home].Text;
                                    var ex_phone_office = worksheet.Cells[row, col_phone_office].Text;
                                    var ex_phone_mobile = worksheet.Cells[row, col_phone_mobile].Text;
                                    var ex_service_type = worksheet.Cells[row, col_service_type].Text;
                                    var ex_warranty_flag = worksheet.Cells[row, col_warranty_flag].Text;
                                    var ex_remark = worksheet.Cells[row, col_remark].Text;
                                    var ex_repair_description = worksheet.Cells[row, col_repair_description].Text;
                                    var ex_defect_description = worksheet.Cells[row, col_defect_description].Text;
                                    var ex_status_comment = worksheet.Cells[row, col_status_comment].Text;
                                    var ex_customer_comment = worksheet.Cells[row, col_customer_comment].Text;

                                    error_serial = get_service_order == null ? " - " : get_service_order;
                                    var ex_customer_time = worksheet.Cells[row, col_customer_time].Text;

                                    //Get data from database
                                    var store = db.tb_store.Where(w => w.code_store == code_store && w.is_delete == 0).Select(s => s.id).FirstOrDefault();
                                    var engineer = db.tb_engineer.Where(w => w.code_engineer == code_engineer && w.is_delete == 0).Select(s => new { id = s.id, name = s.engineer_name }).FirstOrDefault();
                                    var new_enname = "";
                                    var new_enid = 0;
                                    var id_job = db.tb_jobs.Where(w => w.service_order_no == get_service_order && w.is_delete == 0).FirstOrDefault();
                                    var sub_category = db.tb_jobsl_category.Where(w => w.code == code_category && w.is_delete == 0).Select(s => s.id).FirstOrDefault();
                                    var category = db.tb_jobsl_category.Where(w => w.code == code_category && w.is_delete == 0).Select(s => s.parent_id).FirstOrDefault();
                                    var model_db = db.tb_jobsl_category.Where(w => w.code == code_model && w.is_delete == 0).Select(s => s.id).FirstOrDefault();
                                    var province = db.tb_provinces.Where(w => w.province_name == ex_city).Select(s => s.province_id).FirstOrDefault();
                                    var distrince = db.tb_districts.Where(w => w.district_name == ex_district).Select(s => s.district_id).FirstOrDefault();

                                    if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                    {
                                        //Check Account Store and Create
                                        var CheckUserStore = Membership.GetUser(code_store);
                                        if (CheckUserStore == null)
                                        {
                                            var email = code_store + "@mail.com";
                                            MembershipUser newUserStore = Membership.CreateUser(code_store, "Pa@sswd2019", email);
                                            Membership.UpdateUser(newUserStore);
                                            Roles.AddUserToRole(code_store, "shop");
                                        }

                                        // Create Store
                                        if (store == 0 && code_store != "")
                                        {
                                            var getGuidStore = Membership.GetUser(code_store).ProviderUserKey;
                                            Guid convertGuidStore = new Guid(getGuidStore.ToString());
                                            Guid new_Guid_Store = Guid.NewGuid();
                                            string name_site = worksheet.Cells[row, col_store_name].Text;
                                            tb_store obj_new_store = new tb_store();
                                            obj_new_store.store_guid = new_Guid_Store;
                                            obj_new_store.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            obj_new_store.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            obj_new_store.user_update = getName;
                                            obj_new_store.is_delete = 0;
                                            obj_new_store.code_store = code_store;
                                            obj_new_store.province = 0;
                                            obj_new_store.district = 0;
                                            obj_new_store.sub_district = 0;
                                            obj_new_store.site_name = name_site;

                                            //set map
                                            tb_mapping_store obj_map = new tb_mapping_store();
                                            obj_map.account_guid = convertGuidStore;
                                            obj_map.site_id = obj_new_store.id;
                                            db.tb_mapping_store.Add(obj_map);
                                            db.SaveChanges();

                                            db.tb_store.Add(obj_new_store);
                                            db.SaveChanges();

                                            store = db.tb_store.Where(w => w.code_store == code_store && w.is_delete == 0).Select(s => s.id).FirstOrDefault();
                                        }
                                    }

                                    if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                    {

                                        //Check Account Engineer and Create
                                        if (code_engineer != "")
                                        {
                                            var CheckUserEngineer = Membership.GetUser(code_engineer);
                                            if (CheckUserEngineer == null)
                                            {
                                                var email = code_engineer + "@mail.com";
                                                MembershipUser newUserEngineer = Membership.CreateUser(code_engineer, "Pa@sswd2019", email);
                                                Membership.UpdateUser(newUserEngineer);
                                                Roles.AddUserToRole(code_engineer, "engineer");
                                            }
                                        }

                                        //Create Engineer
                                        if (engineer == null && code_engineer != "")
                                        {
                                            var getGuidEngineer = Membership.GetUser(code_engineer).ProviderUserKey;
                                            Guid convertGuidEngineer = new Guid(getGuidEngineer.ToString());
                                            Guid new_Guid_Engineer = Guid.NewGuid();
                                            string name_engineer = worksheet.Cells[row, col_engineer_name].Text;
                                            tb_engineer obj_new_en = new tb_engineer();
                                            obj_new_en.en_guid = new_Guid_Engineer;
                                            obj_new_en.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            obj_new_en.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            obj_new_en.user_update = getName;
                                            obj_new_en.is_delete = 0;
                                            obj_new_en.province = 0;
                                            obj_new_en.engineer_name = name_engineer;
                                            obj_new_en.site_id = store;
                                            obj_new_en.code_engineer = code_engineer;
                                            obj_new_en.account_guid = convertGuidEngineer;

                                            db.tb_engineer.Add(obj_new_en);
                                            db.SaveChanges();
                                            var new_en1 = db.tb_engineer.Where(w => w.code_engineer == code_engineer && w.is_delete == 0).Select(s => new { id = s.id, name = s.engineer_name }).FirstOrDefault();
                                            new_enid = new_enid = new_en1.id;
                                            new_enname = new_en1.name;
                                        }
                                    }

                                    if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                    {
                                        //Create Category
                                        if ((category == null || sub_category == 0) && code_category != "")
                                        {
                                            Guid newGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

                                            var id_other = db.tb_jobsl_category.Where(w => w.guid_category == newGuid).Select(s => s.id).FirstOrDefault();
                                            int id = id_other == 0 ? 0 : id_other;
                                            //create other
                                            if (id_other == 0)
                                            {
                                                tb_jobsl_category new_other = new tb_jobsl_category();
                                                new_other.create_date = DateTime.Now;
                                                new_other.update_date = DateTime.Now;
                                                new_other.is_delete = 0;
                                                new_other.name = "Other";
                                                new_other.code = "other";
                                                new_other.description = "Other";
                                                new_other.guid_category = newGuid;
                                                new_other.user_update = getName;
                                                new_other.parent_id = 0;

                                                db.tb_jobsl_category.Add(new_other);
                                                db.SaveChanges();
                                                id = new_other.id;
                                            }

                                            Guid new_Guid_Category = Guid.NewGuid();
                                            int colum_category = col_name_category == 0 ? (col_code_category + 1) : col_name_category;
                                            string name_category = worksheet.Cells[row, colum_category].Text;
                                            tb_jobsl_category obj_new_category = new tb_jobsl_category();
                                            obj_new_category.create_date = DateTime.Now;
                                            obj_new_category.update_date = DateTime.Now;
                                            obj_new_category.is_delete = 0;
                                            obj_new_category.name = name_category;
                                            obj_new_category.code = code_category;
                                            obj_new_category.description = name_category;
                                            obj_new_category.guid_category = new_Guid_Category;
                                            obj_new_category.user_update = getName;
                                            obj_new_category.parent_id = id;

                                            db.tb_jobsl_category.Add(obj_new_category);
                                            db.SaveChanges();

                                            category = db.tb_jobsl_category.Where(w => w.code == code_category && w.is_delete == 0).Select(s => s.parent_id).FirstOrDefault();
                                            sub_category = db.tb_jobsl_category.Where(w => w.code == code_category && w.is_delete == 0).Select(s => s.id).FirstOrDefault();
                                        }
                                    }


                                    // Check date "System.DateTime"

                                    //last_appointment_date
                                    var check_last_app = false;
                                    var last_appointment_date = worksheet.Cells[row, col_last_appointment_date].Text != "" ? worksheet.Cells[row, col_last_appointment_date].Value.ToString() : "01/01/1999";
                                    last_appointment_date = last_appointment_date.Length > 10 ? last_appointment_date.Substring(0, last_appointment_date.IndexOf(" ")) : last_appointment_date;
                                    last_appointment_date = last_appointment_date == "01/01/1999" ? last_appointment_date : last_appointment_date.IndexOf("00") != -1 ? last_appointment_date : worksheet.Cells[row, col_1st_appointment_date].Value.ToString();

                                    var C_last_appointment_date = worksheet.Cells[row, col_last_appointment_date].Text;

                                    DateTime? D_last_appointment_date = (DateTime?)null;
                                    if (C_last_appointment_date != "")
                                    {
                                        var check_last_appointment_date = worksheet.Cells[row, col_last_appointment_date].Value.GetType();
                                        if (check_last_appointment_date.FullName == "System.DateTime" && (last_appointment_date != "01/01/1999"))
                                        {
                                            check_last_app = true;
                                            D_last_appointment_date = (DateTime)worksheet.Cells[row, col_last_appointment_date].Value;
                                        }
                                        else
                                        {
                                            last_appointment_date = worksheet.Cells[row, col_last_appointment_date].Value.ToString();
                                            last_appointment_date = last_appointment_date.IndexOf("/") == -1 ? worksheet.Cells[row, col_last_appointment_date].Text : last_appointment_date;
                                            last_appointment_date = last_appointment_date.Length > 10 ? last_appointment_date.Substring(0, last_appointment_date.IndexOf(" ")) : last_appointment_date;
                                        }
                                    }

                                    //first_appointment_date
                                    var check_first_app = false;
                                    DateTime? D_first_appointment_date = (DateTime?)null;
                                    var st_appointment_date = worksheet.Cells[row, col_1st_appointment_date].Text != "" ? worksheet.Cells[row, col_1st_appointment_date].Value.ToString() : "01/01/1999";
                                    st_appointment_date = st_appointment_date.Length > 10 ? st_appointment_date.Substring(0, st_appointment_date.IndexOf(" ")) : st_appointment_date;
                                    st_appointment_date = st_appointment_date == "01/01/1999" ? st_appointment_date : st_appointment_date.IndexOf("00") != -1 ? st_appointment_date : worksheet.Cells[row, col_1st_appointment_date].Value.ToString();

                                    var C_st_appointment_date = worksheet.Cells[row, col_1st_appointment_date].Text;
                                    var check_st_appointment_date = worksheet.Cells[row, col_1st_appointment_date].Value.GetType();
                                    if (C_st_appointment_date != "")
                                    {
                                        if (check_st_appointment_date.FullName == "System.DateTime" && (st_appointment_date != "01/01/1999"))
                                        {
                                            check_first_app = true;
                                            D_first_appointment_date = (DateTime)worksheet.Cells[row, col_1st_appointment_date].Value;

                                            //test
                                            var st_appointment_time = worksheet.Cells[row, col_1st_appointment_time].Text;
                                            if (st_appointment_time != "")
                                            {
                                                st_appointment_time = worksheet.Cells[row, col_1st_appointment_time].Value.ToString();
                                                st_appointment_time = st_appointment_time.Substring(st_appointment_time.IndexOf(" ") + 1);
                                                D_first_appointment_date = DateTime.ParseExact(D_first_appointment_date.Value.ToString("dd/MM/yyyy") + " " + st_appointment_time, "dd/MM/yyyy h:mm:ss tt", null);

                                            }
                                        }
                                        else
                                        {
                                            st_appointment_date = worksheet.Cells[row, col_1st_appointment_date].Value.ToString();
                                            st_appointment_date = st_appointment_date.IndexOf("/") == -1 ? worksheet.Cells[row, col_1st_appointment_date].Text : st_appointment_date;
                                            st_appointment_date = st_appointment_date.Length > 10 ? st_appointment_date.Substring(0, st_appointment_date.IndexOf(" ")) : st_appointment_date;
                                        }
                                    }

                                    var final_appointment = last_appointment_date.IndexOf("00") == -1 && last_appointment_date != "01/01/1999" ? last_appointment_date : st_appointment_date;

                                    DateTime? finalappTime = (DateTime?)null;
                                    if (!check_first_app && !check_last_app)
                                    {
                                        var st_appointment_time = worksheet.Cells[row, col_1st_appointment_time].Text != "" ? worksheet.Cells[row, col_1st_appointment_time].Value.ToString() : "";
                                        st_appointment_time = st_appointment_time.Substring(st_appointment_time.IndexOf(" ") + 1);
                                        finalappTime = st_appointment_date == "01/01/1999" ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(final_appointment, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        if (finalappTime != null) finalappTime = finalappTime < toDay ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(final_appointment, new string[] { "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone) : finalappTime;
                                        finalappTime = finalappTime == null ? finalappTime : last_appointment_date == "01/01/1999" ? DateTime.ParseExact(finalappTime.Value.ToString("dd/MM/yyyy") + " " + st_appointment_time, "dd/MM/yyyy h:mm:ss tt", null) : finalappTime;
                                    }

                                    //customer time
                                    var check_customer_date = false;
                                    DateTime? D_customer = (DateTime?)null;
                                    //var ex_customer_date = worksheet.Cells[row, col_customer_date].Value.ToString().Length < 10 ? worksheet.Cells[row, col_customer_date].Text : worksheet.Cells[row, col_customer_date].Value.ToString();
                                    var ex_customer_date = worksheet.Cells[row, col_customer_date].Text != "" ? worksheet.Cells[row, col_customer_date].Value.ToString() : "01/01/1999";
                                    ex_customer_date = ex_customer_date.Length > 10 ? ex_customer_date.Substring(0, ex_customer_date.IndexOf(" ")) : ex_customer_date;
                                    ex_customer_date = ex_customer_date == "01/01/1999" ? ex_customer_date : ex_customer_date.IndexOf("00") != -1 ? ex_customer_date : worksheet.Cells[row, col_customer_date].Value.ToString();
                                    var C_customer = worksheet.Cells[row, col_1st_appointment_date].Text;
                                    var check_customer = worksheet.Cells[row, col_1st_appointment_date].Value.GetType();
                                    DateTime? save_customer_date = (DateTime?)null;
                                    if (C_customer != "")
                                    {
                                        if (check_customer.FullName == "System.DateTime" && (ex_customer_date != "01/01/1999"))
                                        {
                                            check_customer_date = true;
                                            D_customer = (DateTime)worksheet.Cells[row, col_customer_date].Value;
                                        }
                                        else
                                        {
                                            ex_customer_date = worksheet.Cells[row, col_customer_date].Value.ToString();
                                            ex_customer_date = ex_customer_date.IndexOf("/") == -1 ? worksheet.Cells[row, col_customer_date].Text : ex_customer_date;
                                            ex_customer_date = ex_customer_date.Length > 10 ? ex_customer_date.Substring(0, ex_customer_date.IndexOf(" ")) : ex_customer_date;
                                        }
                                    }

                                    if (!check_customer_date)
                                    {
                                        save_customer_date = ex_customer_date == "01/01/1999" ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ex_customer_date, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        if (save_customer_date != null) save_customer_date = save_customer_date < toDay ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ex_customer_date, new string[] { "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone) : save_customer_date;
                                    }


                                    //set Date
                                    var request_date = worksheet.Cells[row, col_request_date].Text != "" ? worksheet.Cells[row, col_request_date].Value.ToString().Length < 10 ? worksheet.Cells[row, col_request_date].Text : worksheet.Cells[row, col_request_date].Value.ToString() : "01/01/1999";
                                    var purchase_date = worksheet.Cells[row, col_purchase_date].Text != "" ? worksheet.Cells[row, col_purchase_date].Value.ToString() == "00/00/0000" ? "01/01/1999" : worksheet.Cells[row, col_purchase_date].Value.ToString().Length < 10 ? worksheet.Cells[row, col_purchase_date].Text == "00/00/0000" ? "01/01/1999" : worksheet.Cells[row, col_purchase_date].Text : worksheet.Cells[row, col_purchase_date].Value.ToString() : "01/01/1999";
                                    purchase_date = purchase_date.Length > 10 ? purchase_date.Substring(0, purchase_date.IndexOf(" ")) : purchase_date;
                                    request_date = request_date.Length > 10 ? request_date.Substring(0, request_date.IndexOf(" ")) : request_date;


                                    //status job 
                                    int im_status_job = 0;
                                    switch (ex_status_code.Trim())
                                    {
                                        case "ST010":
                                            im_status_job = 0; break;
                                        case "ST015":
                                            im_status_job = 0; break;
                                        case "ST025":
                                            im_status_job = 6; break;
                                        case "ST030":
                                            im_status_job = 2; break;
                                        case "ST035":
                                            im_status_job = 3; break;
                                        default:
                                            im_status_job = 0; break;
                                    }



                                    if (im_status_job == 2 && (ex_reason_code.Trim() == "HP015" || ex_reason_code.Trim() == "HP020" || ex_reason_code.Trim() == "HP065"))
                                    {
                                        im_status_job = 6;
                                    }

                                    var keymap = ConfigurationManager.AppSettings["googleMap_key_forserver"];
                                    ex_phone_mobile = ex_phone_mobile == "" || ex_phone_mobile.Length == 10 ? ex_phone_mobile : ex_phone_mobile[0] == 0 ? ex_phone_mobile : "0" + ex_phone_mobile;
                                    ex_phone_office = ex_phone_office == "" ? ex_phone_office : ex_phone_office[0] == 0 ? ex_phone_office : "0" + ex_phone_office;
                                    ex_phone_home = ex_phone_home == "" ? ex_phone_home : ex_phone_home[0] == 0 ? ex_phone_home : "0" + ex_phone_home;
                                    //Add new job
                                    if (id_job == null)
                                    {
                                        j.job_guid = new_Guid;
                                        j.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                        j.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                        j.user_update = getName; // change user login
                                        j.change_user = ex_userUpdate;
                                        j.is_delete = 0;
                                        j.service_order_no = get_service_order;
                                        j.model = code_model;
                                        j.serial_no = ex_serial_no;
                                        j.customer_fullname = ex_customer_fullname;
                                        j.street = ex_street;
                                        j.im_province = ex_city;
                                        j.province = province;
                                        j.im_district = ex_district;
                                        j.district = distrince;
                                        j.zipcode = ex_zipcode;
                                        j.status_comment = ex_status_comment;
                                        j.customer_comment = ex_customer_comment;
                                        j.phone_home = ex_phone_home;
                                        j.phone_office = ex_phone_office;
                                        j.phone_mobile = ex_phone_mobile;
                                        j.service_type = ex_service_type;
                                        j.warranty_flag = ex_warranty_flag;
                                        j.reason_code = ex_reason_code;
                                        j.purchase_date = purchase_date == "01/01/1999" || purchase_date.IndexOf("00") != -1 ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(purchase_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        j.request_date = request_date == "01/01/1999" ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(request_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        j.status_code = ex_status_code;
                                        j.status = ex_status;
                                        j.reason = ex_reason;
                                        j.remark = ex_remark;
                                        j.repair_description = ex_repair_description;
                                        j.defect_description = ex_defect_description;
                                        j.store_code = code_store == "" ? null : code_store;
                                        j.store_id = code_store == "" ? 0 : store;
                                        j.engineer_code = code_engineer == "" || code_engineer == null ? null : code_engineer;
                                        j.engineer_id = code_engineer == "" || code_engineer == null ? 0 : engineer == null ? new_enid : engineer.id;
                                        j.is_change = "excel";
                                        j.job_category_id = category;
                                        j.sub_category_id = sub_category;
                                        j.status_job = im_status_job;
                                        j.appointment_datetime = !check_first_app && !check_last_app ? finalappTime : check_last_app ? D_last_appointment_date : D_first_appointment_date;
                                        j.customer_prefer_date = check_customer_date ? D_customer : save_customer_date; //ex_customer_date == "00/00/0000" || ex_customer_date.IndexOf("00") != -1 ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ex_customer_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        j.customer_perfer_time = ex_customer_time;

                                        if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                        {
                                            db.tb_jobs.Add(j);
                                            db.SaveChanges();
                                            isNew = true;

                                            //log job
                                            //CommonLib.tb_jobs_worklog noti = new CommonLib.tb_jobs_worklog();
                                            //noti.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            //noti.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            //noti.user_update = getName;
                                            //noti.is_delete = 0;
                                            //noti.jobs_id = j.id;
                                            //noti.appointment_datetime = j.appointment_datetime;
                                            //noti.appointment_to_datetime = j.appointment_to_datetime;
                                            //noti.status_job = j.status_job;
                                            //noti.job_start = j.job_start;
                                            //noti.job_start_lat = j.job_start_lat;
                                            //noti.job_start_long = j.job_start_long;
                                            //noti.job_repair = j.job_repair;
                                            //noti.job_repair_lat = j.job_repair_lat;
                                            //noti.job_repair_long = j.job_repair_long;
                                            //noti.job_end = j.job_end;
                                            //noti.id_cancel = j.id_cancel;
                                            //noti.reason_cancel = j.reason_cancel;
                                            //noti.cancel_date = j.cancel_date;

                                            //db.tb_jobs_worklog.Add(noti);
                                            //db.SaveChanges();
                                            //-end log job

                                            //---------------MAP---------------
                                            var tem_address = ex_street + " " + ex_district + " " + ex_city + " " + ex_zipcode;
                                            var client = new RestClient("https://maps.googleapis.com/maps/api/geocode/json?address=" + tem_address + "&key=" + keymap);
                                            var request = new RestRequest(Method.GET);
                                            request.AddHeader("cache-control", "no-cache");
                                            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                            IRestResponse response = client.Execute(request);
                                            api_map data_map = JsonConvert.DeserializeObject<api_map>(response.Content);
                                            var data_lat = data_map.results.Select(s => s.geometry.location.lat).FirstOrDefault();
                                            var data_long = data_map.results.Select(s => s.geometry.location.lng).FirstOrDefault();
                                            j.customer_lat = data_lat.ToString();
                                            j.customer_long = data_long.ToString();
                                            db.tb_jobs.AddOrUpdate(j);
                                            db.SaveChanges();
                                            //
                                        }
                                        else
                                        {
                                            isNew = true;
                                        }
                                    }
                                    else
                                    {
                                        // Update Jobs
                                        var service = get_service_order == id_job.service_order_no ? id_job.service_order_no : get_service_order;
                                        var model = code_model == id_job.model ? id_job.model : code_model;
                                        request_date = request_date.Length > 10 ? request_date.Substring(0, request_date.IndexOf(" ")) : request_date;
                                        var db_request_date = id_job.request_date.ToString();
                                        db_request_date = db_request_date.Length > 10 ? db_request_date.Substring(0, db_request_date.IndexOf(" ")) : db_request_date;
                                        var requestDate = request_date == db_request_date ? id_job.request_date : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(request_date, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone); ;
                                        var purchaseDate = purchase_date.IndexOf("00") != -1 ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(purchase_date.ToString(), new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone) == id_job.purchase_date ? id_job.purchase_date : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(purchase_date.ToString(), new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone); ;

                                        //check date

                                        var db_appointment_datetime = id_job.appointment_datetime != null ? id_job.appointment_datetime.Value.ToString("dd/MM/yyyy") : "";
                                        db_appointment_datetime = db_appointment_datetime.Length > 10 ? db_appointment_datetime.Substring(0, db_appointment_datetime.IndexOf(" ")) : db_appointment_datetime;

                                        final_appointment = !check_first_app && !check_last_app ? finalappTime != null ? finalappTime.Value.ToString("dd/MM/yyyy") : "" : check_last_app ? D_last_appointment_date.Value.ToString("dd/MM/yyyy") : D_first_appointment_date.Value.ToString("dd/MM/yyyy");
                                        var final_app = final_appointment == db_appointment_datetime ? id_job.appointment_datetime : final_appointment == "" ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(final_appointment, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);

                                        //check end date
                                        var check_endDate = final_appointment == db_appointment_datetime ? true : false;
                                        var data_end = check_endDate == true ? id_job.appointment_to_datetime : (id_job.status_job == 2 || id_job.status_job == 0 || id_job.status_job == 6) ? (DateTime?)null : id_job.appointment_to_datetime;
                                        //check date to
                                        var data_to = check_endDate == true ? id_job.appointment_datetime : (id_job.status_job == 2 || id_job.status_job == 0 || id_job.status_job == 6) ? final_app : id_job.appointment_datetime;

                                        id_job.job_guid = id_job.job_guid;
                                        id_job.create_date = id_job.create_date;
                                        id_job.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                        id_job.user_update = getName; //change user login
                                        id_job.change_user = ex_userUpdate == id_job.user_update ? id_job.user_update : ex_userUpdate;
                                        id_job.is_delete = 0;
                                        id_job.service_order_no = service;
                                        id_job.model = model;
                                        id_job.serial_no = ex_serial_no == id_job.serial_no ? id_job.serial_no : ex_serial_no;
                                        id_job.customer_fullname = ex_customer_fullname == id_job.customer_fullname ? id_job.customer_fullname : ex_customer_fullname;
                                        id_job.street = ex_street == id_job.street ? id_job.street : ex_street;
                                        id_job.im_province = ex_city == id_job.im_province ? id_job.im_province : ex_city;
                                        id_job.province = province == id_job.province ? id_job.province : province;
                                        id_job.im_district = ex_district == id_job.im_district ? id_job.im_district : ex_district;
                                        id_job.district = distrince == id_job.district ? id_job.district : distrince;
                                        id_job.zipcode = ex_zipcode == id_job.zipcode ? id_job.zipcode : ex_zipcode;
                                        id_job.status_comment = ex_status_comment == id_job.status_comment ? id_job.status_comment : ex_status_comment;
                                        id_job.customer_comment = ex_customer_comment == id_job.customer_comment ? id_job.customer_comment : ex_customer_comment;
                                        id_job.phone_home = ex_phone_home == id_job.phone_home ? id_job.phone_home : ex_phone_home;
                                        id_job.phone_office = ex_phone_office == id_job.phone_office ? id_job.phone_office : ex_phone_office;
                                        id_job.phone_mobile = ex_phone_mobile == id_job.phone_mobile ? id_job.phone_mobile : ex_phone_mobile;
                                        id_job.warranty_flag = ex_warranty_flag == id_job.warranty_flag ? id_job.warranty_flag : ex_warranty_flag;
                                        id_job.reason_code = ex_reason_code == id_job.reason_code ? id_job.reason_code : ex_reason_code;
                                        id_job.request_date = requestDate.Value.ToString("dd/MM/yyyy") == "01/01/1999" ? (DateTime?)null : requestDate;
                                        id_job.purchase_date = purchaseDate == null ? (DateTime?)null : purchaseDate.Value.Year.ToString() == "1999" ? (DateTime?)null : purchaseDate;
                                        id_job.status_code = ex_status_code == id_job.status_code ? id_job.status_code : ex_status_code;
                                        id_job.status = ex_status == id_job.status ? id_job.status : ex_status;
                                        id_job.reason = ex_reason == id_job.reason ? id_job.reason : ex_reason;
                                        id_job.remark = ex_remark == id_job.remark ? id_job.remark : ex_remark;
                                        id_job.service_type = ex_service_type == id_job.service_type ? id_job.service_type : ex_service_type;
                                        id_job.repair_description = ex_repair_description == id_job.repair_description ? id_job.repair_description : ex_repair_description;
                                        id_job.defect_description = ex_defect_description == id_job.defect_description ? id_job.defect_description : ex_defect_description;
                                        id_job.store_code = code_store == "" ? null : code_store;
                                        id_job.store_id = code_store == "" ? 0 : store;
                                        id_job.engineer_code = code_engineer == "" || code_engineer == null ? null : code_engineer;
                                        id_job.engineer_id = code_engineer == "" || code_engineer == null ? 0 : engineer == null ? new_enid : engineer.id;
                                        id_job.is_change = "excel";
                                        id_job.status_job = id_job.status_job == 3 || id_job.status_job == 7 || id_job.status_job == 8 || id_job.status_job == 9 || id_job.status_job == 10 || id_job.status_job == 11 ? id_job.status_job : im_status_job;
                                        id_job.job_category_id = code_category == "" ? 0 : category;
                                        id_job.sub_category_id = sub_category;
                                        id_job.appointment_datetime = data_to;
                                        id_job.appointment_to_datetime = data_end;
                                        id_job.customer_prefer_date = check_customer_date ? D_customer : save_customer_date;//ex_customer_date == "00/00/0000" || ex_customer_date.IndexOf("00") != -1 ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ex_customer_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                        id_job.customer_perfer_time = ex_customer_time;

                                        if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                        {
                                            db.tb_jobs.AddOrUpdate(id_job);
                                            db.SaveChanges();
                                            isNew = false;

                                            DateTime start_date = id_job.appointment_datetime != null ? TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(id_job.appointment_datetime.Value.ToString("MM dd yyyy")), zone) : TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime("01/01/1999"), zone);
                                            DateTime tomorrow_date = id_job.appointment_datetime != null ? TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(id_job.appointment_datetime.Value.ToString("MM dd yyyy")).AddDays(1), zone) : TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime("01/01/1999"), zone);

                                            //var data_log = db.tb_jobs_worklog.Where(w => w.jobs_id == id_job.id && w.appointment_datetime >= start_date && w.appointment_datetime <= tomorrow_date).FirstOrDefault();

                                            //if (data_log != null)
                                            //{
                                            //    data_log.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            //    data_log.user_update = getName;
                                            //    data_log.is_delete = 0;
                                            //    data_log.jobs_id = id_job.id;
                                            //    data_log.appointment_datetime = id_job.appointment_datetime;
                                            //    data_log.appointment_to_datetime = id_job.appointment_to_datetime;
                                            //    data_log.status_job = id_job.status_job;
                                            //    data_log.job_start = id_job.job_start;
                                            //    data_log.job_start_lat = id_job.job_start_lat;
                                            //    data_log.job_start_long = id_job.job_start_long;
                                            //    data_log.job_repair = id_job.job_repair;
                                            //    data_log.job_repair_lat = id_job.job_repair_lat;
                                            //    data_log.job_repair_long = id_job.job_repair_long;
                                            //    data_log.job_end = id_job.job_end;
                                            //    data_log.id_cancel = id_job.id_cancel;
                                            //    data_log.reason_cancel = id_job.reason_cancel;
                                            //    data_log.cancel_date = id_job.cancel_date;

                                            //    db.tb_jobs_worklog.AddOrUpdate(data_log);
                                            //    db.SaveChanges();
                                            //}
                                            //else
                                            //{
                                            //    //log job
                                            //    CommonLib.tb_jobs_worklog noti = new CommonLib.tb_jobs_worklog();
                                            //    noti.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            //    noti.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                            //    noti.user_update = getName;
                                            //    noti.is_delete = 0;
                                            //    noti.jobs_id = id_job.id;
                                            //    noti.appointment_datetime = id_job.appointment_datetime;
                                            //    noti.appointment_to_datetime = id_job.appointment_to_datetime;
                                            //    noti.status_job = id_job.status_job;
                                            //    noti.job_start = id_job.job_start;
                                            //    noti.job_start_lat = id_job.job_start_lat;
                                            //    noti.job_start_long = id_job.job_start_long;
                                            //    noti.job_repair = id_job.job_repair;
                                            //    noti.job_repair_lat = id_job.job_repair_lat;
                                            //    noti.job_repair_long = id_job.job_repair_long;
                                            //    noti.job_end = id_job.job_end;
                                            //    noti.id_cancel = id_job.id_cancel;
                                            //    noti.reason_cancel = id_job.reason_cancel;
                                            //    noti.cancel_date = id_job.cancel_date;

                                            //    db.tb_jobs_worklog.Add(noti);
                                            //    db.SaveChanges();
                                            //    //-end log job
                                            //}

                                            //jobs update ถ้ายังไม่มี lat , long ถึงจะขอ api
                                            if ((id_job.customer_lat == null || id_job.customer_lat == "0") && (id_job.customer_long == null || id_job.customer_long == "0"))
                                            {
                                                //---------------MAP------------------
                                                var tem_address = ex_street + " " + ex_district + " " + ex_city + " " + ex_zipcode;
                                                var client = new RestClient("https://maps.googleapis.com/maps/api/geocode/json?address=" + tem_address + "&key=" + keymap);
                                                var request = new RestRequest(Method.GET);
                                                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                                IRestResponse response = client.Execute(request);
                                                api_map data_map = JsonConvert.DeserializeObject<api_map>(response.Content);
                                                var data_lat = data_map.results.Select(s => s.geometry.location.lat).FirstOrDefault();
                                                var data_long = data_map.results.Select(s => s.geometry.location.lng).FirstOrDefault();
                                                if (data_lat != 0)
                                                {
                                                    id_job.customer_lat = data_lat.ToString();
                                                }
                                                if (data_long != 0)
                                                {
                                                    id_job.customer_long = data_long.ToString();
                                                }
                                            }
                                            db.tb_jobs.AddOrUpdate(id_job);
                                            db.SaveChanges();
                                        }
                                        else
                                        {
                                            isNew = false;
                                        }
                                    }

                                    //Add parts
                                    if (col_getParts != 0)
                                    {
                                        var getParts = worksheet.Cells[row, col_getParts].Text;
                                        if (getParts != "")
                                        {
                                            tb_jobs_parts obj_new_parts = new tb_jobs_parts();
                                            int col_parts = col_getParts;

                                            //get data from database
                                            var Getguid = id_job == null ? new_Guid : id_job.job_guid;

                                            //delete
                                            var id = db.tb_jobs.Where(w => w.job_guid == Getguid).Select(s => s.id).FirstOrDefault();
                                            var parts_sql = db.tb_jobs_parts.Where(w => w.job_id == id).ToList();

                                            for (int i = 0; i < parts_sql.Count; i++)
                                            {
                                                if (idStore == store || User.IsInRole("admin"))
                                                {
                                                    db.tb_jobs_parts.Remove(parts_sql[i]);
                                                    db.SaveChanges();
                                                }
                                            }

                                            //add
                                            for (int t = 0; t <= (col_getParts - 1); t += 9)
                                            {
                                                if (worksheet.Cells[row, col_parts].Text != "" && (t + 2 != (col_getParts - 1)))
                                                {
                                                    var ship_date = worksheet.Cells[row, col_parts + 5].Text != "" ? worksheet.Cells[row, col_parts + 5].Value.ToString() == "00/00/0000" || worksheet.Cells[row, col_parts + 5].Value.ToString() == null ? "00/00/0000" : worksheet.Cells[row, col_parts + 5].Value.ToString() : "00/00/0000";
                                                    var db_location = worksheet.Cells[row, col_parts].Text;
                                                    var db_patrs_no = worksheet.Cells[row, col_parts + 1].Text;
                                                    ship_date = ship_date.Length > 10 ? ship_date.Substring(0, ship_date.IndexOf(" ")) : ship_date;
                                                    obj_new_parts.job_id = id;
                                                    obj_new_parts.parts_repair_location = db_location;
                                                    obj_new_parts.parts_no = worksheet.Cells[row, col_parts + 1].Text;
                                                    obj_new_parts.material_serial_no = worksheet.Cells[row, col_parts + 2].Text;
                                                    obj_new_parts.quantity = worksheet.Cells[row, col_parts + 3].Text != "" ? int.Parse(worksheet.Cells[row, col_parts + 3].Text) : 0;
                                                    obj_new_parts.parts_status = worksheet.Cells[row, col_parts + 4].Text;
                                                    obj_new_parts.ship_date = ship_date != "00/00/0000" && ship_date.IndexOf("00") == -1 ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ship_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone) : (DateTime?)null;
                                                    obj_new_parts.parts_description = worksheet.Cells[row, col_parts + 6].Text;
                                                    obj_new_parts.parts_in_out = worksheet.Cells[row, col_parts + 7].Text;
                                                    obj_new_parts.so_no = worksheet.Cells[row, col_parts + 8].Text;

                                                    if ((idStore == store && text_error == "") || (User.IsInRole("admin") && text_error == ""))
                                                    {
                                                        db.tb_jobs_parts.Add(obj_new_parts);
                                                        db.SaveChanges();
                                                    }
                                                }
                                                col_parts += 9;
                                            }
                                        }
                                    }

                                    if (idStore == store || User.IsInRole("admin"))
                                    {
                                        Guid gid = isNew == false ? id_job.job_guid : new_Guid;
                                        var id = db.tb_jobs.Where(w => w.job_guid == gid).Select(s => s.id).FirstOrDefault();
                                        save_job.Add(new job
                                        {
                                            Id = id == 0 ? " - " : id.ToString(),
                                            Engineer = code_engineer == "" || code_engineer == null ? " - " : engineer == null ? new_enname : engineer.name,
                                            Store = code_store == null || code_store == "" ? " - " : db.tb_store.Where(w => w.code_store == code_store).Select(s => s.site_name).FirstOrDefault(),
                                            Serial = get_service_order == null ? " - " : isNew == false ? id_job.service_order_no : get_service_order,
                                            Update = text_error != "" ? text_error : isNew == false ? "Update" : "Is New"

                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (save_job.Count() > 0)
                                {
                                    save_job.Add(new job
                                    {
                                        Id = "-",
                                        Engineer = "-",
                                        Store = "-",
                                        Serial = error_serial,
                                        Update = "Error เช็คข้อมูล หมายเลขงาน " + error_serial + " ใน file excel"
                                    });
                                }
                                else
                                {
                                    save_job.Add(new job
                                    {
                                        Id = "error_file",
                                    });
                                }

                                string jsonString2 = Newtonsoft.Json.JsonConvert.SerializeObject(save_job);
                                return new ContentResult()
                                {
                                    Content = jsonString2,
                                    ContentType = "application/json"
                                };
                            }

                        }
                    }
                    System.IO.File.Delete(path);
                    ViewData["Data-job"] = save_job.ToList();
                }
                else
                {
                    save_job.Add(new job
                    {
                        Id = "111111111111111111111111111",
                    });
                    string jsonString2 = Newtonsoft.Json.JsonConvert.SerializeObject(save_job);
                    return new ContentResult()
                    {
                        Content = jsonString2,
                        ContentType = "application/json"
                    };
                }
            }
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(save_job);
            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [HttpPost]
        [Authorize]
        public object btn_edit_job(edit_job obj)
        {
            List<validate_all> validates = new List<validate_all>();

            if (obj.Store.ToString() != "" && obj.Store != null) //Fullname
            {
                var checkStore = common.IsNumeric(obj.Store);
                if (checkStore == false) { validates.Add(new validate_all { name_div = "#alert_select_site", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#alert_select_site", text = "กรุณาเลือกศูนย์บริการ" }); }

            if (obj.Engineer.ToString() != "" && obj.Engineer != null) //Fullname
            {
                var checkEngineer = common.IsNumeric(obj.Engineer);
                if (checkEngineer == false) { validates.Add(new validate_all { name_div = "#alert_select_engineer", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#alert_select_engineer", text = "กรุณาเลือกช่าง" }); }

            var data = db.tb_jobs.Where(w => w.id == obj.Id).FirstOrDefault();
            //check site
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.store_id == idStore ? true : false;
            }

            if (validates.Count() == 0 && (User.IsInRole("admin") || isSite))
            {

                var engineer_code = db.tb_engineer.Where(w => w.id == obj.Engineer).Select(s => s.code_engineer).FirstOrDefault();
                var store_code = db.tb_store.Where(w => w.id == obj.Store).Select(s => s.code_store).FirstOrDefault();
                var im_dis = db.tb_districts.Where(w => w.district_id == obj.District).Select(s => s.district_name).FirstOrDefault();
                var im_pro = db.tb_provinces.Where(w => w.province_id == obj.Province).Select(s => s.province_name).FirstOrDefault();

                data.user_update = getName;
                data.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data.customer_fullname = obj.Fullname;
                data.email = obj.Email;
                data.phone_home = obj.Phone_home;
                data.phone_mobile = obj.Phone_mobile;
                data.phone_office = obj.Phone_office;
                data.site_address = obj.Home_address;
                data.village = obj.Village;
                data.moo = obj.Moo;
                data.district = obj.District;
                data.sub_district = obj.Sub_district;
                data.province = obj.Province;
                data.zipcode = obj.Zipcode;
                data.job_category_id = obj.Category;
                data.sub_category_id = obj.Sub_category;
                data.model = obj.Model;
                data.description = obj.Description;
                data.note = obj.Note;
                data.store_code = store_code;
                data.store_id = obj.Store;
                data.engineer_code = engineer_code;
                data.engineer_id = obj.Engineer;
                data.is_change = "System";
                data.im_district = im_dis;
                data.im_province = im_pro;
                data.gender = obj.Gender;
                data.appointment_datetime = obj.Appointment_datetime;
                data.appointment_to_datetime = obj.Appointment_to_datetime;
                data.status_job = obj.Status_job;
                data.customer_lat = obj.customer_lat;
                data.customer_long = obj.customer_long;

                db.tb_jobs.AddOrUpdate(data);
                db.SaveChanges();

                //DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(data.appointment_datetime.Value.ToString("MM dd yyyy")), zone);
                //DateTime tomorrow_date = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(data.appointment_datetime.Value.ToString("MM dd yyyy")).AddDays(1), zone);


                //var data_log = db.tb_jobs_worklog.Where(w => w.jobs_id == data.id && w.appointment_datetime >= start_date && w.appointment_datetime <= tomorrow_date).FirstOrDefault();

                //if (data_log != null)
                //{
                //    data_log.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                //    data_log.user_update = getName;
                //    data_log.is_delete = 0;
                //    data_log.jobs_id = data.id;
                //    data_log.appointment_datetime = data.appointment_datetime;
                //    data_log.appointment_to_datetime = data.appointment_to_datetime;
                //    data_log.status_job = data.status_job;
                //    data_log.job_start = data.job_start;
                //    data_log.job_start_lat = data.job_start_lat;
                //    data_log.job_start_long = data.job_start_long;
                //    data_log.job_repair = data.job_repair;
                //    data_log.job_repair_lat = data.job_repair_lat;
                //    data_log.job_repair_long = data.job_repair_long;
                //    data_log.job_end = data.job_end;
                //    data_log.id_cancel = data.id_cancel;
                //    data_log.reason_cancel = data.reason_cancel;
                //    data_log.cancel_date = data.cancel_date;

                //    db.tb_jobs_worklog.AddOrUpdate(data_log);
                //    db.SaveChanges();
                //}
                //else
                //{
                //    //log job
                //    CommonLib.tb_jobs_worklog noti = new CommonLib.tb_jobs_worklog();
                //    noti.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                //    noti.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                //    noti.user_update = getName;
                //    noti.is_delete = 0;
                //    noti.jobs_id = data.id;
                //    noti.appointment_datetime = data.appointment_datetime;
                //    noti.appointment_to_datetime = data.appointment_to_datetime;
                //    noti.status_job = data.status_job;
                //    noti.job_start = data.job_start;
                //    noti.job_start_lat = data.job_start_lat;
                //    noti.job_start_long = data.job_start_long;
                //    noti.job_repair = data.job_repair;
                //    noti.job_repair_lat = data.job_repair_lat;
                //    noti.job_repair_long = data.job_repair_long;
                //    noti.job_end = data.job_end;
                //    noti.id_cancel = data.id_cancel;
                //    noti.reason_cancel = data.reason_cancel;
                //    noti.cancel_date = data.cancel_date;

                //    db.tb_jobs_worklog.Add(noti);
                //    db.SaveChanges();
                //    //-end log job
                //}

                return true;
            }
            else
            {
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validates);
                return new ContentResult()
                {
                    Content = jsonString,
                    ContentType = "application/json"
                };
            }
        }

        [HttpPost]
        [Authorize]
        public void btn_delete_job(int id)
        {
            var getName = User.Identity.Name;
            var data = db.tb_jobs.Where(w => w.id == id && w.is_delete == 0).FirstOrDefault();
            var isSite = false;
            if (getName != "" && data != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.store_id == idStore ? true : false;
            }

            if ((User.IsInRole("admin") || isSite))
            {
                data.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data.user_update = getName;
                data.is_delete = 1;
                db.tb_jobs.AddOrUpdate(data);
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Authorize]
        public void stamp_voice(int id)
        {
            var getName = User.Identity.Name;
            var data = db.tb_jobs.Where(w => w.id == id && w.is_delete == 0).FirstOrDefault();
            var isSite = false;
            if (getName != "")
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.store_id == idStore ? true : false;
            }

            if ((User.IsInRole("admin") || isSite))
            {
                tb_attachment obj_new = new tb_attachment();
                Guid guid_id = Guid.NewGuid();
                obj_new.guid_attachment = guid_id;
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.user_update = getName;
                obj_new.is_delete = 0;
                obj_new.job_id = id;
                obj_new.part_name = null;
                obj_new.name_file = "ลูกค้าติดต่อไม่ได้";
                obj_new.file_type = 2;
                obj_new.sort = null;
                obj_new.phone = null;
                db.tb_attachment.Add(obj_new);
                db.SaveChanges();
            }
        }

        [HttpGet]
        [Authorize]
        public object btn_search_and_skip(string search, string s_start_date, string s_end_date, int page, int status, int store, int engineer)
        {
            //status 99 = get all
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            var limit = 20;
            DateTime querydate = DateTime.Now.AddYears(10); //Add 10 year

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);


            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);


            var data = new List<all_job>();

            //var job_db = status == 99 && store == 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0).ToList()
            //         : status == 99 && store != 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store).ToList()
            //         : status != 99 && store == 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status).ToList()
            //         : status == 99 && store == 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.engineer_id == engineer).ToList()
            //         : status != 99 && store != 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status && w.store_id == store).ToList()
            //         : status != 99 && store == 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status && w.engineer_id == engineer).ToList()
            //         : status == 99 && store != 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store && w.engineer_id == engineer).ToList()
            //         : db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store && w.engineer_id == engineer && w.status_job == status).ToList();

            //new 2020-03-27
            var job_db = status == 99 && store == 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status == 99 && store != 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status != 99 && store == 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status == 99 && store == 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.engineer_id == engineer && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status != 99 && store != 0 && engineer == 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status && w.store_id == store && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status != 99 && store == 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == status && w.engineer_id == engineer && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : status == 99 && store != 0 && engineer != 0 ? db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store && w.engineer_id == engineer && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null))
                     : db.tb_jobs.Where(w => w.is_delete == 0 && w.store_id == store && w.engineer_id == engineer && w.status_job == status && ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null));

            //var job_and_sur = (from j in job_db
            //                   join s in db.tb_survey on j.job_guid equals s.job_guid
            //                   select new all_job()).ToList();

            //var x = job_db.ToList();

            if (User.IsInRole("admin"))
            {
                var data1 = search == "" ? (from j in job_db
                                                //join s in db.tb_survey on j.job_guid equals s.job_guid
                                            select new all_job()
                                            {
                                                id = j.id,
                                                job_guid = j.job_guid,
                                                job_no = j.service_order_no,
                                                status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                duedate = j.appointment_datetime,
                                                to_duedate = j.appointment_to_datetime,
                                                customer_name = j.customer_fullname,
                                                job_type = j.service_type,
                                                asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                                engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                                mobile = j.phone_mobile,
                                                //count = job_db.Where(w => w.is_delete == 0 && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null).Count(),
                                                count = job_db.Where(w => ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null) && w.is_delete == 0).Count(),
                                                setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) || (j.appointment_datetime.HasValue == true && j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                                negative = db.tb_survey.Where(w => w.job_guid == j.job_guid).Select(s => s.is_negative).FirstOrDefault()
                                            }).ToList()
                                :
                                (from j in job_db
                                     // join s in db.tb_survey on j.job_guid equals s.job_guid
                                 where j.customer_fullname.Contains(search.Trim()) || j.service_order_no.Contains(search.Trim())
                                 select new all_job()
                                 {
                                     id = j.id,
                                     job_guid = j.job_guid,
                                     job_no = j.service_order_no,
                                     status = ((CommonLib.Status_job)j.status_job).ToString(),
                                     duedate = j.appointment_datetime,
                                     to_duedate = j.appointment_to_datetime,
                                     customer_name = j.customer_fullname,
                                     job_type = j.service_type,
                                     asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                     engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                     mobile = j.phone_mobile,
                                     //count = job_db.Where(w => w.is_delete == 0 && (w.update_date >= start_date && (w.customer_fullname.Contains(search.Trim()) || w.service_order_no.Contains(search.Trim())) && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null)).Count(),
                                     count = job_db.Where(w => ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null) && w.is_delete == 0 && (w.customer_fullname.Contains(search.Trim()) || w.service_order_no.Contains(search.Trim()))).Count(),
                                     setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                     negative = db.tb_survey.Where(w => w.job_guid == j.job_guid).Select(s => s.is_negative).FirstOrDefault()
                                 }).ToList();

                var datatest = data1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                data = data1.OrderByDescending(w => w.setDateOrder.Value).Skip(skipPage).Take(limit).ToList();
            }
            else
            {
                var getName = User.Identity.Name;

                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                var data1 = search == "" ? (from j in job_db
                                                //join s in db.tb_survey on j.job_guid equals s.job_guid
                                            where j.store_id == idStore
                                            select new all_job()
                                            {
                                                id = j.id,
                                                job_guid = j.job_guid,
                                                job_no = j.service_order_no,
                                                status = ((CommonLib.Status_job)j.status_job).ToString(),
                                                duedate = j.appointment_datetime,
                                                to_duedate = j.appointment_to_datetime,
                                                customer_name = j.customer_fullname,
                                                job_type = j.service_type,
                                                asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                                engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                                mobile = j.phone_mobile,
                                                setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                                //count = job_db.Where(w => w.is_delete == 0 && w.store_id == idStore && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null).Count()
                                                count = job_db.Where(w => ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null) && w.is_delete == 0 && w.store_id == idStore).Count(),
                                                negative = db.tb_survey.Where(w => w.job_guid == j.job_guid).Select(s => s.is_negative).FirstOrDefault()
                                            })
                                :
                                (from j in job_db
                                     //join s in db.tb_survey on j.job_guid equals s.job_guid
                                 where j.store_id == idStore && (j.customer_fullname.Contains(search.Trim()) || j.service_order_no.Contains(search.Trim()))
                                 select new all_job()
                                 {
                                     id = j.id,
                                     job_guid = j.job_guid,
                                     job_no = j.service_order_no,
                                     status = ((CommonLib.Status_job)j.status_job).ToString(),
                                     duedate = j.appointment_datetime,
                                     to_duedate = j.appointment_to_datetime,
                                     customer_name = j.customer_fullname,
                                     job_type = j.service_type,
                                     asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                     engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                     mobile = j.phone_mobile,
                                     setDateOrder = (j.appointment_datetime == null || j.appointment_to_datetime == null) ? querydate : j.appointment_datetime,
                                     //count = job_db.Where(w => w.is_delete == 0 && w.store_id == idStore && ((w.customer_fullname.Contains(search.Trim()) || w.service_order_no.Contains(search.Trim())) && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null)).Count()
                                     count = job_db.Where(w => ((w.appointment_datetime >= start_date && w.appointment_datetime <= end_date) || w.appointment_datetime == null || w.appointment_to_datetime == null) && w.is_delete == 0 && w.store_id == idStore && (w.customer_fullname.Contains(search.Trim()) || w.service_order_no.Contains(search.Trim()))).Count(),
                                     negative = db.tb_survey.Where(w => w.job_guid == j.job_guid).Select(s => s.is_negative).FirstOrDefault()
                                 });

                var datatest = data1.Count();
                limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;
                data = data1.OrderByDescending(w => w.setDateOrder.Value).Skip(skipPage).Take(limit).ToList();
            }

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [HttpGet]
        [Authorize]
        public object get_descriptionengineer(int engineer, string date)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            var data = new List<CommonLib.tb_jobs>();

            if (User.IsInRole("admin"))
            {
                data = db.tb_jobs.Where(w => w.is_delete == 0 && w.engineer_id == engineer
                                                                    && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date)).ToList();
            }
            else
            {
                var getName = User.Identity.Name;

                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                data = db.tb_jobs.Where(w => w.is_delete == 0 && w.engineer_id == engineer && w.store_id == idStore
                                                                    && (w.appointment_datetime >= start_date && w.appointment_datetime <= end_date)).ToList();
            }

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

    }
}