using CommonLib;
using OfficeOpenXml;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace s_tracking_mobile.Controllers
{
    [Authorize(Roles = "admin")]
    public class DashboardController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // GET: Dashboard
        public ActionResult Survey(int site = 0, string str_start = "", string str_end = "")
        {
            if (str_start == "") str_start = now.Date.AddDays(-29).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);
            end = end.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            if (s_check && e_check)
            {
                var ls_date = new List<DateTime>();
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    ls_date.Add(day);
                }

                if (ls_date.Count <= 90)
                {


                    var job2 = (from p in db.tb_jobs
                                where p.is_delete == 0 && p.status_job == 3 && p.appointment_datetime >= start && p.appointment_datetime <= end
                                select new
                                {
                                    p.appointment_to_datetime,
                                    p.appointment_datetime,
                                    p.job_guid,
                                    p.store_id,

                                    survey = (from s in db.tb_survey
                                              where s.is_delete == 0 && s.job_guid == p.job_guid
                                              select new
                                              {
                                                  s.source,
                                                  s.is_negative,
                                                  s.create_date

                                              }).FirstOrDefault()
                                });

                    //var job = db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == 3 && w.appointment_datetime >= start && w.appointment_datetime <= end);
                    //var ls_guid = job.Select(s => s.job_guid).ToList();
                    //var survey = db.tb_survey.Where(w => w.is_delete == 0 && ls_guid.Contains(w.job_guid) && w.create_date >= start && w.create_date <= end);
                    var store = db.tb_store.Where(w => w.is_delete == 0);
                    if (site > 0)
                    {
                        //job = job.Where(w => w.store_id == site);
                        store = store.Where(w => w.id == site);
                        job2 = job2.Where(w => w.store_id == site);
                    }


                    List<int> ls_job = new List<int>();
                    List<int> ls_survey = new List<int>();
                    List<int> ls_positive = new List<int>();
                    List<int> ls_negative = new List<int>();
                    List<DateTime> lb_date = new List<DateTime>();

                    //foreach (var date in ls_date)
                    //{
                    //    DateTime plus_day = date.AddDays(1);

                    //    var day_job = job.Where(w => w.appointment_datetime >= date && w.appointment_datetime < plus_day).Count();
                    //    var day_sur = survey.Where(w => w.create_date >= date && w.create_date < plus_day).Count();
                    //    var day_posi = survey.Where(w => w.create_date >= date && w.create_date < plus_day && (w.is_negative == 0 || w.is_negative == null)).Count();
                    //    var day_nega = survey.Where(w => w.create_date >= date && w.create_date < plus_day && w.is_negative == 1).Count();

                    //    if (day_job > 0 || day_sur > 0 || day_posi > 0 || day_nega > 0)
                    //    {
                    //        ls_job.Add(day_job);
                    //        ls_survey.Add(day_sur);
                    //        ls_positive.Add(day_posi);
                    //        ls_negative.Add(day_nega);
                    //        lb_date.Add(date);
                    //    }         
                    //}


                    foreach (var date in ls_date)
                    {
                        DateTime plus_day = date.AddDays(1);

                        var day_job = job2.Where(w => w.appointment_datetime >= date && w.appointment_datetime < plus_day).Count();
                        var day_sur = job2.Where(w => w.survey != null && w.survey.create_date >= date && w.survey.create_date < plus_day).Count();
                        var day_posi = job2.Where(w => w.survey != null && w.survey.create_date >= date && w.survey.create_date < plus_day && (w.survey.is_negative == 0 || w.survey.is_negative == null)).Count();
                        var day_nega = job2.Where(w => w.survey != null && w.survey.create_date >= date && w.survey.create_date < plus_day && w.survey.is_negative == 1).Count();

                        if (day_job > 0 || day_sur > 0 || day_posi > 0 || day_nega > 0)
                        {
                            ls_job.Add(day_job);
                            ls_survey.Add(day_sur);
                            ls_positive.Add(day_posi);
                            ls_negative.Add(day_nega);
                            lb_date.Add(date);
                        }
                    }


                    var sep_site = (from s in store
                                    select new Result_Dashboard_Survey
                                    {
                                        site = s.site_name,
                                        job = job2.Where(w => w.store_id == s.id).Count(),
                                        survey = job2.Where(w => w.store_id == s.id && w.survey != null).Count(),
                                        positive = job2.Where(w => w.store_id == s.id && w.survey != null && (w.survey.is_negative == 0 || w.survey.is_negative == null)).Count(),
                                        negative = job2.Where(w => w.store_id == s.id && w.survey != null && w.survey.is_negative == 1).Count(),
                                    }).ToList();


                    //var by_site = (from j in job
                    //               select new Dashboard_Survey
                    //               {
                    //                   id = j.id,
                    //                   job_guid = j.job_guid,
                    //                   site = j.store_id,
                    //                   survey = survey.Where(w => w.job_guid == j.job_guid).Count(),
                    //                   positive = survey.Where(w => w.job_guid == j.job_guid && (w.is_negative == 0 || w.is_negative == null)).Count(),
                    //                   negative = survey.Where(w => w.job_guid == j.job_guid && w.is_negative == 1).Count()
                    //               });

                    //var result = (from s in store
                    //              select new Result_Dashboard_Survey
                    //              {
                    //                  site = s.site_name,
                    //                  job = by_site.Where(w => w.site == s.id).Count(),
                    //                  survey = by_site.Where(w => w.site == s.id).Select(se => se.survey).Sum(),
                    //                  positive = by_site.Where(w => w.site == s.id).Select(se => se.positive).Sum(),
                    //                  negative = by_site.Where(w => w.site == s.id).Select(se => se.negative).Sum()
                    //              }).ToList();


                    JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
                    string sls_job = jsSerializer.Serialize(ls_job);
                    string sls_survey = jsSerializer.Serialize(ls_survey);
                    string sls_positive = jsSerializer.Serialize(ls_positive);
                    string sls_negative = jsSerializer.Serialize(ls_negative);


                    //ViewData["all_count"] = jsonobj;
                    ViewData["all_site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();

                    //ViewData["site_count"] = result;
                    ViewData["site_count"] = sep_site;
                    ViewData["ls_job"] = sls_job;
                    ViewData["ls_survey"] = sls_survey;
                    ViewData["ls_positive"] = sls_positive;
                    ViewData["ls_negative"] = sls_negative;

                    ViewData["ls_date"] = lb_date;

                    ViewData["start"] = start.ToString("yyyy/MM/dd");

                    ViewData["input_start"] = start.ToString("dd/MM/yyyy");
                    ViewData["input_end"] = end.ToString("dd/MM/yyyy");
                    ViewData["count_date"] = ls_date.Count;
                    ViewData["site"] = site;

                    ViewData["t_job"] = ls_job.Sum();
                    ViewData["t_survey"] = ls_survey.Sum();
                    ViewData["t_positive"] = ls_positive.Sum();
                    ViewData["t_negative"] = ls_negative.Sum();

                    return View();
                }
                else
                {
                    ViewData["out_date"] = 1;
                    return Survey();
                }
            }
            else
            {
                return View("Error");
            }


        }

        public void SurveyExport(int site = 0, string str_start = "", string str_end = "")
        {
            if (str_start == "") str_start = now.Date.AddDays(-29).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);

            if (s_check && e_check)
            {
                var ls_date = new List<DateTime>();
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    ls_date.Add(day);
                }

                if (ls_date.Count <= 90)
                {

                    var job2 = (from p in db.tb_jobs
                                where p.is_delete == 0 && p.status_job == 3 && p.appointment_datetime >= start && p.appointment_datetime <= end
                                select new
                                {
                                    p.appointment_to_datetime,
                                    p.appointment_datetime,
                                    p.job_guid,
                                    p.store_id,

                                    survey = (from s in db.tb_survey
                                              where s.is_delete == 0 && s.job_guid == p.job_guid
                                              select new
                                              {
                                                  s.source,
                                                  s.is_negative,
                                                  s.create_date

                                              }).FirstOrDefault()
                                });


                    var store = db.tb_store.Where(w => w.is_delete == 0);
                    if (site > 0)
                    {

                        store = store.Where(w => w.id == site);
                        job2 = job2.Where(w => w.store_id == site);
                    }

                    var result = (from s in store
                                  select new Result_Dashboard_Survey
                                  {
                                      site = s.site_name,
                                      job = job2.Where(w => w.store_id == s.id).Count(),
                                      survey = job2.Where(w => w.store_id == s.id && w.survey != null).Count(),
                                      positive = job2.Where(w => w.store_id == s.id && w.survey != null && (w.survey.is_negative == 0 || w.survey.is_negative == null)).Count(),
                                      negative = job2.Where(w => w.store_id == s.id && w.survey != null && w.survey.is_negative == 1).Count(),
                                  }).ToList();


                    //var job = db.tb_jobs.Where(w => w.is_delete == 0 && w.status_job == 3 && w.appointment_datetime >= start && w.appointment_datetime <= end);
                    //var ls_guid = job.Select(s => s.job_guid).ToList();
                    //var survey = db.tb_survey.Where(w => w.is_delete == 0 && ls_guid.Contains(w.job_guid) && w.create_date >= start && w.create_date <= end);
                    //var store = db.tb_store.Where(w => w.is_delete == 0);
                    //if (site > 0)
                    //{
                    //    job = job.Where(w => w.store_id == site);
                    //    store = store.Where(w => w.id == site);
                    //}

                    //var by_site = (from j in job
                    //               select new Dashboard_Survey
                    //               {
                    //                   id = j.id,
                    //                   job_guid = j.job_guid,
                    //                   site = j.store_id,
                    //                   survey = survey.Where(w => w.job_guid == j.job_guid).Count(),
                    //                   positive = survey.Where(w => w.job_guid == j.job_guid && (w.is_negative == 0 || w.is_negative == null)).Count(),
                    //                   negative = survey.Where(w => w.job_guid == j.job_guid && w.is_negative == 1).Count()
                    //               });

                    //var result = (from s in store
                    //              select new Result_Dashboard_Survey
                    //              {
                    //                  site = s.site_name,
                    //                  job = by_site.Where(w => w.site == s.id).Count(),
                    //                  survey = by_site.Where(w => w.site == s.id).Select(se => se.survey).Sum(),
                    //                  positive = by_site.Where(w => w.site == s.id).Select(se => se.positive).Sum(),
                    //                  negative = by_site.Where(w => w.site == s.id).Select(se => se.negative).Sum()
                    //              }).ToList();


                    ExcelPackage excel = new ExcelPackage();
                    var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                    workSheet.Row(1).Style.Font.Bold = true;
                    workSheet.Cells[1, 1].Value = "ศูนย์บริการ";
                    workSheet.Cells[1, 2].Value = "งานเสร็จ";
                    workSheet.Cells[1, 3].Value = "แบบสอบถาม";
                    workSheet.Cells[1, 4].Value = "คำตอบเชิงบวก";
                    workSheet.Cells[1, 5].Value = "คำตอบเชิงลบ";


                    int recordIndex = 2;
                    foreach (var val in result)
                    {

                        workSheet.Cells[recordIndex, 1].Value = val.site;
                        workSheet.Cells[recordIndex, 2].Value = val.job == null ? 0 : val.job;
                        workSheet.Cells[recordIndex, 3].Value = val.survey == null ? 0 : val.survey;
                        workSheet.Cells[recordIndex, 4].Value = val.positive == null ? 0 : val.positive;
                        workSheet.Cells[recordIndex, 5].Value = val.negative == null ? 0 : val.negative;
                        recordIndex++;
                    }

                    for (int i = 1; i <= 5; i++)
                    {
                        workSheet.Column(i).AutoFit();
                    }

                    var name_file = "ReportSurvey " + start.ToString("dd/MM/yyyy") + " - " + end.ToString("dd/MM/yyyy") + ".xlsx";
                    using (var memoryStream = new MemoryStream())
                    {
                        excel.SaveAs(memoryStream);
                        byte[] bytesInStream = memoryStream.ToArray();
                        Response.ContentType = "application/vnd.ms-excel";
                        Response.AddHeader("content-disposition", "attachment;    filename=" + name_file);
                        Response.BinaryWrite(bytesInStream);
                        Response.Flush();
                        Response.End();
                    }


                }
                else
                {
                }
            }

        }


        public ActionResult Jobs(int site = 0, string str_start = "", string str_end = "", string type = "")
        {
            if (str_start == "") str_start = now.Date.AddDays(-29).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);
            end = end.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            ViewData["type"] = type;

            if (s_check && e_check)
            {
                var ls_date = new List<DateTime>();
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    ls_date.Add(day);
                }

                if (ls_date.Count <= 90)
                {

                    var job = db.tb_jobs.Where(w => w.is_delete == 0 && w.appointment_datetime >= start && w.appointment_datetime <= end);
                    if (type != "") job = job.Where(w => w.service_type == type);
                    var store = db.tb_store.Where(w => w.is_delete == 0);
                    if (site > 0)
                    {
                        job = job.Where(w => w.store_id == site);
                        store = store.Where(w => w.id == site);
                    }


                    List<int> all_job = new List<int>();
                    List<int> done_job = new List<int>();
                    List<int> cancel_job = new List<int>();
                    List<DateTime> lb_date = new List<DateTime>();

                    foreach (var date in ls_date)
                    {
                        DateTime plus_day = date.AddDays(1);

                        var day_job = job.Where(w => w.appointment_datetime >= date && w.appointment_datetime < plus_day).Count();
                        var day_done = job.Where(w => w.appointment_datetime >= date && w.appointment_datetime < plus_day && w.status_job == 3).Count();
                        var day_cancel = job.Where(w => w.appointment_datetime >= date && w.appointment_datetime < plus_day && (w.status_job == 2 || w.status_job == 4)).Count();

                        if (day_job > 0 || day_done > 0 || day_cancel > 0)
                        {
                            all_job.Add(day_job);
                            done_job.Add(day_done);
                            cancel_job.Add(day_cancel);
                            lb_date.Add(date);
                        }


                    }

                    var result = (from s in store
                                  select new Result_Dashboard_Job
                                  {
                                      site = s.site_name,
                                      all_job = job.Where(w => w.store_id == s.id).Count(),
                                      done_job = job.Where(w => w.store_id == s.id && w.status_job == 3).Count(),
                                      cancel_job = job.Where(w => w.store_id == s.id && (w.status_job == 2 || w.status_job == 4)).Count()
                                  }).ToList();

                    JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
                    string sall_job = jsSerializer.Serialize(all_job);
                    string sdone_job = jsSerializer.Serialize(done_job);
                    string scancel_job = jsSerializer.Serialize(cancel_job);

                    if (site > 0)
                    {
                        var job_sms = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start && j.appointment_datetime <= end) select j).ToList();
                        ViewData["open_sms"] = (double)job_sms.Where(w => w.store_id == site && w.date_customer != null && (w.date_customer >= start && w.date_customer <= end)).Count();

                        ViewData["ls_sms"] = (double)(from l in db.tb_log where l.job_id != null && l.send_date >= start && l.send_date < end && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start && j.appointment_datetime < end) select j.id).Count() > 0 select l).Count();
                    }
                    else
                    {
                        var job_sms = (from j in db.tb_jobs where j.is_delete == 0 && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start && j.appointment_datetime <= end) select j).ToList();
                        ViewData["open_sms"] = (double)job_sms.Where(w => w.date_customer != null && (w.date_customer >= start && w.date_customer <= end)).Count();

                        ViewData["ls_sms"] = (double)(from l in db.tb_log where l.job_id != null && l.send_date >= start && l.send_date < end && (from j in db.tb_jobs where j.is_delete == 0 && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start && j.appointment_datetime < end) select j.id).Count() > 0 select l).Count();
                    }
                                        

                    ViewData["all_site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
                    ViewData["site_count"] = result;
                    ViewData["sall_job"] = sall_job;
                    ViewData["sdone_job"] = sdone_job;
                    ViewData["scancel_job"] = scancel_job;

                    ViewData["ls_date"] = lb_date;

                    ViewData["start"] = start.ToString("yyyy/MM/dd");

                    ViewData["input_start"] = start.ToString("dd/MM/yyyy");
                    ViewData["input_end"] = end.ToString("dd/MM/yyyy");
                    ViewData["count_date"] = ls_date.Count;
                    ViewData["site"] = site;

                    ViewData["count_all_job"] = all_job.Sum();
                    ViewData["count_done_job"] = done_job.Sum();
                    ViewData["count_cancel_job"] = cancel_job.Sum();

                    var group_cancel = job.Where(w => (w.status_job == 2 || w.status_job == 4) && w.id_cancel != null ).GroupBy(g => g.id_cancel).OrderByDescending(s => s.Count()).ToList();
                    int max = 0;
                    foreach (var data in group_cancel)
                    {
                        max += data.Count();
                    }
                    ViewData["negative_type"] = group_cancel;
                    ViewData["percent"] = max > 0 ? (double)100 / max : 0;
                    return View();
                }
                else
                {
                    ViewData["out_date"] = 1;
                    return Jobs();
                }
            }
            else
            {
                return View("Error");
            }
        }


        public void JobExport(int site = 0, string str_start = "", string str_end = "")
        {
            if (str_start == "") str_start = now.Date.AddDays(-29).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);

            if (s_check && e_check)
            {
                var ls_date = new List<DateTime>();
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    ls_date.Add(day);
                }

                if (ls_date.Count <= 90)
                {
                    var job = db.tb_jobs.Where(w => w.is_delete == 0 && w.appointment_datetime >= start && w.appointment_datetime <= end);
                    var store = db.tb_store.Where(w => w.is_delete == 0);
                    if (site > 0)
                    {
                        job = job.Where(w => w.store_id == site);
                        store = store.Where(w => w.id == site);
                    }

                    var result = (from s in store
                                  select new Result_Dashboard_Job
                                  {
                                      site = s.site_name,
                                      all_job = job.Where(w => w.store_id == s.id).Count(),
                                      done_job = job.Where(w => w.store_id == s.id && w.status_job == 3).Count(),
                                      cancel_job = job.Where(w => w.store_id == s.id && (w.status_job == 2 || w.status_job == 4)).Count()
                                  }).ToList();


                    ExcelPackage excel = new ExcelPackage();
                    var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                    workSheet.Row(1).Style.Font.Bold = true;
                    workSheet.Cells[1, 1].Value = "ศูนย์บริการ";
                    workSheet.Cells[1, 2].Value = "งานทั้งหมด";
                    workSheet.Cells[1, 3].Value = "งานสำเร็จ";
                    workSheet.Cells[1, 4].Value = "งานยกเลิก";



                    int recordIndex = 2;
                    foreach (var val in result)
                    {

                        workSheet.Cells[recordIndex, 1].Value = val.site;
                        workSheet.Cells[recordIndex, 2].Value = val.all_job == null ? 0 : val.all_job;
                        workSheet.Cells[recordIndex, 3].Value = val.done_job == null ? 0 : val.done_job;
                        workSheet.Cells[recordIndex, 4].Value = val.cancel_job == null ? 0 : val.cancel_job;

                        recordIndex++;
                    }

                    for (int i = 1; i <= 5; i++)
                    {
                        workSheet.Column(i).AutoFit();
                    }

                    var name_file = "ReportJob " + start.ToString("dd/MM/yyyy") + " - " + end.ToString("dd/MM/yyyy") + ".xlsx";
                    using (var memoryStream = new MemoryStream())
                    {
                        excel.SaveAs(memoryStream);
                        byte[] bytesInStream = memoryStream.ToArray();
                        Response.ContentType = "application/vnd.ms-excel";
                        Response.AddHeader("content-disposition", "attachment;    filename=" + name_file);
                        Response.BinaryWrite(bytesInStream);
                        Response.Flush();
                        Response.End();
                    }


                }
                else
                {
                }
            }

        }

    }
}