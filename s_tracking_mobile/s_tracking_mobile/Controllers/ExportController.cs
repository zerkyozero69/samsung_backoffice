using CommonLib;
using OfficeOpenXml;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class ExportController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        public object export_file()
        {
            var path_azure = ConfigurationManager.AppSettings["Azure_url"];
            var getName = User.Identity.Name;
            var CheckUser = Membership.GetUser(getName).ProviderUserKey;
            var idStore = db.tb_mapping_store.Where(w => w.account_guid.ToString() == CheckUser.ToString()).Select(s => s.site_id).FirstOrDefault();
            var page = Request["id"];

            StandardController service_con = new StandardController();
            string key_azure = service_con.GetKeyAzure(1440);

            if (page == "3")
            {
                var search = Request["search"];
                var s_start_date = Request["s_date"];
                var s_end_date = Request["e_date"];
                var status = int.Parse(Request["status"]);
                var store = int.Parse(Request["store"]);
                var engineer = int.Parse(Request["engineer"]);
                                

                DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_start_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

                start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
                end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

                var data = new List<all_job_export>();

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

                // dashboard job export
                var service_type = Request["type"];
                if (service_type != null) job_db = job_db.Where(w => w.service_type == service_type);
                //

                if (User.IsInRole("admin"))
                {
                    data = search == "" ? (from j in job_db
                                           orderby j.update_date descending
                                           select new all_job_export()
                                           {
                                               job_type = j.service_type,
                                               job_no = j.service_order_no,
                                               status = ((CommonLib.Status_job)j.status_job).ToString().Replace("_"," "),
                                               duedate = j.appointment_datetime,
                                               to_duedate = j.appointment_to_datetime,
                                               customer_name = j.customer_fullname,
                                               customer_mobile = j.phone_mobile,
                                               customer_home = j.phone_home,
                                               customer_office = j.phone_office,
                                               customer_street = j.street,
                                               customer_district = j.im_district,
                                               customer_province = j.im_province,
                                               customer_zipcode = j.zipcode.ToString(),
                                               customer_lat = j.customer_lat,
                                               customer_lng = j.customer_long,
                                               asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                               engineer_code = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.code_engineer).FirstOrDefault(),
                                               engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                               site_code = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.code_store).FirstOrDefault(),
                                               site_name = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.site_name).FirstOrDefault(),
                                               description_breakdown = j.description_breakdown,
                                               description_parts = j.description_parts,
                                               job_start = j.job_start,
                                               job_repair = j.job_repair,
                                               job_end = j.job_end,
                                               engineer_signature_path = j.signature_path,
                                               engineer_signature_name = j.signature_name,
                                               reason_cancel = j.reason_cancel,
                                               cancel_date = j.cancel_date,
                                               picture = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 1 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                               voice = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 2 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                               customer_date = j.date_customer,
                                               customer_ip = j.ip_customer,
                                               //sms_send = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).FirstOrDefault(),
                                               sms_send_new = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).Select(s => s.send_date).FirstOrDefault(),
                                               customer_prefer_date = j.customer_prefer_date,
                                               customer_perfer_time = j.customer_perfer_time,
                                               skip_status = j.skip_status,
                                               skip_note = j.skip_note,
                                               is_used_coupon = j.is_used_coupon,
                                               is_code_coupon = db.tb_coupon.Where(c=>c.id== j.id_coupon).Select(c=>c.code_coupon).FirstOrDefault(),
                                               req_coupon_date = j.req_coupon_date,
                                               used_coupon_date = j.used_coupon_date

                                           }).ToList()
                                    :
                                    (from j in job_db
                                     where j.customer_fullname.Contains(search.Trim()) || j.service_order_no.Contains(search.Trim())
                                     orderby j.update_date descending
                                     select new all_job_export()
                                     {
                                         job_type = j.service_type,
                                         job_no = j.service_order_no,
                                         status = ((CommonLib.Status_job)j.status_job).ToString().Replace("_", " "),
                                         duedate = j.appointment_datetime,
                                         to_duedate = j.appointment_to_datetime,
                                         customer_name = j.customer_fullname,
                                         customer_mobile = j.phone_mobile,
                                         customer_home = j.phone_home,
                                         customer_office = j.phone_office,
                                         customer_street = j.street,
                                         customer_district = j.im_district,
                                         customer_province = j.im_province,
                                         customer_zipcode = j.zipcode.ToString(),
                                         customer_lat = j.customer_lat,
                                         customer_lng = j.customer_long,
                                         asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                         engineer_code = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.code_engineer).FirstOrDefault(),
                                         engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                         site_code = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.code_store).FirstOrDefault(),
                                         site_name = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.site_name).FirstOrDefault(),
                                         description_breakdown = j.description_breakdown,
                                         description_parts = j.description_parts,
                                         job_start = j.job_start,
                                         job_repair = j.job_repair,
                                         job_end = j.job_end,
                                         engineer_signature_path = j.signature_path,
                                         engineer_signature_name = j.signature_name,
                                         reason_cancel = j.reason_cancel,
                                         cancel_date = j.cancel_date,
                                         picture = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 1 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                         voice = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 2 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                         customer_date = j.date_customer,
                                         customer_ip = j.ip_customer,
                                         //sms_send = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).FirstOrDefault(),
                                         sms_send_new = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).Select(s => s.send_date).FirstOrDefault(),
                                         customer_prefer_date = j.customer_prefer_date,
                                         customer_perfer_time = j.customer_perfer_time,
                                         skip_status = j.skip_status,
                                         skip_note = j.skip_note,
                                         is_used_coupon = j.is_used_coupon,
                                         is_code_coupon = db.tb_coupon.Where(c => c.id == j.id_coupon).Select(c => c.code_coupon).FirstOrDefault(),
                                         req_coupon_date = j.req_coupon_date,
                                         used_coupon_date = j.used_coupon_date

                                     }).ToList();
                }
                else
                {
                    data = search == "" ? (from j in job_db
                                           where j.store_id == idStore
                                           orderby j.update_date descending
                                           select new all_job_export()
                                           {
                                               job_type = j.service_type,
                                               job_no = j.service_order_no,
                                               status = ((CommonLib.Status_job)j.status_job).ToString().Replace("_", " "),
                                               duedate = j.appointment_datetime,
                                               to_duedate = j.appointment_to_datetime,
                                               customer_name = j.customer_fullname,
                                               customer_mobile = j.phone_mobile,
                                               customer_home = j.phone_home,
                                               customer_office = j.phone_office,
                                               customer_street = j.street,
                                               customer_district = j.im_district,
                                               customer_province = j.im_province,
                                               customer_zipcode = j.zipcode.ToString(),
                                               customer_lat = j.customer_lat,
                                               customer_lng = j.customer_long,
                                               asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                               engineer_code = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.code_engineer).FirstOrDefault(),
                                               engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                               site_code = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.code_store).FirstOrDefault(),
                                               site_name = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.site_name).FirstOrDefault(),
                                               description_breakdown = j.description_breakdown,
                                               description_parts = j.description_parts,
                                               job_start = j.job_start,
                                               job_repair = j.job_repair,
                                               job_end = j.job_end,
                                               engineer_signature_path = j.signature_path,
                                               engineer_signature_name = j.signature_name,
                                               reason_cancel = j.reason_cancel,
                                               cancel_date = j.cancel_date,
                                               picture = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 1 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                               voice = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 2 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                               customer_date = j.date_customer,
                                               customer_ip = j.ip_customer,
                                               //sms_send = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).FirstOrDefault(),
                                               sms_send_new = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).Select(s => s.send_date).FirstOrDefault(),
                                               customer_prefer_date = j.customer_prefer_date,
                                               customer_perfer_time = j.customer_perfer_time,
                                               skip_status = j.skip_status,
                                               skip_note = j.skip_note,
                                               is_used_coupon = j.is_used_coupon,
                                               is_code_coupon = db.tb_coupon.Where(c => c.id == j.id_coupon).Select(c => c.code_coupon).FirstOrDefault(),
                                               req_coupon_date = j.req_coupon_date,
                                               used_coupon_date = j.used_coupon_date
                                           }).ToList()
                                    :
                                    (from j in job_db
                                     where j.store_id == idStore && (j.customer_fullname.Contains(search.Trim()) || j.service_order_no.Contains(search.Trim()))
                                     orderby j.update_date descending
                                     select new all_job_export()
                                     {
                                         job_type = j.service_type,
                                         job_no = j.service_order_no,
                                         status = ((CommonLib.Status_job)j.status_job).ToString().Replace("_", " "),
                                         duedate = j.appointment_datetime,
                                         to_duedate = j.appointment_to_datetime,
                                         customer_name = j.customer_fullname,
                                         customer_mobile = j.phone_mobile,
                                         customer_home = j.phone_home,
                                         customer_office = j.phone_office,
                                         customer_street = j.street,
                                         customer_district = j.im_district,
                                         customer_province = j.im_province,
                                         customer_zipcode = j.zipcode.ToString(),
                                         customer_lat = j.customer_lat,
                                         customer_lng = j.customer_long,
                                         asset = (from cat in db.tb_jobsl_category where cat.is_delete == 0 && cat.id == j.sub_category_id select cat.name).FirstOrDefault(),
                                         engineer_code = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.code_engineer).FirstOrDefault(),
                                         engineer_name = (from eng in db.tb_engineer where eng.is_delete == 0 && eng.id == j.engineer_id select eng.engineer_name).FirstOrDefault(),
                                         site_code = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.code_store).FirstOrDefault(),
                                         site_name = (from site in db.tb_store where site.is_delete == 0 && site.id == j.store_id select site.site_name).FirstOrDefault(),
                                         description_breakdown = j.description_breakdown,
                                         description_parts = j.description_parts,
                                         job_start = j.job_start,
                                         job_repair = j.job_repair,
                                         job_end = j.job_end,
                                         engineer_signature_path = j.signature_path,
                                         engineer_signature_name = j.signature_name,
                                         reason_cancel = j.reason_cancel,
                                         cancel_date = j.cancel_date,
                                         picture = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 1 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                         voice = (from att in db.tb_attachment where att.is_delete == 0 && att.job_id == j.id && att.file_type == 2 select new picture_voice() { path = att.part_name, name = att.name_file, sort = att.sort }).ToList(),
                                         customer_date = j.date_customer,
                                         customer_ip = j.ip_customer,
                                         //sms_send = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).FirstOrDefault(),
                                         sms_send_new = db.tb_log.Where(w => j.id == w.job_id).OrderByDescending(o => o.send_date).Select(s => s.send_date).FirstOrDefault(),
                                         customer_prefer_date = j.customer_prefer_date,
                                         customer_perfer_time = j.customer_perfer_time,
                                         skip_status = j.skip_status,
                                         skip_note = j.skip_note,
                                         is_used_coupon = j.is_used_coupon,
                                         is_code_coupon = db.tb_coupon.Where(c => c.id == j.id_coupon).Select(c => c.code_coupon).FirstOrDefault(),
                                         req_coupon_date = j.req_coupon_date,
                                         used_coupon_date = j.used_coupon_date
                                     }).ToList();
                }

                ExcelPackage excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");

                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "หมายเลขงาน";
                workSheet.Cells[1, 2].Value = "สถานะงาน";
                workSheet.Cells[1, 3].Value = "รหัสศูนย์บริการ";
                workSheet.Cells[1, 4].Value = "ชื่อศูนย์บริการ";
                workSheet.Cells[1, 5].Value = "รหัสช่างซ่อม";
                workSheet.Cells[1, 6].Value = "ชื่อช่างซ่อม";
                workSheet.Cells[1, 7].Value = "ชื่อลูกค้า";
                workSheet.Cells[1, 8].Value = "เบอร์โทรศัพท์ลูกค้า(Mobile)";
                workSheet.Cells[1, 9].Value = "เบอร์โทรศัพท์ลูกค้า(Home)";
                workSheet.Cells[1, 10].Value = "เบอร์โทรศัพท์ลูกค้า(Office)";
                workSheet.Cells[1, 11].Value = "ที่อยู่ลูกค้า";
                workSheet.Cells[1, 12].Value = "Latitude ลูกค้า";
                workSheet.Cells[1, 13].Value = "Longitude ลูกค้า";
                //new 2020-03-25
                workSheet.Cells[1, 14].Value = "Customer Prefer Date";
                workSheet.Cells[1, 15].Value = "Customer Prefer Date Result";
                //end-new
                workSheet.Cells[1, 16].Value = "สินค้า";
                workSheet.Cells[1, 17].Value = "รายละเอียดอาการเสีย";
                workSheet.Cells[1, 18].Value = "รายละเอียดเกี่ยวกับอะไหล่";
                workSheet.Cells[1, 19].Value = "วันที่เข้าซ่อม / เวลา";
                workSheet.Cells[1, 20].Value = "Job Start";
                workSheet.Cells[1, 21].Value = "ใช้เวลาเดินทาง";
                workSheet.Cells[1, 22].Value = "Job Repair";
                workSheet.Cells[1, 23].Value = "Job End";
                workSheet.Cells[1, 24].Value = "ใช้เวลาทำงาน";
                workSheet.Cells[1, 25].Value = "เหตุผลในการยกเลิก";
                workSheet.Cells[1, 26].Value = "เวลาที่ช่างยกเลิก";
                workSheet.Cells[1, 27].Value = "ลายเซ็นลูกค้า";
                workSheet.Cells[1, 28].Value = "ภาพถ่ายงานซ่อม 1";
                workSheet.Cells[1, 29].Value = "ภาพถ่ายงานซ่อม 2";
                workSheet.Cells[1, 30].Value = "ภาพถ่ายงานซ่อม 3";
                workSheet.Cells[1, 31].Value = "ภาพถ่ายงานซ่อม 4";
                workSheet.Cells[1, 32].Value = "ภาพถ่ายงานซ่อม 5";
                workSheet.Cells[1, 33].Value = "ภาพถ่ายงานซ่อม 6";
                workSheet.Cells[1, 34].Value = "บันทึกเสียงการโทร 1";
                workSheet.Cells[1, 35].Value = "บันทึกเสียงการโทร 2";
                workSheet.Cells[1, 36].Value = "เวลาล่าสุดที่เปิดดูหน้าเว็บ";
                workSheet.Cells[1, 37].Value = "IP ลูกค้า";
                workSheet.Cells[1, 38].Value = "เวลาที่ส่ง SMS";
                workSheet.Cells[1, 39].Value = "ประเภท";
                workSheet.Cells[1, 40].Value = "ข้ามการประเมิน";
                workSheet.Cells[1, 41].Value = "เหุตผล";
                workSheet.Cells[1, 42].Value = "ขอใช้สิทธิ์คูปอง";
                workSheet.Cells[1, 43].Value = "ยืนยันสิทธิ์ใช้คูปอง";
                workSheet.Cells[1, 44].Value = "คูปอง";
                int recordIndex = 2;
                var pic1 = ""; var pic2 = ""; var pic3 = ""; var pic4 = ""; var pic5 = ""; var pic6 = "";
                var voice1 = ""; var voice2 = "";

                foreach (var val in data)
                {
                    foreach (var pic in val.picture)
                    {
                        if (pic.path != null)
                        {
                            if (pic1 == "" && pic.sort == 1) pic1 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                            if (pic2 == "" && pic.sort == 2) pic2 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                            if (pic3 == "" && pic.sort == 3) pic3 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                            if (pic4 == "" && pic.sort == 4) pic4 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                            if (pic5 == "" && pic.sort == 5) pic5 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                            if (pic6 == "" && pic.sort == 6) pic6 = path_azure + pic.path.ToString() + pic.name.ToString() + key_azure;
                        }
                    }

                    foreach (var voice in val.voice)
                    {
                        if (voice.path != null)
                        {
                            if (voice1 == "") voice1 = path_azure + voice.path.ToString() + voice.name.ToString() + key_azure;
                            if (voice2 == "") voice2 = path_azure + voice.path.ToString() + voice.name.ToString() + key_azure;
                            voice2 = voice1 == voice2 ? "" : voice2;
                        }
                    }

                    //DateTime? sms_date = (DateTime?)null;
                    //if (val.sms_send.Count > 0)
                    //{
                    //    foreach (var item in val.sms_send)
                    //    {
                    //        if (sms_date == null)
                    //        {
                    //            sms_date = item.send_date;
                    //        }
                    //        else
                    //        {
                    //            if (sms_date < item.send_date)
                    //            {
                    //                sms_date = item.send_date;
                    //            }
                    //        }
                    //    }
                    //}

                    var date = val.duedate != null && val.to_duedate != null ? val.duedate.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "กรุณาตั้งเวลา";
                    var time = val.to_duedate != null ? " - " + val.to_duedate.Value.Hour + ":" + (val.to_duedate.Value.Minute < 10 ? "0" + val.to_duedate.Value.Minute.ToString() : val.to_duedate.Value.Minute.ToString()) : "";
                    var fulldate = time == "" ? date : date + time;
                    var url_pic = path_azure + (val.engineer_signature_path != null ? val.engineer_signature_path.ToString() : "") + (val.engineer_signature_name != null ? val.engineer_signature_name.ToString() : "") + key_azure;
                    var address = val.customer_street + val.customer_district + val.customer_province + val.customer_zipcode;
                    TimeSpan difference = val.job_repair != null && val.job_start != null ? (val.job_start.Value - val.job_repair.Value) : TimeSpan.Zero;
                    TimeSpan difference2 = val.job_end != null && val.job_repair != null ? (val.job_repair.Value - val.job_end.Value) : TimeSpan.Zero;

                    workSheet.Cells[recordIndex, 1].Value = val.job_no;
                    workSheet.Cells[recordIndex, 2].Value = val.status;
                    workSheet.Cells[recordIndex, 3].Value = val.site_code;
                    workSheet.Cells[recordIndex, 4].Value = val.site_name;
                    workSheet.Cells[recordIndex, 5].Value = val.engineer_code;
                    workSheet.Cells[recordIndex, 6].Value = val.engineer_name;
                    workSheet.Cells[recordIndex, 7].Value = val.customer_name;
                    workSheet.Cells[recordIndex, 8].Value = val.customer_mobile;
                    workSheet.Cells[recordIndex, 9].Value = val.customer_home;
                    workSheet.Cells[recordIndex, 10].Value = val.customer_office;
                    workSheet.Cells[recordIndex, 11].Value = address;
                    workSheet.Cells[recordIndex, 12].Value = val.customer_lat;
                    workSheet.Cells[recordIndex, 13].Value = val.customer_lng;

                    //new 2020-03-25
                    DateTime? new_prefer = val.customer_prefer_date != null ? new DateTime(val.customer_prefer_date.Value.Year, val.customer_prefer_date.Value.Month, val.customer_prefer_date.Value.Day, 0, 0, 0) : (DateTime?)null;

                    workSheet.Cells[recordIndex, 14].Value = new_prefer != null ? new_prefer.Value.ToString("dd/MMM/yyyy") + " เวลา " + val.customer_perfer_time : "ไม่ได้กรอกเวลา";

                    if (val.customer_perfer_time != null)
                    {
                        var new_prefer_time = val.customer_perfer_time.ToString();
                        var hours = Convert.ToInt32(new_prefer_time.Substring(0, new_prefer_time.IndexOf(":")));
                        hours = hours.ToString().Length == 0 ? 0 : hours;
                        var sub_colon = new_prefer_time.Substring((new_prefer_time.IndexOf(":") + 1));
                        var minutes = sub_colon.ToString().Length > 2 ? sub_colon.Substring(0, 2) : "0";
                        var double_min = sub_colon.ToString().Length > 2 ? Convert.ToInt32(minutes.Substring(0, 2)) : 0;
                        //new_prefer.Value.AddHours(hours);
                        //new_prefer.Value.AddMinutes(double_min);
                        if(new_prefer != null)
                        {
                            new_prefer = new DateTime(val.customer_prefer_date.Value.Year, val.customer_prefer_date.Value.Month, val.customer_prefer_date.Value.Day, hours, double_min, 0);
                        }
                    }
                    var prefer_result = (val.status == "Completed" && val.duedate != null) ?
                            new_prefer != null ? val.duedate <= new_prefer ? "true" : "false" 
                                : "false"
                        : ""; 
                    workSheet.Cells[recordIndex, 15].Value = val.customer_prefer_date != null ? prefer_result.ToString() : "";
                    //end-new

                    workSheet.Cells[recordIndex, 16].Value = val.asset;
                    workSheet.Cells[recordIndex, 17].Value = val.description_breakdown;
                    workSheet.Cells[recordIndex, 18].Value = val.description_parts;
                    workSheet.Cells[recordIndex, 19].Value = fulldate;
                    workSheet.Cells[recordIndex, 20].Value = val.job_start != null ? val.job_start.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    workSheet.Cells[recordIndex, 21].Value = val.job_repair != null ? difference.ToString(@"hh\:mm\:ss") + "นาที" : "";
                    workSheet.Cells[recordIndex, 22].Value = val.job_repair != null ? val.job_repair.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    workSheet.Cells[recordIndex, 23].Value = val.job_end != null ? val.job_end.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    workSheet.Cells[recordIndex, 24].Value = val.job_end != null ? difference2.ToString(@"hh\:mm\:ss") + "นาที" : "";
                    workSheet.Cells[recordIndex, 25].Value = val.reason_cancel;
                    workSheet.Cells[recordIndex, 26].Value = val.cancel_date != null ? val.cancel_date.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    if (val.engineer_signature_path != null)
                    {
                        workSheet.Cells[recordIndex, 27].Hyperlink = new Uri(url_pic, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 27].Value = "View";
                        workSheet.Cells[recordIndex, 27].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 27].Value = ""; }
                    if (pic1 != "")
                    {
                        workSheet.Cells[recordIndex, 28].Hyperlink = new Uri(pic1, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 28].Value = "View";
                        workSheet.Cells[recordIndex, 28].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 28].Value = ""; }
                    if (pic2 != "")
                    {
                        workSheet.Cells[recordIndex, 29].Hyperlink = new Uri(pic2, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 29].Value = "View";
                        workSheet.Cells[recordIndex, 29].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 29].Value = ""; }
                    if (pic3 != "")
                    {
                        workSheet.Cells[recordIndex, 30].Hyperlink = new Uri(pic3, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 30].Value = "View";
                        workSheet.Cells[recordIndex, 30].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 30].Value = ""; }
                    if (pic4 != "")
                    {
                        workSheet.Cells[recordIndex, 31].Hyperlink = new Uri(pic4, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 31].Value = "View";
                        workSheet.Cells[recordIndex, 31].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 31].Value = ""; }
                    if (pic5 != "")
                    {
                        workSheet.Cells[recordIndex, 32].Hyperlink = new Uri(pic5, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 32].Value = "View";
                        workSheet.Cells[recordIndex, 32].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 32].Value = ""; }
                    if (pic6 != "")
                    {
                        workSheet.Cells[recordIndex, 33].Hyperlink = new Uri(pic6, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 33].Value = "View";
                        workSheet.Cells[recordIndex, 33].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 33].Value = ""; }
                    if (voice1 != "")
                    {
                        workSheet.Cells[recordIndex, 34].Hyperlink = new Uri(voice1, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 34].Value = "View";
                        workSheet.Cells[recordIndex, 34].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 34].Value = ""; }
                    if (voice2 != "")
                    {
                        workSheet.Cells[recordIndex, 35].Hyperlink = new Uri(voice2, UriKind.Absolute);
                        workSheet.Cells[recordIndex, 35].Value = "View";
                        workSheet.Cells[recordIndex, 35].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    else { workSheet.Cells[recordIndex, 35].Value = ""; }

                    workSheet.Cells[recordIndex, 36].Value = val.customer_date;
                    workSheet.Cells[recordIndex, 37].Value = val.customer_ip;
                    workSheet.Cells[recordIndex, 38].Value = val.sms_send_new != null ? val.sms_send_new.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "" ;

                    workSheet.Cells[recordIndex, 39].Value = val.job_type;

                    workSheet.Cells[recordIndex, 40].Value = val.skip_status == 1 ? "True" : "";
                    workSheet.Cells[recordIndex, 41].Value = val.skip_note != null ? val.skip_note : "";
                    workSheet.Cells[recordIndex, 42].Value = val.req_coupon_date != null ? val.req_coupon_date.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    workSheet.Cells[recordIndex, 43].Value = val.used_coupon_date != null ? val.used_coupon_date.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                    workSheet.Cells[recordIndex, 44].Value = val.is_code_coupon != null ? val.is_code_coupon : "";
                    recordIndex++;
                }

                for(int i = 1; i <= 44; i++)
                {
                    workSheet.Column(i).AutoFit();
                }

                end_date = end_date.AddDays(-1).ToString("dd/MM/yyyy") == TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone).ToString("dd/MM/yyyy") ? end_date.AddDays(-1) : end_date;
                var name_file = "all-job " + start_date.ToString("dd/MM/yyyy") + " - " + end_date.ToString("dd/MM/yyyy") + ".xlsx";

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
            else if(page == "4")
            {
                var search = Request["search"];
                var site = int.Parse(Request["site"]);
                var status = int.Parse(Request["status"]);
                var category = int.Parse(Request["category"]);

                var s_start_date = Request["s_start_date"];
                var s_end_date = Request["s_end_date"];

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
                                   select new { id = j.Key }).ToList();


                    foreach (var add_data in data_id)
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
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site != 0 && category == 0 && status == 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site != 0 && category != 0 && status == 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site != 0 && category != 0 && status != 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.job_category_id == category && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site == 0 && category != 0 && status != 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site == 0 && category == 0 && status != 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site == 0 && category != 0 && status == 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == s.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.job_category_id == category && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.job_category_id == category && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }
                else if (site != 0 && category == 0 && status != 99)
                {
                    filter_data_site = (from s in db.tb_store where s.is_delete == 0 && s.id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 orderby s.site_name select s).ToList();
                    filter_data_job = (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j).ToList();
                    filter_data_engineer = (from en in db.tb_engineer where en.is_delete == 0 && en.site_id == site && (from j in db.tb_jobs where j.is_delete == 0 && j.engineer_id == en.id && j.status_job == status && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select en).ToList();
                    filter_data_logSMS = (from l in db.tb_log where l.job_id != null && (l.send_date >= start_date && l.send_date <= end_date) && (from j in db.tb_jobs where j.is_delete == 0 && j.store_id == site && j.status_job == status && j.id == l.job_id && j.appointment_datetime != null && j.appointment_to_datetime != null && (j.appointment_datetime >= start_date && j.appointment_datetime <= end_date) select j.id).Count() > 0 select l).ToList();
                }

                var data = new List<report>();
                if (User.IsInRole("admin"))
                {
                    data = search == "" ? (from s in filter_data_site
                                           orderby s.site_name
                                           select new report()
                                           {
                                               site_name = s.site_name,
                                               site_code = s.code_store,
                                               count_job = filter_data_job.Where(w => w.store_id == s.id).Count(),
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
                                                                    all_job = filter_data_job.Where(w => w.engineer_id == en.id).Count(),
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
                                           }).ToList()
                                      :
                                      (from s in filter_data_site
                                       where tem_id.Contains(s.id.ToString())
                                       orderby s.site_name
                                       select new report()
                                       {
                                           site_name = s.site_name,
                                           site_code = s.code_store,
                                           count_job = (from j in filter_data_job where j.store_id == s.id && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
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
                                                                all_job = filter_data_job.Where(w => w.engineer_id == en.id).Count(),
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
                                       }).ToList();

                }
                else
                {
                    data = search == "" ? (from s in filter_data_site
                                           where s.id == idStore
                                           orderby s.site_name
                                           select new report()
                                           {
                                               site_name = s.site_name,
                                               site_code = s.code_store,
                                               count_job = filter_data_job.Where(w => w.store_id == idStore).Count(),
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
                                                                    all_job = filter_data_job.Where(w => w.engineer_id == en.id).Count(),
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
                                           }).ToList()
                                     :
                                     (from s in filter_data_site
                                      where tem_id.Contains(idStore.ToString())
                                      orderby s.site_name
                                      select new report()
                                      {
                                          site_name = s.site_name,
                                          site_code = s.code_store,
                                          count_job = (from j in filter_data_job where j.store_id == s.id && (from e in filter_data_engineer where e.engineer_name.Contains(search.Trim()) && j.engineer_id == e.id select e.id).Count() > 0 select j.id).Count(),
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
                                                               all_job = filter_data_job.Where(w => w.engineer_id == en.id).Count(),
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
                                      }).ToList();
                }

                ExcelPackage excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "Code SVC";
                workSheet.Cells[1, 2].Value = "ชื่อศูนย์บริการ";
                workSheet.Cells[1, 3].Value = "ชื่อช่าง";
                workSheet.Cells[1, 4].Value = "รหัสช่าง";
                workSheet.Cells[1, 5].Value = "งานทั้งหมด";
                workSheet.Cells[1, 6].Value = "งานที่เสร็จสิ้น";
                workSheet.Cells[1, 7].Value = "งานยกเลิก";
                workSheet.Cells[1, 8].Value = "งานที่รอดำเนินการ";
                workSheet.Cells[1, 9].Value = "Prefer Date";
                workSheet.Cells[1, 10].Value = "งานล่าช้า";
                workSheet.Cells[1, 11].Value = "ส่ง sms";
                workSheet.Cells[1, 12].Value = "เปิดลิงค์";

                int recordIndex = 2;
                foreach (var val in data)
                {
                    foreach(var val2 in val.list_engineer)
                    {
                        workSheet.Cells[recordIndex, 1].Value = val.site_code;
                        workSheet.Cells[recordIndex, 2].Value = val.site_name;
                        workSheet.Cells[recordIndex, 3].Value = val2.engineer_name;
                        workSheet.Cells[recordIndex, 4].Value = val2.engineer_code;
                        workSheet.Cells[recordIndex, 5].Value = val2.all_job;
                        workSheet.Cells[recordIndex, 6].Value = val2.done_job;
                        workSheet.Cells[recordIndex, 7].Value = val2.cancel_job;
                        workSheet.Cells[recordIndex, 8].Value = val2.peding_job;
                        workSheet.Cells[recordIndex, 9].Value = val2.perfer_date;
                        workSheet.Cells[recordIndex, 10].Value = val2.delay_job;
                        workSheet.Cells[recordIndex, 11].Value = val2.sms_engineer;
                        workSheet.Cells[recordIndex, 12].Value = val2.sms_customer;
                        recordIndex++;
                    }
                }

                for (int i = 1; i <= 12; i++)
                {
                    workSheet.Column(i).AutoFit();
                }

                end_date = end_date.AddDays(-1).ToString("dd/MM/yyyy") == TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, zone).ToString("dd/MM/yyyy") ? end_date.AddDays(-1) : end_date;
                var name_file = "report " + start_date.ToString("dd/MM/yyyy") + " - " + end_date.ToString("dd/MM/yyyy") + ".xlsx";
                using (var memoryStream = new MemoryStream())
                {
                    excel.SaveAs(memoryStream);
                    byte[] bytesInStream = memoryStream.ToArray();
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.AddHeader("content-disposition", "attachment;    filename="+ name_file);
                    Response.BinaryWrite(bytesInStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return "true";
        }
    }
}