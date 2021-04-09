using CommonLib;
using s_tracking_mobile.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace s_tracking_mobile.Controllers
{
    public class tController : Controller
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        public ActionResult Index(string jobsid)
        {
            if (jobsid != null && jobsid.Trim() != "")
            {
                var check_date = db.tb_jobs.Where(w => w.service_order_no == jobsid && w.is_delete == 0).FirstOrDefault();
                if (check_date != null)
                {
                    var engineer_data = (from e in db.tb_engineer where e.is_delete == 0 && check_date.engineer_id == e.id select e);
                    var store_data = db.tb_store.Where(w => w.id == engineer_data.FirstOrDefault().site_id && w.is_delete == 0).FirstOrDefault();
                    if (Convert.ToDateTime(check_date.appointment_datetime).Date == DateTime.Today.Date && check_date.appointment_to_datetime != null && store_data != null)
                    {
                        var data = (from j in db.tb_jobs
                                    where j.is_delete == 0 && j.service_order_no == jobsid
                                    select new index
                                    {
                                        //phone_mobile = j.phone_mobile,
                                        service_order_no = j.service_order_no,
                                        appointment_datetime = j.appointment_datetime,
                                        appointment_to_datetime = j.appointment_to_datetime,
                                        customer_lat = j.customer_lat,
                                        customer_long = j.customer_long,
                                        customer_fullname = j.customer_fullname,
                                        customer_mobile = j.phone_mobile,
                                        status_job = j.status_job,
                                        engi = engineer_data.FirstOrDefault(),
                                        phone_office = store_data.tel1 != null ? store_data.tel1 : store_data.tel2 != null ? store_data.tel2 : store_data.tel3
                                    }).FirstOrDefault();

                        if (data.status_job != 2 && data.status_job != 4)
                        {
                            ViewData["job_engi"] = data;
                            StandardController service_con = new StandardController();
                            string key_azure = service_con.GetKeyAzure();
                            ViewData["Key_Azure"] = key_azure;

                            //get IP , Stamp
                            var ip_client = byi_common.common.GetIPAddress();
                            check_date.ip_customer = ip_client;
                            check_date.date_customer = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                            db.tb_jobs.AddOrUpdate(check_date);
                            db.SaveChanges();

                            return View();
                        }
                        return View("Error");
                    }
                    return View("Error");
                }
                return View("Error");
            }
            return View("Error");
        }

        public object edit(edit_loca data)
        {
            string service = Convert.ToString(data.order_no);
            string lati = Convert.ToString(data.new_lati);
            string longti = Convert.ToString(data.new_longti);
            var job = db.tb_jobs.Where(w => w.service_order_no == service && w.is_delete == 0).FirstOrDefault();
            if (job != null)
            {
                job.customer_lat = lati;
                job.customer_long = longti;
                job.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                db.tb_jobs.AddOrUpdate(job);
                db.SaveChanges();
            }
            return null;
        }
    }
}
