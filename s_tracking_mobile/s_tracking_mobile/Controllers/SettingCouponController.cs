using byi_common;
using CommonLib;
using Newtonsoft.Json;
using OfficeOpenXml;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize(Roles = "admin")]
    public class SettingCouponController : Controller
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        int show = 20;

        // GET: Coupon
        public ActionResult Index()
        {
            var low_date = now.Date.AddDays(-30);
            var high_date = now.Date.AddDays(30).AddDays(1);
            var q_coupon = db.tb_coupon.Where(w => w.coupon_start_date >= low_date && w.coupon_end_date <= high_date && w.is_delete == 0);

            var data = (from c in q_coupon
                        select new coupon_job
                        {
                            id = c.id,
                            type_job = c.type_job,
                            code_coupon = c.code_coupon,
                            is_delete = c.is_delete,
                            detail = c.detail,
                            type_coupon = c.type_coupon,
                            wty = c.wty,
                            user_update = c.user_update,
                            status_coupon = c.status_coupon,
                            coupon_start_date = c.coupon_start_date,
                            coupon_end_date = c.coupon_end_date,
                            is_used = c.is_used,
                            job_guid = db.tb_jobs.Where(j => j.id_coupon == c.id).Select(j => j.job_guid.ToString()).FirstOrDefault(),
                            service_order_no = db.tb_jobs.Where(s => s.id_coupon == c.id).Select(s => s.service_order_no).FirstOrDefault(),
                        }).OrderBy(s => s.id).Take(show).ToList();
            // .Select((r, i) => new { Row = r, Index = i+1 })
            //.Select((currRow, index) => new { drow = currRow, dindex = index + 1 })  8
            
            ViewBag.item_coupon = data;
            ViewData["count"] = q_coupon.Count();
            ViewData["low_date"] = low_date.ToString("dd/MM/yyyy");
            ViewData["high_date"] = high_date.ToString("dd/MM/yyyy");

            return View("index");
        }


        private IQueryable<tb_coupon> GetCouponFilter(string s_date, string e_date, int is_used = 99, int type = 0, string wty = "", string search = "", int typejob = 0)
        {

            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(s_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(e_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            var coupon = db.tb_coupon.Where(w => (w.coupon_start_date >= start_date && w.coupon_end_date <= end_date));

            if (!string.IsNullOrEmpty(search)) coupon = coupon.Where(w => w.code_coupon.Contains(search));
            if (!string.IsNullOrEmpty(wty)) coupon = coupon.Where(w => w.wty == wty);
            if (is_used != 99) coupon = coupon.Where(w => w.is_used == is_used);
            if (type != 0) coupon = coupon.Where(w => w.type_coupon == type);
            if (typejob != 0) coupon = coupon.Where(w => w.type_job == typejob);
            return coupon;
        }


        [HttpGet]
        public ActionResult page_skip_search(string s_date, string e_date, int is_used = 99, int type = 0, string wty = "", string search = "", int page = 1, int typejob = 0)
        {
            if (page < 1) { page = 1; }
            var skip_page = (page - 1) * show;
            IQueryable<tb_coupon> coupon = GetCouponFilter(s_date, e_date, is_used, type, wty, search, typejob);

            var res = (from c in coupon
                        select new coupon_job
                        {
                            id = c.id,
                            type_job = c.type_job,
                            code_coupon = c.code_coupon,
                            is_delete = c.is_delete,
                            detail = c.detail,
                            type_coupon = c.type_coupon,
                            wty = c.wty,
                            user_update = c.user_update,
                            status_coupon = c.status_coupon,
                            coupon_start_date = c.coupon_start_date,
                            coupon_end_date = c.coupon_end_date,
                            is_used = c.is_used,
                            job_guid = db.tb_jobs.Where(j => j.id_coupon == c.id).Select(j => j.job_guid.ToString()).FirstOrDefault(),
                            service_order_no = db.tb_jobs.Where(s => s.id_coupon == c.id).Select(s => s.service_order_no).FirstOrDefault()

                        }).OrderBy(s => s.id).Skip(skip_page).Take(show).ToList();

            return Json(new { data = res, count = coupon.Count() }, JsonRequestBehavior.AllowGet);
        }


        public object export_file(string s_date, string e_date, int is_used = 99, int type = 0, string wty = "", string search = "", int typejob = 0)
        {
            var data = GetCouponFilter(s_date, e_date, is_used, type, wty, search, typejob).ToList();

            ExcelPackage excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
            workSheet.Row(1).Style.Font.Bold = true;
            workSheet.Cells[1, 1].Value = "หมายเลขงาน";
            workSheet.Cells[1, 2].Value = "ประเภทงาน";
            workSheet.Cells[1, 3].Value = "คูปอง";
            workSheet.Cells[1, 4].Value = "รายละเอียด";
            workSheet.Cells[1, 5].Value = "ประเภท";
            workSheet.Cells[1, 6].Value = "WTY";
            workSheet.Cells[1, 7].Value = "วันที่เริ่มใช้";
            workSheet.Cells[1, 8].Value = "วันที่สิ้นสุด";
            workSheet.Cells[1, 9].Value = "สถานะ";


            int recordIndex = 2;
            foreach (var val in data)
            {

                workSheet.Cells[recordIndex, 1].Value = db.tb_jobs.Where(s => s.id_coupon == val.id).Select(s => s.service_order_no).FirstOrDefault();
                workSheet.Cells[recordIndex, 2].Value = val.type_job == 1 ? "Repair" : val.type_job == 2 ? "Cancel" : "";
                workSheet.Cells[recordIndex, 3].Value = val.code_coupon;
                workSheet.Cells[recordIndex, 4].Value = val.detail;
                workSheet.Cells[recordIndex, 5].Value = val.type_coupon == 1 ? coupon_type.mobile : val.type_coupon == 2 ? coupon_type.appliance : 0;
                workSheet.Cells[recordIndex, 6].Value = val.wty;
                workSheet.Cells[recordIndex, 7].Value = val.coupon_start_date != null ? val.coupon_start_date.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                workSheet.Cells[recordIndex, 8].Value = val.coupon_end_date != null ? val.coupon_end_date.Value.ToString("dd/MMM/yyy เวลา HH:mm") : "";
                workSheet.Cells[recordIndex, 9].Value = val.is_used == 1 ? "ใช้งานแล้ว" : "ยังไม่ใช้งาน";
                recordIndex++;

            }

            for (int i = 1; i <= 9; i++)
            {
                workSheet.Column(i).AutoFit();
            }


            var name_file = "Export Coupon" + now.ToString("dd/MM/yyyy") + ".xlsx";
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

            return "true";
        }


        public ActionResult Import()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            return View();
        }

        [HttpPost]
        public object import_file()
        {
            var excelfile = Request.Files[0];
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            var return_data = new List<error_import>();
            var return_error = new List<string>();
           

            var return_import = new List<object>();

            var error_serial = 0;
            //Step 1 เช็คไฟล์มีไหม
            if (excelfile == null || excelfile.ContentLength == 0)
            {
                ViewBag.Error = "Please select a excel file";
                return View();
            }
            else
            {
                //เช็คว่าเป็นไฟล์ excel
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
                        int indexrows = 1;
                        var toDay = now;
                        toDay = new DateTime(toDay.Year, toDay.Month, toDay.Day, 0, 0, 0);
                        var getName = User.Identity.Name;
                        var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                        Guid convertGuid = new Guid(CheckUser.ToString());

                        #region Set Column
                        var coupon_job = 0;
                        var coupon_code = 0; var coupon_detail = 0; var coupon_type = 0;
                        var coupon_wty = 0; var coupon_start = 0; var coupon_end = 0;
                        var coupon_delete = 0;

                        Debug.WriteLine(totalRows);

                        for (int i = 1; i <= totalColumn; i++)
                        {
                            var text = worksheet.Cells[1, i];

                            if (text.Value != null)
                            {
                                switch (text.Value.ToString().ToLower().Trim())
                                {
                                    case "coupon_job": coupon_job = i; break;
                                    case "coupon_code": coupon_code = i; break;
                                    case "coupon_detail": coupon_detail = i; break;
                                    case "coupon_type": coupon_type = i; break;
                                    case "coupon_wty": coupon_wty = i; break;
                                    case "coupon_start": coupon_start = i; break;
                                    case "coupon_end": coupon_end = i; break;
                                    case "coupon_delete": coupon_delete = i; break;
                                }
                            }

                        }
                        #endregion

                        #region Check Column               
                        if (coupon_job == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_job" });
                        if (coupon_code == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_code" });
                        if (coupon_detail == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_detail" });
                        if (coupon_type == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_type" });
                        if (coupon_wty == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_wty" });
                        if (coupon_start == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_start" });
                        if (coupon_end == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_end" });
                        if (coupon_delete == 0) return_data.Add(new error_import { text = "ไม่พบคอลัมน์ coupon_delete" });

                        if (return_data.Count > 0)
                        {
                            return Json(new { id = 1, msg = "Column faill", table = return_data }, JsonRequestBehavior.AllowGet);
                        }
                        #endregion

                        //เช็คว่าไฟล์ import มีข้อมูลครบ                    
                        #region import ไฟล์
                        for (int row = 2; row <= totalRows; row++)
                        {
                            string strcoupon_job = worksheet.Cells[row, coupon_job].Text;
                            string strcoupon_code = worksheet.Cells[row, coupon_code].Text;
                            string strcoupon_detail = worksheet.Cells[row, coupon_detail].Text;
                            string strcoupon_type = worksheet.Cells[row, coupon_type].Text;
                            string strcoupon_wty = worksheet.Cells[row, coupon_wty].Text.ToLower();
                            var strcoupon_start = Convert.ToDateTime(worksheet.Cells[row, coupon_start].Value);
                            var strcoupon_end = Convert.ToDateTime(worksheet.Cells[row, coupon_end].Value);
                            string strcoupon_delete = worksheet.Cells[row, coupon_delete].Text;

                            error_serial = row;
                            var new_coupon = new tb_coupon();

                            if (worksheet.Cells[row, coupon_code].Text != "")
                            {
                                //ดักข้อมูลใน excel
                                if (strcoupon_job == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Job" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_code == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Code" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_detail == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Detail" + " ของ คอลั่ม" + " " + strcoupon_code });
                                //strcoupon_detail = worksheet.Cells[row, coupon_detail].Text == "" ? null : worksheet.Cells[row, coupon_detail].Text;
                                //text_error += strcoupon_detail != null ? "" : text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Coupon Detail" : " , ไม่พบข้อมูล คอลัมน์ Coupon Detail";
                                if (strcoupon_type == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Type" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_wty == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon WTY" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_start == null) return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Start" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_end == null) return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon End" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (strcoupon_delete == "") return_data.Add(new error_import { text = "ไม่พบข้อมูล คอลัมน์ Coupon Delete" + " ของ คอลั่ม" + " " + strcoupon_code });
                                if (return_data.Count > 0)
                                {
                                    return Json(new { id = 2, msg = "Error ไฟล์ไม่ครบ", table = return_data }, JsonRequestBehavior.AllowGet);

                                }


                                try
                                {
                                    string couponcode = worksheet.Cells[row, coupon_code].Text;
                                    var objcoupon_code = db.tb_coupon.Where(w => w.code_coupon == couponcode).FirstOrDefault();

                                    if (objcoupon_code != null)
                                    {
                                        //อัพเดท
                                        if (objcoupon_code.is_delete == 0 || objcoupon_code.is_used == 0)
                                        {
                                            if (objcoupon_code.is_used == 1) //เคสนี้ใช้คูปองแล้ว
                                            {
                                                return_import.Add(new
                                                {
                                                    strcoupon_job = strcoupon_job,
                                                    strcoupon_code = strcoupon_code,
                                                    strcoupon_detail = strcoupon_detail,
                                                    strcoupon_type = strcoupon_type,
                                                    strcoupon_wty = strcoupon_wty,
                                                    strcoupon_start = strcoupon_start.ToShortDateString(),
                                                    strcoupon_end = strcoupon_end.ToShortDateString(),
                                                    import_status = "Error",
                                                    error_msg = "ไม่สามารถอัพเดท คอลัมน์ Coupon ของ" + " " + strcoupon_code + " " + "เนื่องจากคูปอง ถูกใช้ไปแล้ว "
                                                });
                                            }

                                            //อัพเดท
                                            // objcoupon_id.is_delete =0; ไม่อัพเดทฟิลนี้
                                            objcoupon_code.status_coupon = 0;
                                            objcoupon_code.type_job = EnumJob(strcoupon_job);
                                            objcoupon_code.code_coupon = strcoupon_code;
                                            objcoupon_code.detail = strcoupon_detail;
                                            objcoupon_code.type_coupon = EnumType(strcoupon_type);
                                            objcoupon_code.wty = EnumJob(strcoupon_job) == 1 ? strcoupon_wty : EnumJob(strcoupon_job) == 2 ? "out" : null; // if Cancel ปรับเป็น out เลย
                                            objcoupon_code.is_delete = EnumIsused(strcoupon_delete);
                                            objcoupon_code.create_date = now;
                                            objcoupon_code.update_date = now;
                                            objcoupon_code.user_update = User.Identity.Name;
                                            objcoupon_code.status_coupon = 1; //อัพเดพฟิลสเตตัส
                                                                              //เช็ค datetime
                                            objcoupon_code.coupon_start_date = strcoupon_start;
                                            objcoupon_code.coupon_end_date = strcoupon_end;
                                            db.SaveChanges();

                                            return_import.Add(new
                                            {
                                                strcoupon_job = strcoupon_job,
                                                strcoupon_code = strcoupon_code,
                                                strcoupon_detail = strcoupon_detail,
                                                strcoupon_type = strcoupon_type,
                                                strcoupon_wty = strcoupon_wty,
                                                strcoupon_start = strcoupon_start.ToShortDateString(),
                                                strcoupon_end = strcoupon_end.ToShortDateString(),
                                                import_status = "Update",
                                                error_msg = return_data
                                            });

                                        }
                                        else
                                        {

                                            return_import.Add(new
                                            {
                                                strcoupon_job = strcoupon_job,
                                                strcoupon_code = strcoupon_code,
                                                strcoupon_detail = strcoupon_detail,
                                                strcoupon_type = strcoupon_type,
                                                strcoupon_wty = strcoupon_wty,
                                                strcoupon_start = strcoupon_start.ToShortDateString(),
                                                strcoupon_end = strcoupon_end.ToShortDateString(),
                                                import_status = "Error",
                                                error_msg = "ไม่สามารถอัพเดท คอลัมน์ Coupon ของ" + " " + strcoupon_code + " " + "เนื่องจากคูปอง ถูกลบไปแล้ว "

                                            });
                                        }
                                    }
                                    else
                                    {
                                        //เพิ่มใหม่
                                        if (!string.IsNullOrEmpty(strcoupon_job)
                                            && !string.IsNullOrEmpty(strcoupon_code)
                                            && !string.IsNullOrEmpty(strcoupon_type)
                                            && !string.IsNullOrEmpty(strcoupon_wty)
                                            && !string.IsNullOrEmpty(strcoupon_delete)
                                            && (strcoupon_start != null)
                                            && (strcoupon_end != null)
                                            && !string.IsNullOrEmpty(strcoupon_detail))
                                        {
                                            // new_coupon.is_delete = 0;
                                            new_coupon.status_coupon = 0;
                                            new_coupon.type_job = EnumJob(strcoupon_job);
                                            new_coupon.code_coupon = strcoupon_code;
                                            new_coupon.detail = strcoupon_detail;
                                            new_coupon.type_coupon = EnumType(strcoupon_type);
                                            new_coupon.wty = EnumJob(strcoupon_job) == 1 ? strcoupon_wty : EnumJob(strcoupon_job) == 2 ? "out" : null; // if Cancel ปรับเป็น out เลย
                                            new_coupon.is_delete = EnumIsused(strcoupon_delete);
                                            new_coupon.create_date = now;
                                            new_coupon.update_date = now;
                                            new_coupon.user_update = User.Identity.Name;
                                            new_coupon.coupon_start_date = strcoupon_start;
                                            new_coupon.coupon_end_date = strcoupon_end;

                                            db.tb_coupon.Add(new_coupon);
                                            db.SaveChanges();


                                            return_import.Add(new
                                            {
                                                strcoupon_job = strcoupon_job,
                                                strcoupon_code = strcoupon_code,
                                                strcoupon_detail = strcoupon_detail,
                                                strcoupon_type = strcoupon_type,
                                                strcoupon_wty = strcoupon_wty,
                                                strcoupon_start = strcoupon_start.ToShortDateString(),  //ตัดให้เหลือแต่วันที่
                                                strcoupon_end = strcoupon_end.ToShortDateString(),
                                                import_status = "New",
                                                error_msg = return_data
                                            });
                                        }
                                    }


                                }
                                catch (Exception e)
                                {
                                    // error ระหว่างเพิ่มข้อมูล
                                    return_data.Add(new error_import { text = "ไม่สามารถ Import ข้อมูลหมายเลขคูปอง " + new_coupon.code_coupon + " เนื่องจาก Coupon Code ซ้ำ " });
                                    return Json(new { id = 2, msg = "OK", table = return_data }, JsonRequestBehavior.AllowGet); //ระบุ id แต่ละอันว่าจะ return อะไร
                                }
                                #endregion

                                indexrows++;

                                // ถ้าจะเพิ่ม return error แบบครั้งเดียวสามารถเพิ่มตรงนี้

                            }
                            System.IO.File.Delete(path);
                        }
                    }
                }
                else
                {
                    // error นามสกุลไม่ถูก
                    return Json(new { id = 5, msg = "File incorrect", table = return_data }, JsonRequestBehavior.AllowGet);

                }
            }

            return Json(new { id = 3, msg = "OK", table = return_import }, JsonRequestBehavior.AllowGet); //ระบุ id แต่ละอันว่าจะ return อะไร

        }


        private int EnumIsused(string str_used)
        {
            int res = 0;
            if (str_used.ToLower().Trim() == "yes")
            {
                res = 1;
            }
            return res;
        }

        private int EnumType(string str_type)
        {
            int res = 0;

            switch (str_type.ToLower().Trim())
            {
                case "mobile": res = 1; break;
                case "appliance": res = 2; break;

            }
            return res;
        }

        private int EnumJob(string str_type)
        {
            int res = 0;
            switch (str_type.ToLower().Trim())
            {
                case "repair": res = 1; break;
                case "cancel": res = 2; break;
            }
            return res;
        }


        public ActionResult Setting()
        {
            var setting_cou = db.tb_setting_coupon.FirstOrDefault();
            if (setting_cou != null)
            {
                var objsetting_coupon = db.tb_setting_coupon.Select(p => new
                {
                    url_mobile = p.url_mobile,
                    url_electronic = p.url_electronic,
                    is_show_mobile = p.is_show_mobile,
                    is_show_electronic = p.is_show_electronic,
                    detail_mobile = p.detail_mobile,
                    detail_electronic = p.detail_electronic

                }).SingleOrDefault();

                if (objsetting_coupon != null)
                {
                    ViewBag.is_show_mobile = objsetting_coupon.is_show_mobile;
                    ViewBag.is_show_electronic = objsetting_coupon.is_show_electronic;
                }
                ViewData["data"] = setting_cou;
            }

            return View();
        }

        private int EnumStatus(string str_type)
        {
            int dev = 0;
            switch(str_type.ToLower().Trim())
            {
                case "new": dev = 0; break;
                case "error": dev = 1; break;
            }
            return dev;
        }

        [HttpPost, ValidateInput(false)]
        public object btn_save_setting(coupon_setting csetting)
        {
            List<validate_all> validates = new List<validate_all>();
            if (csetting != null)
            {
                //find old setting or create new
                var sett = db.tb_setting_coupon.FirstOrDefault();
                if (sett != null)
                {
                    sett.message = csetting.message;
                    sett.detail_mobile = csetting.message_moblie;
                    sett.detail_electronic = csetting.message_electronic;
                    sett.url_mobile = csetting.url_mobile;
                    sett.url_electronic = csetting.url_elec;
                    sett.is_show_mobile = csetting.is_show_mobile ? 1 : 0;
                    sett.is_show_electronic = csetting.is_show_elec ? 1 : 0;
                    sett.update_date = now;
                    sett.user_update = User.Identity.Name;
                    db.tb_setting_coupon.AddOrUpdate(sett);
                    db.SaveChanges();

                }
                else
                {
                    tb_setting_coupon tb = new tb_setting_coupon();
                    tb.message = csetting.message;
                    tb.detail_mobile = csetting.message_moblie;
                    tb.detail_electronic = csetting.message_electronic;
                    tb.url_mobile = csetting.url_mobile;
                    tb.url_electronic = csetting.url_elec;
                    sett.is_show_mobile = csetting.is_show_mobile ? 1 : 0;
                    sett.is_show_electronic = csetting.is_show_elec ? 1 : 0;
                    tb.create_date = now;
                    tb.update_date = now;
                    tb.user_update = User.Identity.Name;
                    db.tb_setting_coupon.Add(tb);
                    db.SaveChanges();
                }

                return true;
            }
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validates);
            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [HttpPost]
        public void btn_delete(int id)
        {
            var data = db.tb_coupon.Where(w => w.id == id).FirstOrDefault();
            if (data != null)
            {
                data.is_delete = 1;
                data.user_update = User.Identity.Name;
                data.update_date = now;
                db.tb_coupon.AddOrUpdate(data);
                db.SaveChanges();
            }

        }
    

    }
}
