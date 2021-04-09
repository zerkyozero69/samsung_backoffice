using byi_common;
using CommonLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{// 
    public class ViewCouponController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        // GET: OfferCoupon
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public ActionResult Index(string couponjob)
        {
            //เช็คมี id หรือเปล่า
            if (!string.IsNullOrEmpty(couponjob))
            {
                //linq แบบซ้อน ตอนเรียกใช้ เรียกแบบ objjobs. ddd ได้เลย
                var objjobs = (from p in db.tb_jobs
                               where p.req_couponnumble == couponjob
                               select new
                               {
                                   req_coupon_date = p.req_coupon_date,
                                   used_coupon_date = p.used_coupon_date,
                                   coupon = (from c in db.tb_coupon where c.id == p.id_coupon select new {
                                       code_coupon = c.code_coupon,
                                       type_coupon = c.type_coupon,
                                       coupon_start_date = c.coupon_start_date,
                                       coupon_end_date = c.coupon_end_date,
                                       used_coupon_date = p.used_coupon_date
                                   }).FirstOrDefault()
                               }

                          ).FirstOrDefault();
                 
                if (objjobs != null)
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

                    DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc((DateTime)objjobs.req_coupon_date, zone);
                    int day = (int)(now - start_date).TotalDays;

                    if (objsetting_coupon != null && day <= 7)
                    {
                        ViewBag.couponjob = couponjob;
                        ViewBag.url_mobile = objsetting_coupon.url_mobile;
                        ViewBag.url_electronic = objsetting_coupon.url_electronic;
                        ViewBag.is_show_mobile = objsetting_coupon.is_show_mobile;
                        ViewBag.is_show_electronic = objsetting_coupon.is_show_electronic;
                        ViewBag.detail_mobile = objsetting_coupon.detail_mobile;
                        ViewBag.detail_electronic = objsetting_coupon.detail_electronic;

                        if (objsetting_coupon.is_show_mobile == 0 || objsetting_coupon.is_show_electronic == 0)
                        {
                            int type_coupon = objsetting_coupon.is_show_mobile == 0 ? 2 : objsetting_coupon.is_show_electronic == 0 ? 1 : 0;
                            var objData = this.LoadCoupon(couponjob, type_coupon);
                            if (objData != null) // Load auto 1 tab
                            {
                                if ((int)objData[0] == 1)
                                {
                                    ViewBag.req_type = objData[1];
                                    ViewBag.req_coupon = objData[2];
                                    ViewBag.coupon_end_date = "Expirational Date : " + objData[3];
                                }
                            }
                            else // Back view coupon
                            {
                                if (objjobs.coupon != null)
                                {
                                    ViewBag.req_type = objjobs.coupon.type_coupon;
                                    ViewBag.req_coupon = objjobs.coupon.code_coupon;
                                    ViewBag.coupon_end_date = "Expirational Date : " + objjobs.coupon.coupon_end_date.Value.ToString("dd/MMM/yyyy");
                                }
                            }
                        }
                        else
                        {
                            // Back view coupon open 2 tab
                            if (objjobs.coupon != null)
                            {
                                ViewBag.req_type = objjobs.coupon.type_coupon;
                                ViewBag.req_coupon = objjobs.coupon.code_coupon;
                                ViewBag.coupon_end_date = "Expirational Date : " + objjobs.coupon.coupon_end_date.Value.ToString("dd/MMM/yyyy");
                            }
                        }

                        return View();
                    }
                }
            }

            return Redirect("/customerror.html");
        }


        [HttpPost]
        public object GetCoupon(coupon _coupon)
        {
            //รับเลข req_couponnumble ที่ส่งมาจาก API
            var objData = this.LoadCoupon(_coupon.couponjob, _coupon.type);
            if (objData != null)
            {
                if((int)objData[0] == 1)
                {
                    return Json(new { id = objData[0], type = objData[1], code_coupon = objData[2], coupon_end_date = objData[3] }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { id = objData[0], type = objData[1], msg = objData[2] }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { id = 3 }, JsonRequestBehavior.AllowGet);
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


        private object[] LoadCoupon(string couponjob, int type)
        {
            if (!string.IsNullOrEmpty(couponjob))
            {
                var data = db.tb_jobs.Where(w => w.is_delete == 0 && w.req_couponnumble == couponjob).FirstOrDefault();
                if (data != null)
                {
                    if (data.req_couponnumble == couponjob && data.id_coupon == null)
                    {
                        var iswty = data.warranty_flag == "OW" && !string.IsNullOrEmpty(data.warranty_flag) ? "out" : 
                                    data.warranty_flag == "LP" && !string.IsNullOrEmpty(data.warranty_flag) ? "in" : "";
                        var checkcoupon = db.tb_coupon.Where(w => w.is_delete == 0 && w.is_used == 0 && w.type_coupon == type 
                                          && w.wty == iswty && w.type_job == data.req_typejob).FirstOrDefault();

                        if (checkcoupon != null)
                        {
                            checkcoupon.is_used = 1;
                            checkcoupon.used_coupon_date = now;
                            data.id_coupon = checkcoupon.id;
                            data.used_coupon_date = now;
                            data.is_used_coupon = 1;
                            db.SaveChanges();

                            return new object[4] { 1, type, checkcoupon.code_coupon, checkcoupon.coupon_end_date.Value.ToString("dd/MMM/yyyy")};
                        }
                        else
                        {
                            return new object[3] { 2, type, "Coupon sold out!" };
                        }
                    }
                }
            }

            return null;
        }
    }
}