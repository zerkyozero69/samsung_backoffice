using byi_common;
using CommonLib;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        // GET: Category
        public ActionResult all()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            var getName = User.Identity.Name;
            if (getName != "" || User.IsInRole("admin"))
            {
                var data = db.tb_jobsl_category.Where(w => w.is_delete == 0).ToList();
                var sub_Data = (from c in data
                                where c.parent_id == 0 && c.is_delete == 0
                                select new category()
                                {
                                    category_name = c.name,
                                    data = (from new_c in data
                                            where c.is_delete == 0 && new_c.parent_id == c.id
                                            select new sub_data()
                                            {
                                                name = new_c.name,
                                                code = new_c.code,
                                            }).ToList()
                                }).ToList();

                ViewData["Data-Category"] = data;
                ViewData["Sub-data"] = sub_Data;
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }

            return View();
        }

        public ActionResult editSub(string id = "")
        {
            Guid check_Guid;
            bool isValid = Guid.TryParse(id, out check_Guid);

            if (User.IsInRole("admin") && isValid)
            {
                Guid newGuid = Guid.Parse(id);
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                var data = db.tb_jobsl_category.Where(w => w.guid_category == newGuid && w.is_delete == 0 && w.parent_id == 0).FirstOrDefault();
                var data_catagory = db.tb_jobsl_category.Where(w => w.is_delete == 0).ToList();
                ViewData["Data-Category"] = data_catagory;
                ViewData["Data-sub"] = data;
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        public ActionResult addSub(int id=0)
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                var data = db.tb_jobsl_category.Where(w => w.is_delete == 0 && w.parent_id == 0).ToList();
                ViewData["Data-Category"] = data;
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        public ActionResult addCategory()
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                var data = (from c in db.tb_jobsl_category
                            where c.is_delete == 0 && c.parent_id == 0
                            select new cat_count()
                            {
                                id = c.id,
                                guid_category = c.guid_category,
                                count = (from nc in db.tb_jobsl_category where nc.is_delete == 0 && nc.parent_id == c.id select nc).Count(),
                                name = c.name,
                                code = c.code
                            }).ToList();

                ViewData["Data-Category"] = data;
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        public ActionResult editcategory()
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        [HttpPost]
        public object btn_save_Category(string name , string code)
        {
            List<validate_all> validates = new List<validate_all>();
            if (name != "" && name != null) //name
            {
                var checkName = common.xss_input_string(name, name.Length);
                if (checkName == false) { validates.Add(new validate_all { name_div = "#txtName", text = "รูปแบบชื่อประเภทไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtName", text = "กรุณาระบุชื่อประเภท" }); }

            if (code != "" && code != null) //code
            {
                var checkCode = common.xss_input_string(code, code.Length);
                if (checkCode == false) { validates.Add(new validate_all { name_div = "#txtCode", text = "รูปแบบโค้ดประเภทไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtCode", text = "กรุณาระบุโค้ดประเภท" }); }

            if (validates.Count() == 0 && User.IsInRole("admin"))
            {
                Guid new_guid = Guid.NewGuid();
                tb_jobsl_category obj_new = new tb_jobsl_category();
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.is_delete = 0;
                obj_new.user_update = "admin";
                obj_new.name = name;
                obj_new.code = code;
                obj_new.guid_category = new_guid;
                obj_new.description = "";
                obj_new.parent_id = 0;

                db.tb_jobsl_category.Add(obj_new);
                db.SaveChanges();

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
        public void btn_delete_Category(int id)
        {
            if(User.IsInRole("admin"))
            {
                var obj_new = db.tb_jobsl_category.Where(w => w.id == id && w.is_delete == 0).FirstOrDefault();
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.is_delete = 1;
                db.tb_jobsl_category.AddOrUpdate(obj_new);
                db.SaveChanges();
            }
        }

        [HttpPost]
        public object btn_save_sub(tb_jobsl_category sub_job)
        {
            List<validate_all> validates = new List<validate_all>();

            if (sub_job.name != "" && sub_job.name != null) //name
            {
                var checkNameCategory = common.xss_input_string(sub_job.name, sub_job.name.Length);
                if (checkNameCategory == false) { validates.Add(new validate_all { name_div = "#txtProductName", text = "รูปแบบชื่อผลิตภัณฑ์ไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtProductName", text = "กรุณาระบุชื่อผลิตภัณฑ์" }); }

            if (sub_job.code != "" && sub_job.code != null) //code
            {
                var checkCodeCategory = common.xss_input_string(sub_job.code, sub_job.code.Length);
                if (checkCodeCategory == false) { validates.Add(new validate_all { name_div = "#txtCodeName", text = "รูปแบบโค้ดผลิตภัณฑ์ไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtCodeName", text = "กรุณาระบุโค้ดผลิตภัณฑ์" }); }


            if (validates.Count() == 0 && User.IsInRole("admin"))
            {
                Guid new_guid = Guid.NewGuid();
                tb_jobsl_category obj_new = new tb_jobsl_category();
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.is_delete = 0;
                obj_new.user_update = "admin";
                obj_new.name = sub_job.name;
                obj_new.code = sub_job.code;
                obj_new.parent_id = sub_job.parent_id;
                obj_new.guid_category = new_guid;

                db.tb_jobsl_category.Add(obj_new);
                db.SaveChanges();
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

        [HttpGet]
        public object btn_select_category(int id)
        {
            string jsonString = "";
            if (User.IsInRole("admin"))
            {
                if (id == 0)
                {
                    var sub_Data = (from c_all in db.tb_jobsl_category
                                    where c_all.parent_id == 0 && c_all.is_delete == 0
                                    select new category()
                                    {
                                        category_name = c_all.name,
                                        data = (from new_c in db.tb_jobsl_category
                                                where c_all.is_delete == 0 && new_c.parent_id == c_all.id
                                                select new sub_data()
                                                {
                                                    name = new_c.name,
                                                    code = new_c.code
                                                }).ToList()
                                    }).ToList();
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(sub_Data);

                    return new ContentResult()
                    {
                        Content = jsonString,
                        ContentType = "application/json"
                    };
                }
                else
                {
                    var sub_Data = (from c in db.tb_jobsl_category
                                    where c.id == id && c.is_delete == 0
                                    select new category()
                                    {
                                        category_name = c.name,
                                        data = (from new_c in db.tb_jobsl_category
                                                where c.is_delete == 0 && new_c.parent_id == c.id
                                                select new sub_data()
                                                {
                                                    name = new_c.name,
                                                    code = new_c.code
                                                }).ToList()
                                    }).ToList();
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(sub_Data);

                   
                }
            }

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }
    }
}