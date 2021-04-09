using CommonLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Migrations;
using byi_common;
using s_tracking_mobile.Models;
using System.Web.Security;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Configuration;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class EngineerController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public void GetDataFromDb()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["Provinces"] = db.tb_provinces.ToList();
            ViewData["Districtes"] = db.tb_districts.ToList();
            ViewData["Sub_Districtes"] = db.tb_amphures.ToList();
            ViewData["SiteName"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
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
        // GET: Engineer
        public ActionResult all()
        {
            GetDataFromDb();

            if (User.IsInRole("admin"))
            {
                var obj_list_en = (from e_admin in db.tb_engineer
                                   where e_admin.is_delete == 0
                                   orderby e_admin.id descending
                                   select new engineer()
                                   {
                                       id = e_admin.id,
                                       en_guid = e_admin.en_guid,
                                       en_name = e_admin.engineer_name,
                                       code_engineer = e_admin.code_engineer,
                                       shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e_admin.site_id select s.site_name).FirstOrDefault(),
                                       en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e_admin.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                       en_tel1 = e_admin.tel1
                                   });

                ViewData["All-Engineer"] = obj_list_en.Skip(0).Take(20).ToList();
                ViewData["Count-Data"] = obj_list_en.Count();
            }
            else //get site id
            {
                var getName = User.Identity.Name;

                if (getName != "" && getName != null)
                {
                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var obj_list_en = (from e in db.tb_engineer
                                       where e.is_delete == 0 && e.site_id == idStore
                                       orderby e.id descending
                                       select new engineer()
                                       {
                                           id = e.id,
                                           en_guid = e.en_guid,
                                           en_name = e.engineer_name,
                                           code_engineer = e.code_engineer,
                                           shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e.site_id select s.site_name).FirstOrDefault(),
                                           en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                           en_tel1 = e.tel1
                                       });

                    ViewData["All-Engineer"] = obj_list_en.Skip(0).Take(20).ToList();
                    ViewData["Count-Data"] = obj_list_en.Count();
                }
                else
                {
                    //wait redirect
                    var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                    Response.Redirect(url);
                }
            }

            ViewData["All-Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();
            return View();
        }

        public ActionResult add()
        {
            GetDataFromDb();

            return View();
        }

        public ActionResult edit(string id)
        {
            GetDataFromDb();

            Guid check_Guid;
            bool isValid = Guid.TryParse(id, out check_Guid);

            if (isValid)
            {
                Guid newGuid = Guid.Parse(id);
                if (User.IsInRole("admin"))
                {
                    var obj_list_en = (from e in db.tb_engineer
                                       where e.en_guid == newGuid && e.is_delete == 0
                                       select e).FirstOrDefault();

                    if(obj_list_en != null)
                    {
                        ViewData["Data"] = obj_list_en;
                    }
                    else
                    {
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                        Response.Redirect(url);
                    }
                    
                }
                else
                {
                    var getName = User.Identity.Name;

                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var obj_list_en = (from e in db.tb_engineer
                                       where e.site_id == idStore && e.en_guid == newGuid && e.is_delete == 0
                                       select e).FirstOrDefault();

                    if (obj_list_en != null)
                    {
                        ViewData["Data"] = obj_list_en;
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                        Response.Redirect(url);
                    }
                }
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                Response.Redirect(url);
            }

            ViewData["Azure_Path"] = ConfigurationManager.AppSettings["Azure_url"];
            StandardController service_con = new StandardController();
            string key_azure = service_con.GetKeyAzure();
            ViewData["Key_Azure"] = key_azure;

            return View();
        }

        public ActionResult ps(string id)
        {
            GetDataFromDb();

            Guid check_Guid;
            bool isValid = Guid.TryParse(id, out check_Guid);

            if (isValid)
            {
                Guid newGuid = Guid.Parse(id);
                if (User.IsInRole("admin"))
                {
                    var obj_list_en = (from e in db.tb_engineer
                                       where e.en_guid == newGuid && e.is_delete == 0
                                       select e).FirstOrDefault();

                    if (obj_list_en != null)
                    {
                        ViewData["Data"] = obj_list_en;
                    }
                    else
                    {
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                        Response.Redirect(url);
                    }

                }
                else
                {
                    var getName = User.Identity.Name;

                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var obj_list_en = (from e in db.tb_engineer
                                       where e.site_id == idStore && e.en_guid == newGuid && e.is_delete == 0
                                       select e).FirstOrDefault();

                    if (obj_list_en != null)
                    {
                        ViewData["Data"] = obj_list_en;
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                        Response.Redirect(url);
                    }
                }
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "engineer/all";
                Response.Redirect(url);
            }

            //ViewData["Azure_Path"] = ConfigurationManager.AppSettings["Azure_url"];
            //StandardController service_con = new StandardController();
            //string key_azure = service_con.GetKeyAzure();
            //ViewData["Key_Azure"] = key_azure;

            return View();
        }

        [HttpPost]
        public object btn_save_engineer()
        {
            List<validate_all> validates = new List<validate_all>();
            var name = User.Identity.Name;
            var code_engineer = Request.Form["code_engineer"];
            var engineer_name = Request.Form["engineer_name"];
            var tel1 = Request.Form["tel1"];
            var tel2 = Request.Form["tel2"];
            var tel3 = Request.Form["tel3"];
            var email1 = Request.Form["email1"];
            var repair_type_info1 = Request.Form["repair_type_info1"];
            var repair_type_info2 = Request.Form["repair_type_info2"];
            var repair_type_info3 = Request.Form["repair_type_info3"];
            var site_id = Request.Form["site_id"];

            #region Check information
            //Check
            if (code_engineer != "" && code_engineer != null) //engineer_name
            {
                var checkCode_engineer = common.xss_input_string(code_engineer, code_engineer.Length);
                if (checkCode_engineer == false) {
                    validates.Add(new validate_all { name_div = "#txtEngineerCode", text = "รูปแบบโค้ดช่างไม่ถูกต้อง" });
                }
                else
                {
                    var checkdata_db = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer == code_engineer).FirstOrDefault();
                    if (checkdata_db != null) { validates.Add(new validate_all { name_div = "#txtEngineerCode", text = "โค้ดซ้ำกับในระบบ" }); }
                }
            }
            else { validates.Add(new validate_all { name_div = "#txtEngineerCode", text = "กรุณาระบุโค้ดช่าง" }); }

            if (engineer_name != "" && engineer_name != null) //engineer_name
            {
                var checkEngineer_name = common.xss_input_string(engineer_name, engineer_name.Length);
                if (checkEngineer_name == false) { validates.Add(new validate_all { name_div = "#txtEngineerName", text = "รูปแบบชื่อช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEngineerName", text = "กรุณาระบุชื่อช่าง" }); }

            if (repair_type_info1 != "" && repair_type_info1 != null) //repair_type_info1
            {
                var checkRepair_type_info1 = common.xss_input_string(repair_type_info1, repair_type_info1.Length);
                if (checkRepair_type_info1 == false) { validates.Add(new validate_all { name_div = "#txtinfo1", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (repair_type_info2 != "" && repair_type_info2 != null) //repair_type_info2
            {
                var checkRepair_type_info2 = common.xss_input_string(repair_type_info2, repair_type_info2.Length);
                if (checkRepair_type_info2 == false) { validates.Add(new validate_all { name_div = "#txtinfo2", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (repair_type_info3 != "" && repair_type_info3 != null) //repair_type_info3
            {
                var checkRepair_type_info3 = common.xss_input_string(repair_type_info3, repair_type_info3.Length);
                if (checkRepair_type_info3 == false) { validates.Add(new validate_all { name_div = "#txtinfo3", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (tel1 != "" && tel1 != null) //tel1
            {
                var checkTel1 = common.IsNumeric(tel1);
                if (checkTel1 == false) { validates.Add(new validate_all { name_div = "#txtTel1", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtTel1", text = "กรุณาระบุหมายเลขโทรศัพท์ช่าง" }); }

            if (tel2 != "" && tel2 != null) //tel2
            {
                var checkTel2 = common.IsNumeric(tel2);
                if (checkTel2 == false) { validates.Add(new validate_all { name_div = "#txtTel2", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }

            if (tel3 != "" && tel3 != null) //tel3
            {
                var checkTel3 = common.IsNumeric(tel3);
                if (checkTel3 == false) { validates.Add(new validate_all { name_div = "#txtTel3", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }

            if (email1 != "" && email1 != null) //email1
            {
                var checkEmail1 = common.isEmailFormat(email1);
                if (checkEmail1 == false) { validates.Add(new validate_all { name_div = "#txtEmail1", text = "รูปแบบอีเมล์ช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEmail1", text = "กรุณาระบุอีเมล์ช่าง" }); }

            if (Int32.Parse(site_id) == 0 || Int32.Parse(site_id) != 0) //site_id
            {
                var checkSite_id = common.IsNumeric(Int32.Parse(site_id));
                if (checkSite_id == false) { validates.Add(new validate_all { name_div = "#alert_select_site", text = "กรุณาเลือกศูนย์บริการ" }); }
            }
            #endregion
            
            #region Check File Image
            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                string filename = files[0].FileName;
            }
            #endregion

            //check site
            var getName = User.Identity.Name;
            var idStore = 0;
            if (getName != "")
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
            }


            if (validates.Count() == 0 && (User.IsInRole("admin") || idStore != 0))
            {
                #region Check User
                var CheckUser = Membership.GetUser(code_engineer);
                if (CheckUser == null)
                {
                    MembershipUser newUser = Membership.CreateUser(code_engineer, "1111111111", email1);
                    Membership.UpdateUser(newUser);
                    Roles.AddUserToRole(code_engineer, "engineer");
                }

                var getGuid = Membership.GetUser(code_engineer).ProviderUserKey;
                Guid convertGuid = new Guid(getGuid.ToString());
                #endregion

                #region Save Data
                tb_engineer obj_new = new tb_engineer();
                Guid guid_id = Guid.NewGuid();
                obj_new.en_guid = guid_id;
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.user_update = name;
                obj_new.is_delete = 0;
                obj_new.engineer_name = engineer_name;
                obj_new.tel1 = tel1;
                obj_new.tel2 = tel2;
                obj_new.tel3 = tel3;
                obj_new.email1 = email1;
                obj_new.email2 = null;
                obj_new.email3 = null;
                obj_new.ability_asset = null;
                obj_new.province = 0;
                obj_new.site_id = Int32.Parse(site_id);
                obj_new.code_engineer = code_engineer;
                obj_new.account_guid = convertGuid;

                db.tb_engineer.Add(obj_new);
                db.SaveChanges();
                #endregion

                #region Save File
                if (Request.Files.Count > 0)
                {
                    string url = ConfigurationManager.AppSettings["Api_url"].ToString();
                    HttpFileCollectionBase files = Request.Files;
                    string filename = files[0].FileName;

                    var stream = files[0].InputStream;

                    var uri2 = url + "engineer/uploadavatar";
                    var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new StreamContent(stream), "\"fileupload_1\"", filename);


                    // EventModel other fields
                    multipartContent.Add(new StringContent(obj_new.id.ToString()), "id_engi");
                    var httpClient = new HttpClient();
                    var httpResponseMessage = httpClient.PostAsync(uri2, multipartContent).Result;
                }
                #endregion

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
        public void btn_delete_engineer(int id)
        {
            var data = db.tb_engineer.SingleOrDefault(w => w.id == id);

            //check site
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.site_id == idStore ? true : false;
            }

            if ((User.IsInRole("admin") || isSite))
            {
                data.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data.is_delete = 1;
                db.tb_engineer.AddOrUpdate(data);
                db.SaveChanges();
            }
        }

        [HttpPost]
        public object btn_edit_engineer()
        {
            List<validate_all> validates = new List<validate_all>();
            var name = User.Identity.Name;
            var id = Request.Form["id"];
            var engineer_name = Request.Form["engineer_name"];
            var tel1 = Request.Form["tel1"];
            var tel2 = Request.Form["tel2"];
            var tel3 = Request.Form["tel3"];
            var email1 = Request.Form["email1"];
            var repair_type_info1 = Request.Form["repair_type_info1"];
            var repair_type_info2 = Request.Form["repair_type_info2"];
            var repair_type_info3 = Request.Form["repair_type_info3"];
            var site_id = Request.Form["site_id"];
            var current_img = Request.Form["current_img"];

            #region Check Information
            if (engineer_name != "" && engineer_name != null) //engineer_name
            {
                var checkEngineer_name = common.xss_input_string(engineer_name, engineer_name.Length);
                if (checkEngineer_name == false) { validates.Add(new validate_all { name_div = "#txtEngineerName", text = "รูปแบบชื่อช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEngineerName", text = "กรุณาระบุชื่อช่าง" }); }

            if (repair_type_info1 != "" && repair_type_info1 != null) //repair_type_info1
            {
                var checkRepair_type_info1 = common.xss_input_string(repair_type_info1, repair_type_info1.Length);
                if (checkRepair_type_info1 == false) { validates.Add(new validate_all { name_div = "#txtinfo1", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (repair_type_info2 != "" && repair_type_info2 != null) //repair_type_info2
            {
                var checkRepair_type_info2 = common.xss_input_string(repair_type_info2, repair_type_info2.Length);
                if (checkRepair_type_info2 == false) { validates.Add(new validate_all { name_div = "#txtinfo2", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (repair_type_info3 != "" && repair_type_info3 != null) //repair_type_info3
            {
                var checkRepair_type_info3 = common.xss_input_string(repair_type_info3, repair_type_info3.Length);
                if (checkRepair_type_info3 == false) { validates.Add(new validate_all { name_div = "#txtinfo3", text = "รูปแบบชื่อสินค้าไม่ถูกต้อง" }); }
            }

            if (tel1 != "" && tel1 != null) //tel1
            {
                var checkTel1 = common.IsNumeric(tel1);
                if (checkTel1 == false) { validates.Add(new validate_all { name_div = "#txtTel1", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtTel1", text = "กรุณาระบุหมายเลขโทรศัพท์ช่าง" }); }

            if (tel2 != "" && tel2 != null) //tel2
            {
                var checkTel2 = common.IsNumeric(tel2);
                if (checkTel2 == false) { validates.Add(new validate_all { name_div = "#txtTel2", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }

            if (tel3 != "" && tel3 != null) //tel3
            {
                var checkTel3 = common.IsNumeric(tel3);
                if (checkTel3 == false) { validates.Add(new validate_all { name_div = "#txtTel3", text = "รูปแบบหมายเลขโทรศัพท์ช่างไม่ถูกต้อง" }); }
            }

            if (email1 != "" && email1 != null) //email1
            {
                var checkEmail1 = common.isEmailFormat(email1);
                if (checkEmail1 == false) { validates.Add(new validate_all { name_div = "#txtEmail1", text = "รูปแบบอีเมล์ช่างไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEmail1", text = "กรุณาระบุอีเมล์ช่าง" }); }

            if (Int32.Parse(site_id) == 0 || Int32.Parse(site_id) != 0) //site_id
            {
                var checkSite_id = common.IsNumeric(Int32.Parse(site_id));
                if (checkSite_id == false) { validates.Add(new validate_all { name_div = "#alert_select_site", text = "กรุณาเลือกศูนย์บริการ" }); }
            }
            #endregion

            #region Check Role
            var convert_id = Int32.Parse(id);
            var data_store = db.tb_engineer.SingleOrDefault(w => w.id == convert_id);
            //check site
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data_store != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data_store.site_id == idStore ? true : false;
            }
            #endregion

            if (validates.Count() == 0 && (User.IsInRole("admin") || isSite))
            {
                #region Save Data
                data_store.user_update = name;
                data_store.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data_store.engineer_name = engineer_name;
                data_store.tel1 = tel1;
                data_store.tel2 = tel2;
                data_store.tel3 = tel3;
                data_store.email1 = email1;
                data_store.email2 = null;
                data_store.email3 = null;
                data_store.province = 0;
                data_store.repair_type_info1 = repair_type_info1;
                data_store.repair_type_info2 = repair_type_info2;
                data_store.repair_type_info3 = repair_type_info3;
                data_store.site_id = Int32.Parse(site_id);

                db.tb_engineer.AddOrUpdate(data_store);
                db.SaveChanges();
                #endregion

                #region Save Image
                if (Request.Files.Count > 0)
                {
                    HttpFileCollectionBase files = Request.Files;
                    string filename = files[0].FileName;
                    string url = ConfigurationManager.AppSettings["Api_url"].ToString();
                    var stream = files[0].InputStream;

                    var uri2 = url + "engineer/uploadavatar";
                    var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new StreamContent(stream), "\"fileupload_1\"", filename);


                    // EventModel other fields
                    multipartContent.Add(new StringContent(data_store.id.ToString()), "id_engi");
                    var httpClient = new HttpClient();
                    var httpResponseMessage = httpClient.PostAsync(uri2, multipartContent).Result;
                }
                else
                {
                    if(current_img == "0")
                    {
                        data_store.pictuce_path = null;
                        data_store.picture_name = null;
                        db.tb_engineer.AddOrUpdate(data_store);
                        db.SaveChanges();
                    }
                }
                #endregion

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
        public ActionResult reset_password(string new_password , string confirm_password, string code_engineer="", string guid_engineer = "")
        {
            List<validate_all> validates = new List<validate_all>();
            Regex regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})");
            Match match = regex.Match(new_password);
            Match match2 = regex.Match(confirm_password);
            if (new_password != "" && new_password != null) //engineer_name
            {
                var checkNewPassword = common.xss_input_string(new_password, new_password.Length);
                if (checkNewPassword == false || !match.Success) { validates.Add(new validate_all { name_div = "#NewPassword", text = "รหัสผ่านไม่ถูกต้อง" }); }
            }

            if (confirm_password != "" && confirm_password != null) //engineer_name
            {
                var checkConfirmPassword = common.xss_input_string(confirm_password, confirm_password.Length);
                if (checkConfirmPassword == false || !match2.Success) { validates.Add(new validate_all { name_div = "#txtConfirmPassword", text = "รหัสผ่านไม่ถูกต้อง" }); }
            }

            //check site
            var data_store = db.tb_engineer.SingleOrDefault(w => w.code_engineer == code_engineer);
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data_store != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data_store.site_id == idStore ? true : false;
            }

            if (validates.Count() == 0 && (User.IsInRole("admin") || isSite))
            {
                MembershipUser user_en = Membership.GetUser(code_engineer);
                var result = user_en.ChangePassword(user_en.ResetPassword(), new_password);
                if (result)
                {
                    
                    string url = WebConfigurationManager.AppSettings["Base_URL"];
                    return Redirect(url + "engineer/ps?id=" +guid_engineer+ "&success=1");
             
                    //engineer / ps ? id = bd4c355d - bcaf - 4ec6 - b9dd - 843ce9f75490
                    //return true;
                }
                else
                {
                    ViewData["error"] = "รหัสผ่านไม่ถูกต้อง";
                    ps(guid_engineer);
                    return View("ps");
                    //validates.Add(new validate_all { name_div = "#NewPassword", text = "รหัสผ่านไม่ถูกต้อง" });
                    //validates.Add(new validate_all { name_div = "#txtConfirmPassword", text = "รหัสผ่านไม่ถูกต้อง" });

                    //string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validates);
                    //return new ContentResult()
                    //{
                    //    Content = jsonString,
                    //    ContentType = "application/json"
                    //};
                }
            }
            else
            {
                ViewData["error"] = "รหัสผ่านไม่ถูกต้อง";
                ps(guid_engineer);
                return View("ps");
                //string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validates);
                //return new ContentResult()
                //{
                //    Content = jsonString,
                //    ContentType = "application/json"
                //};

            }

            

        }

        [HttpGet]
        public object btn_search_and_page(string en_name , int province, int site, int page)
        {
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;

            var data = new List<engineer>();
            if (User.IsInRole("admin"))
            {
                var pro_admin = province == 0 && site == 0 ? db.tb_engineer.Where(w => w.is_delete == 0).OrderByDescending(o => o.id ).ToList()
                : province != 0 && site != 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.province == province && w.site_id == site).OrderByDescending(o => o.id).ToList()
                : province != 0 && site == 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.province == province).OrderByDescending(o => o.id).ToList()
                : province == 0 && site != 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == site).OrderByDescending(o => o.id).ToList() : db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == site).OrderByDescending(o => o.id).ToList();
                
                data = en_name == "" ? (from e_all_admin in pro_admin
                                        orderby e_all_admin.id descending
                                        select new engineer()
                                            {
                                                id = e_all_admin.id,
                                                en_guid = e_all_admin.en_guid,
                                                en_name = e_all_admin.engineer_name,
                                                code_engineer = e_all_admin.code_engineer,
                                                shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e_all_admin.site_id select s.site_name).FirstOrDefault(),
                                                en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e_all_admin.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                                en_tel1 = e_all_admin.tel1,
                                                count = pro_admin.Count()
                                            }).Skip(skipPage).Take(20).ToList()
                                    :
                                    (from e_admin in pro_admin
                                     where e_admin.code_engineer.Contains(en_name.Trim()) || e_admin.engineer_name.Contains(en_name.Trim()) 
                                     orderby e_admin.id descending
                                     select new engineer()
                                     {
                                         id = e_admin.id,
                                         en_guid = e_admin.en_guid,
                                         en_name = e_admin.engineer_name,
                                         code_engineer = e_admin.code_engineer,
                                         shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e_admin.site_id select s.site_name).FirstOrDefault(),
                                         en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e_admin.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                         en_tel1 = e_admin.tel1,
                                         count = pro_admin.Where(w => w.engineer_name.Contains(en_name.Trim())).Count()
                                     }).Skip(skipPage).Take(20).ToList();
            }
            else
            {
                var getName = User.Identity.Name;

                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                var pro = province == 0 && site == 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == idStore).ToList()
                : province != 0 && site != 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.province == province && w.site_id == site && w.site_id == idStore).ToList()
                : province != 0 && site == 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.province == province && w.site_id == idStore).ToList()
                : province == 0 && site != 0 ? db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == site && w.site_id == idStore).ToList() : db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == site && w.site_id == idStore).ToList();


                data = en_name == "" ? (from e_all in pro
                                        where e_all.site_id == idStore
                                        orderby e_all.id descending
                                        select new engineer()
                                        {
                                            id = e_all.id,
                                            en_guid = e_all.en_guid,
                                            en_name = e_all.engineer_name,
                                            code_engineer = e_all.code_engineer,
                                            shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e_all.site_id select s.site_name).FirstOrDefault(),
                                            en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e_all.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                            en_tel1 = e_all.tel1,
                                            count = pro.Count()
                                        }).Skip(skipPage).Take(20).ToList()
                                    :
                                    (from e in pro
                                     where e.code_engineer.Contains(en_name.Trim()) || e.engineer_name.Contains(en_name.Trim()) && e.site_id == idStore
                                     orderby e.id descending
                                     select new engineer()
                                     {
                                         id = e.id,
                                         en_guid = e.en_guid,
                                         en_name = e.engineer_name,
                                         code_engineer = e.code_engineer,
                                         shopname = (from s in db.tb_store where s.is_delete == 0 && s.id == e.site_id select s.site_name).FirstOrDefault(),
                                         en_province = (from p in db.tb_provinces where p.province_id == (from s in db.tb_store where s.is_delete == 0 && s.id == e.site_id select s.province).FirstOrDefault() select p.province_name).FirstOrDefault(),
                                         en_tel1 = e.tel1,
                                         count = pro.Where(w => w.engineer_name.Contains(en_name.Trim()) && w.site_id == idStore).Count()
                                     }).Skip(skipPage).Take(20).ToList();
            }

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

        [HttpPost]
        public object sing_out_device()
        {
            var getName = User.Identity.Name;
            var idStore = 0;
            if (getName != "")
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
            }

            if ((User.IsInRole("admin") || idStore != 0))
            {
                var id = int.Parse(Request.Form["id"]);
                var name = User.Identity.Name;
                var data = db.tb_engineer.Where(w => w.id == id).FirstOrDefault();
                data.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data.user_update = name;
                data.mobile_imei_login = null;
                data.mobile_imei_lastUpdate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

                db.tb_engineer.AddOrUpdate(data);
                db.SaveChanges();
            }

            return true;
        }
    }
}