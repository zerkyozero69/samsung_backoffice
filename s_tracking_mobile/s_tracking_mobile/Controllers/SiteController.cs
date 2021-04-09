using CommonLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Migrations;
using s_tracking_mobile.Models;
using byi_common;
using System.Web.Security;
using OfficeOpenXml;
using System.IO;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class SiteController : Controller
    {
        // GET: Site
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public void GetDataFromDb()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["Provinces"] = db.tb_provinces.ToList();
            ViewData["Districtes"] = db.tb_districts.ToList();
            ViewData["Sub_Districtes"] = db.tb_amphures.ToList();

        }

        public void GetDateTime()
        {
            List<times> time = new List<times>()
            {
                new times{ id = 1 , time = "08:00"},
                new times{ id = 2 , time = "08:30"},
                new times{ id = 3 , time = "09:00"},
                new times{ id = 4 , time = "09:30"},
                new times{ id = 5 , time = "10:00"},
                new times{ id = 6 , time = "10:30"},
                new times{ id = 7 , time = "11:00"},
                new times{ id = 8 , time = "11:30"},
                new times{ id = 9 , time = "12:00"},
                new times{ id = 10 , time = "12:30"},
                new times{ id = 11 , time = "13:00"},
                new times{ id = 12 , time = "13:30"},
                new times{ id = 13 , time = "14:00"},
                new times{ id = 14 , time = "14:30"},
                new times{ id = 15 , time = "15:00"},
                new times{ id = 16 , time = "15:30"},
                new times{ id = 17 , time = "16:00"},
                new times{ id = 18 , time = "16:30"},
                new times{ id = 19 , time = "17:00"},
                new times{ id = 20 , time = "17:30"},
                new times{ id = 21 , time = "18:00"},
                new times{ id = 22 , time = "18:30"},
                new times{ id = 23 , time = "19:00"},
                new times{ id = 24 , time = "19:30"},
                new times{ id = 25 , time = "20:00"},
                new times{ id = 26 , time = "20:30"},
                new times{ id = 27 , time = "21:00"},
                new times{ id = 28 , time = "21:30"},
                new times{ id = 29 , time = "22:00"},
                new times{ id = 30 , time = "22:30"},
                new times{ id = 31 , time = "23:00"},
                new times{ id = 32 , time = "23:30"},
                new times{ id = 33 , time = "24:00"}
            };

            List<date> date = new List<date>()
            {
                new date{id = 1 , day = "อาทิตย์"},
                new date{id = 2 , day = "จันทร์"},
                new date{id = 3 , day = "อังคาร"},
                new date{id = 4 , day = "พุธ"},
                new date{id = 5 , day = "พฤหัส"},
                new date{id = 6 , day = "ศุกร์"},
                new date{id = 7 , day = "เสาร์"}
            };

            ViewData["Date"] = date;
            ViewData["Time"] = time;
        }

        public ActionResult add()
        {
            if (User.IsInRole("admin"))
            {
                GetDataFromDb();
                GetDateTime();
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }

            return View();
        }

        public ActionResult all()
        {
            GetDataFromDb();

            if (User.IsInRole("admin"))
            {
                var obj_list_site = (from s in db.tb_store
                                     where s.is_delete == 0
                                     orderby s.id descending
                                     select new site()
                                     {
                                         id = s.id,
                                         store_guid = s.store_guid,
                                         store_name = s.site_name,
                                         store_address = s.site_address,
                                         store_village = s.village,
                                         store_moo = s.moo,
                                         store_street = s.street,
                                         store_sub_district = (from a in db.tb_amphures where a.amphur_id == s.sub_district select a.amphur_name).FirstOrDefault(),
                                         store_district = (from d in db.tb_districts where d.district_id == s.district select d.district_name).FirstOrDefault(),
                                         store_postcode = s.postcode,
                                         store_province = (from p in db.tb_provinces where p.province_id == s.province select p.province_name).FirstOrDefault(),
                                         store_contact1 = s.contact1,
                                         store_contact2 = s.contact2,
                                         store_contact3 = s.contact3,
                                         store_tel1 = s.tel1,
                                         store_tel2 = s.tel2,
                                         store_tel3 = s.tel3,
                                         store_email1 = s.email1,
                                         store_email12 = s.email2,
                                         store_email13 = s.email3
                                     });

                ViewData["All-Site"] = obj_list_site.Skip(0).Take(20).ToList();
                ViewData["Count-Data"] = obj_list_site.Count();
            }
            else
            {
                var getName = User.Identity.Name;
                if (getName != "")
                {
                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var obj_list_site = (from s in db.tb_store
                                         where s.is_delete == 0 && s.id == idStore
                                         orderby s.id descending
                                         select new site()
                                         {
                                             id = s.id,
                                             store_guid = s.store_guid,
                                             store_name = s.site_name,
                                             store_address = s.site_address,
                                             store_village = s.village,
                                             store_moo = s.moo,
                                             store_street = s.street,
                                             store_sub_district = (from a in db.tb_amphures where a.amphur_id == s.sub_district select a.amphur_name).FirstOrDefault(),
                                             store_district = (from d in db.tb_districts where d.district_id == s.district select d.district_name).FirstOrDefault(),
                                             store_postcode = s.postcode,
                                             store_province = (from p in db.tb_provinces where p.province_id == s.province select p.province_name).FirstOrDefault(),
                                             store_contact1 = s.contact1,
                                             store_contact2 = s.contact2,
                                             store_contact3 = s.contact3,
                                             store_tel1 = s.tel1,
                                             store_tel2 = s.tel2,
                                             store_tel3 = s.tel3,
                                             store_email1 = s.email1,
                                             store_email12 = s.email2,
                                             store_email13 = s.email3
                                         });

                    ViewData["All-Site"] = obj_list_site.Skip(0).Take(20).ToList();
                    ViewData["Count-Data"] = obj_list_site.Count();
                }
                else
                {
                    //wait redirect
                    var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                    Response.Redirect(url);
                }
            }
            return View();
        }

        public ActionResult edit(string id)
        {
            GetDataFromDb();
            GetDateTime();

            Guid check_Guid;
            bool isValid = Guid.TryParse(id, out check_Guid);

            if (isValid)
            {
                Guid newGuid = Guid.Parse(id);
                if (User.IsInRole("admin"))
                {
                    var data = db.tb_store.Where(w => w.store_guid == newGuid).FirstOrDefault();
                    if (data != null)
                    {
                        ViewData["Data"] = data;
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "site/all";
                        Response.Redirect(url);
                    }
                }
                else
                {
                    var getName = User.Identity.Name;

                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                    var data = db.tb_store.Where(w => w.store_guid == newGuid && w.id == idStore).FirstOrDefault();

                    if (data != null)
                    {
                        ViewData["Data"] = data;
                    }
                    else
                    {
                        //wait redirect
                        var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "site/all";
                        Response.Redirect(url);
                    }
                }
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "site/all";
                Response.Redirect(url);
            }

            return View();
        }

        [HttpPost]
        public object btn_save_store(tb_store data)
        {
            List<validate_all> validates = new List<validate_all>();
            var name = User.Identity.Name;
            //Check
            if (data.code_store != "" && data.code_store != null) //code
            {
                var checkCodeStore = common.xss_input_string(data.code_store, data.code_store.Length);
                if (checkCodeStore == false) { validates.Add(new validate_all { name_div = "#txtSiteCode", text = "รูปแบบโค้ดศูนย์บริการไม่ถูกต้อง" }); }
            } else { validates.Add(new validate_all { name_div = "#txtSiteCode", text = "กรุณาระบุโค้ดศูนย์บริการ" }); }

            if (data.site_name != "" && data.site_name != null) //sitename
            {
                var checkSiteName = common.xss_input_string(data.site_name, data.site_name.Length);
                if (checkSiteName == false) { validates.Add(new validate_all { name_div = "#txtSiteName", text = "รูปแบบชื่อศูนย์บริการไม่ถูกต้อง" }); }
            } else{ validates.Add(new validate_all { name_div = "#txtSiteName", text = "กรุณาระบุชื่อศูนย์บริการ" }); }

            if (data.contact1 != "" && data.contact1 != null) //contact1
            {
                var checkContact1 = common.xss_input_string(data.contact1, data.contact1.Length);
                if (checkContact1 == false) { validates.Add(new validate_all { name_div = "#txtContactName1", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            } else { validates.Add(new validate_all { name_div = "#txtContactName1", text = "กรุณาระบุผู้ติดต่อ" }); }

            if (data.contact2 != "" && data.contact2 != null) //contact2
            {
                var checkContact2 = common.xss_input_string(data.contact2, data.contact2.Length);
                if (checkContact2 == false) { validates.Add(new validate_all { name_div = "#txtContactName2", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            }

            if (data.contact3 != "" && data.contact3 != null) //contact3
            {
                var checkContact3 = common.xss_input_string(data.contact3, data.contact3.Length);
                if (checkContact3 == false) { validates.Add(new validate_all { name_div = "#txtContactName3", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            }

            if (data.tel1 != "" && data.tel1 != null) //tel1
            {
                var checkTel1 = common.IsNumeric(data.tel1);
                if (checkTel1 == false) { validates.Add(new validate_all { name_div = "#txtTel1", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            } else { validates.Add(new validate_all { name_div = "#txtTel1", text = "กรุณาระบุหมายเลขโทรศัพท์" }); }

            if (data.tel2 != "" && data.tel2 != null) //tel2
            {
                var checkTel2 = common.IsNumeric(data.tel2);
                if (checkTel2 == false) { validates.Add(new validate_all { name_div = "#txtTel2", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            }

            if (data.tel3 != "" && data.tel3 != null) //tel3
            {
                var checkTel3 = common.IsNumeric(data.tel3);
                if (checkTel3 == false) { validates.Add(new validate_all { name_div = "#txtTel3", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            }

            if (data.email1 != "" && data.email1 != null) //email1
            {
                var checkEmail1 = common.isEmailFormat(data.email1);
                if (checkEmail1 == false) { validates.Add(new validate_all { name_div = "#txtEmail1", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            } else { validates.Add(new validate_all { name_div = "#txtEmail1", text = "กรุณาระบุอีเมล์" }); }

            if (data.email2 != "" && data.email2 != null) //email2
            {
                var checkEmail2 = common.isEmailFormat(data.email2);
                if (checkEmail2 == false) { validates.Add(new validate_all { name_div = "#txtEmail2", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }

            if (data.email3 != "" && data.email3 != null) //email3
            {
                var checkEmail3 = common.isEmailFormat(data.email3);
                if (checkEmail3 == false) { validates.Add(new validate_all { name_div = "#txtEmail3", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }

            if (data.site_address != "" && data.site_address != null) //site_address
            {
                var checkSiteAddress = common.xss_input_string(data.site_address, data.site_address.Length);
                if (checkSiteAddress == false) { validates.Add(new validate_all { name_div = "#txtSiteAddressNo", text = "รูปแบบเลขที่อยู่ไม่ถูกต้อง" }); }
            }

            if (data.village != "" && data.village != null) //village
            {
                var checkVillage = common.xss_input_string(data.village, data.village.Length);
                if (checkVillage == false) { validates.Add(new validate_all { name_div = "#txtVillage", text = "รูปแบบหมู่บ้าน / ตึกไม่ถูกต้อง" }); }
            }

            if (data.moo != "" && data.moo != null) //moo
            {
                var checkMoo = common.xss_input_string(data.moo, data.moo.Length);
                if (checkMoo == false) { validates.Add(new validate_all { name_div = "#txtMoo", text = "รูปแบบหมู่ / ซอยไม่ถูกต้อง" }); }
            }

            if (data.street != "" && data.street != null) //street
            {
                var checkStreet = common.xss_input_string(data.street, data.street.Length);
                if (checkStreet == false) { validates.Add(new validate_all { name_div = "#txtStreet", text = "รูปแบบถนนไม่ถูกต้อง" }); }
            }

            if (data.sub_district != null) //sub_district
            {
                var checkSubDistrict = common.IsNumeric(data.sub_district);
                if (checkSubDistrict == false) { validates.Add(new validate_all { name_div = "#selectSubDistrict", text = "รูปแบบอำเภอ / เขตไม่ถูกต้อง" }); }
            }

            if (data.district != null) //district
            {
                var checkDistrict = common.IsNumeric(data.district);
                if (checkDistrict == false) { validates.Add(new validate_all { name_div = "#selectDistrict", text = "รูปแบบตำบล / แขวงไม่ถูกต้อง" }); }
            }

            if (data.province != null) //province
            {
                var checkProvince = common.IsNumeric(data.province);
                if (checkProvince == false) { validates.Add(new validate_all { name_div = "#selectProvince", text = "รูปแบบจังหวัดไม่ถูกต้อง" }); }
            }

            if (data.postcode != null) //postcode
            {
                var checkPostcode = common.IsNumeric(data.postcode);
                if (checkPostcode == false) { validates.Add(new validate_all { name_div = "#txtPostcode", text = "รูปแบบรหัสไปรษณีย์ไม่ถูกต้อง" }); }
            }

            if (data.store_lat != null) //store_lat
            {
                var checkStoreLat = common.xss_input_string(data.store_lat, data.store_lat.Length);
                if (checkStoreLat == false) { validates.Add(new validate_all { name_div = "#txtstore_lat", text = "รูปแบบไม่ถูกต้อง" }); }
            }

            if (data.store_long != null) //store_long
            {
                var checkStoreLong = common.xss_input_string(data.store_long, data.store_long.Length);
                if (checkStoreLong == false) { validates.Add(new validate_all { name_div = "#txtstore_long", text = "รูปแบบไม่ถูกต้อง" }); }
            }

            //Check
            //var getName = User.Identity.Name;
            //var idStore = 0;
            //if (getName != "")
            //{
            //    Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
            //    idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
            //}

            if (validates.Count() == 0 && User.IsInRole("admin"))
            {
                var CheckUser = Membership.GetUser(data.code_store);
                if (CheckUser == null)
                {
                    MembershipUser newUser = Membership.CreateUser(data.code_store, "Pa@sswd2019", data.email1);
                    Membership.UpdateUser(newUser);
                    Roles.AddUserToRole(data.code_store, "shop");
                }

                var getGuid = Membership.GetUser(data.code_store).ProviderUserKey;
                Guid convertGuid = new Guid(getGuid.ToString());

                Guid id = Guid.NewGuid();
                tb_store obj_new = new tb_store();
                obj_new.store_guid = id;
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.user_update = name;
                obj_new.is_delete = 0;
                obj_new.site_name = data.site_name;
                obj_new.code_store = data.code_store;
                obj_new.contact1 = data.contact1;
                obj_new.contact2 = data.contact2;
                obj_new.contact3 = data.contact3;
                obj_new.tel1 = data.tel1;
                obj_new.tel2 = data.tel2;
                obj_new.tel3 = data.tel3;
                obj_new.email1 = data.email1;
                obj_new.email2 = data.email2;
                obj_new.email3 = data.email3;
                obj_new.site_address = data.site_address;
                obj_new.village = data.village;
                obj_new.moo = data.moo;
                obj_new.street = data.street;
                obj_new.sub_district = data.sub_district;
                obj_new.district = data.district;
                obj_new.province = data.province;
                obj_new.country = data.country;
                obj_new.postcode = data.postcode;
                obj_new.store_close = data.store_close;
                obj_new.store_opendate1 = data.store_opendate1;
                obj_new.store_to_opendate1 = data.store_to_opendate1;
                obj_new.store_opentime1 = data.store_opentime1;
                obj_new.store_to_opentime1 = data.store_to_opentime1;
                obj_new.store_opendate2 = data.store_opendate2;
                obj_new.store_to_opendate2 = data.store_to_opendate2;
                obj_new.store_opentime2 = data.store_opentime2;
                obj_new.store_to_opentime2 = data.store_to_opentime2;
                obj_new.store_lat = data.store_lat == "" ? null : data.store_lat;
                obj_new.store_long = data.store_long == "" ? null : data.store_long;

                db.tb_store.Add(obj_new);
                db.SaveChanges();

                //set map
                tb_mapping_store obj_map = new tb_mapping_store();
                obj_map.account_guid = convertGuid;
                obj_map.site_id = obj_new.id;
                db.tb_mapping_store.Add(obj_map);
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
        public void btn_delete_store(int id)
        {
            var data = db.tb_store.SingleOrDefault(s => s.id == id);
            //check site
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.id == idStore ? true : false;
            }

            if ((User.IsInRole("admin") || isSite))
            {
                data.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data.is_delete = 1;
                db.tb_store.AddOrUpdate(data);
                db.SaveChanges();
            }
        }

        [HttpPost]
        public object btn_edit_store(tb_store data)
        {
            List<validate_all> validates = new List<validate_all>();
            var name = User.Identity.Name;
            //Check
            if (data.code_store != "" && data.code_store != null) //code
            {
                var checkCodeStore = common.xss_input_string(data.code_store, data.code_store.Length);
                if (checkCodeStore == false) { validates.Add(new validate_all { name_div = "#txtSiteCode", text = "รูปแบบโค้ดศูนย์บริการไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtSiteCode", text = "กรุณาระบุโค้ดศูนย์บริการ" }); }

            if (data.site_name != "" && data.site_name != null) //sitename
            {
                var checkSiteName = common.xss_input_string(data.site_name, data.site_name.Length);
                if (checkSiteName == false) { validates.Add(new validate_all { name_div = "#txtSiteName", text = "รูปแบบชื่อศูนย์บริการไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtSiteName", text = "กรุณาระบุชื่อศูนย์บริการ" }); }

            if (data.contact1 != "" && data.contact1 != null) //contact1
            {
                var checkContact1 = common.xss_input_string(data.contact1, data.contact1.Length);
                if (checkContact1 == false) { validates.Add(new validate_all { name_div = "#txtContactName1", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtContactName1", text = "กรุณาระบุผู้ติดต่อ" }); }

            if (data.contact2 != "" && data.contact2 != null) //contact2
            {
                var checkContact2 = common.xss_input_string(data.contact2, data.contact2.Length);
                if (checkContact2 == false) { validates.Add(new validate_all { name_div = "#txtContactName2", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            }

            if (data.contact3 != "" && data.contact3 != null) //contact3
            {
                var checkContact3 = common.xss_input_string(data.contact3, data.contact3.Length);
                if (checkContact3 == false) { validates.Add(new validate_all { name_div = "#txtContactName3", text = "รูปแบบผู้ติดต่อไม่ถูกต้อง" }); }
            }

            if (data.tel1 != "" && data.tel1 != null) //tel1
            {
                var checkTel1 = common.IsNumeric(data.tel1);
                if (checkTel1 == false) { validates.Add(new validate_all { name_div = "#txtTel1", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtTel1", text = "กรุณาระบุหมายเลขโทรศัพท์" }); }

            if (data.tel2 != "" && data.tel2 != null) //tel2
            {
                var checkTel2 = common.IsNumeric(data.tel2);
                if (checkTel2 == false) { validates.Add(new validate_all { name_div = "#txtTel2", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            }

            if (data.tel3 != "" && data.tel3 != null) //tel3
            {
                var checkTel3 = common.IsNumeric(data.tel3);
                if (checkTel3 == false) { validates.Add(new validate_all { name_div = "#txtTel3", text = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง" }); }
            }

            if (data.email1 != "" && data.email1 != null) //email1
            {
                var checkEmail1 = common.isEmailFormat(data.email1);
                if (checkEmail1 == false) { validates.Add(new validate_all { name_div = "#txtEmail1", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEmail1", text = "กรุณาระบุอีเมล์" }); }

            if (data.email2 != "" && data.email2 != null) //email2
            {
                var checkEmail2 = common.isEmailFormat(data.email2);
                if (checkEmail2 == false) { validates.Add(new validate_all { name_div = "#txtEmail2", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }

            if (data.email3 != "" && data.email3 != null) //email3
            {
                var checkEmail3 = common.isEmailFormat(data.email3);
                if (checkEmail3 == false) { validates.Add(new validate_all { name_div = "#txtEmail3", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }

            if (data.site_address != "" && data.site_address != null) //site_address
            {
                var checkSiteAddress = common.xss_input_string(data.site_address, data.site_address.Length);
                if (checkSiteAddress == false) { validates.Add(new validate_all { name_div = "#txtSiteAddressNo", text = "รูปแบบเลขที่อยู่ไม่ถูกต้อง" }); }
            }

            if (data.village != "" && data.village != null) //village
            {
                var checkVillage = common.xss_input_string(data.village, data.village.Length);
                if (checkVillage == false) { validates.Add(new validate_all { name_div = "#txtVillage", text = "รูปแบบหมู่บ้าน / ตึกไม่ถูกต้อง" }); }
            }

            if (data.moo != "" && data.moo != null) //moo
            {
                var checkMoo = common.xss_input_string(data.moo, data.moo.Length);
                if (checkMoo == false) { validates.Add(new validate_all { name_div = "#txtMoo", text = "รูปแบบหมู่ / ซอยไม่ถูกต้อง" }); }
            }

            if (data.street != "" && data.street != null) //street
            {
                var checkStreet = common.xss_input_string(data.street, data.street.Length);
                if (checkStreet == false) { validates.Add(new validate_all { name_div = "#txtStreet", text = "รูปแบบถนนไม่ถูกต้อง" }); }
            }

            if (data.sub_district != null) //sub_district
            {
                var checkSubDistrict = common.IsNumeric(data.sub_district);
                if (checkSubDistrict == false) { validates.Add(new validate_all { name_div = "#selectSubDistrict", text = "รูปแบบอำเภอ / เขตไม่ถูกต้อง" }); }
            }

            if (data.district != null) //district
            {
                var checkDistrict = common.IsNumeric(data.district);
                if (checkDistrict == false) { validates.Add(new validate_all { name_div = "#selectDistrict", text = "รูปแบบตำบล / แขวงไม่ถูกต้อง" }); }
            }

            if (data.province != null) //province
            {
                var checkProvince = common.IsNumeric(data.province);
                if (checkProvince == false) { validates.Add(new validate_all { name_div = "#selectProvince", text = "รูปแบบจังหวัดไม่ถูกต้อง" }); }
            }

            if (data.postcode != null) //postcode
            {
                var checkPostcode = common.IsNumeric(data.postcode);
                if (checkPostcode == false) { validates.Add(new validate_all { name_div = "#txtPostcode", text = "รูปแบบรหัสไปรษณีย์ไม่ถูกต้อง" }); }
            }

            if (data.store_lat != null) //store_lat
            {
                var checkStoreLat = common.xss_input_string(data.store_lat, data.store_lat.Length);
                if (checkStoreLat == false) { validates.Add(new validate_all { name_div = "#txtstore_lat", text = "รูปแบบไม่ถูกต้อง" }); }
            }

            if (data.store_long != null) //store_long
            {
                var checkStoreLong = common.xss_input_string(data.store_long, data.store_long.Length);
                if (checkStoreLong == false) { validates.Add(new validate_all { name_div = "#txtstore_long", text = "รูปแบบไม่ถูกต้อง" }); }
            }

            var data_store = db.tb_store.SingleOrDefault(s => s.id == data.id);
            //check site
            var getName = User.Identity.Name;
            var isSite = false;
            if (getName != "" && data_store != null)
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                isSite = data.id == idStore ? true : false;
            }

            if (validates.Count() == 0 && (User.IsInRole("admin") || isSite))
            {
                //var data_store = db.tb_store.SingleOrDefault(s => s.id == data.id);

                data_store.user_update = name;
                data_store.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                data_store.code_store = data.code_store;
                data_store.site_name = data.site_name;
                data_store.contact1 = data.contact1;
                data_store.contact2 = data.contact2;
                data_store.contact3 = data.contact3;
                data_store.tel1 = data.tel1;
                data_store.tel2 = data.tel2;
                data_store.tel3 = data.tel3;
                data_store.email1 = data.email1;
                data_store.email2 = data.email2;
                data_store.email3 = data.email3;
                data_store.site_address = data.site_address;
                data_store.village = data.village;
                data_store.moo = data.moo;
                data_store.street = data.street;
                data_store.sub_district = data.sub_district;
                data_store.district = data.district;
                data_store.province = data.province;
                data_store.country = data.country;
                data_store.postcode = data.postcode;
                data_store.store_close = data.store_close;
                data_store.store_opendate1 = data.store_opendate1;
                data_store.store_to_opendate1 = data.store_to_opendate1;
                data_store.store_opentime1 = data.store_opentime1;
                data_store.store_to_opentime1 = data.store_to_opentime1;
                data_store.store_opendate2 = data.store_opendate2;
                data_store.store_to_opendate2 = data.store_to_opendate2;
                data_store.store_opentime2 = data.store_opentime2;
                data_store.store_to_opentime2 = data.store_to_opentime2;
                data_store.store_lat = data.store_lat;
                data_store.store_long = data.store_long;

                db.tb_store.AddOrUpdate(data_store);
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
        public object btn_search_and_skip(int province, int page)
        {
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            var data = new List<site>();
            if (User.IsInRole("admin"))
            {
                data = province == 0 ? (from s_all in db.tb_store
                                        where s_all.is_delete == 0
                                        orderby s_all.id descending
                                        select new site()
                                        {
                                            id = s_all.id,
                                            store_guid = s_all.store_guid,
                                            store_name = s_all.site_name,
                                            store_address = s_all.site_address,
                                            store_village = s_all.village,
                                            store_moo = s_all.moo,
                                            store_street = s_all.street,
                                            store_sub_district = (from a in db.tb_amphures where a.amphur_id == s_all.sub_district select a.amphur_name).FirstOrDefault(),
                                            store_district = (from d in db.tb_districts where d.district_id == s_all.district select d.district_name).FirstOrDefault(),
                                            store_postcode = s_all.postcode,
                                            store_province = (from p in db.tb_provinces where p.province_id == s_all.province select p.province_name).FirstOrDefault(),
                                            store_contact1 = s_all.contact1,
                                            store_contact2 = s_all.contact2,
                                            store_contact3 = s_all.contact3,
                                            store_tel1 = s_all.tel1,
                                            store_tel2 = s_all.tel2,
                                            store_tel3 = s_all.tel3,
                                            store_email1 = s_all.email1,
                                            store_email12 = s_all.email2,
                                            store_email13 = s_all.email3,
                                            count = db.tb_store.Where(w => w.is_delete == 0).Count()
                                        }).Skip(skipPage).Take(20).ToList()
                                  :
                                  (from s in db.tb_store
                                   where s.is_delete == 0 && s.province == province
                                   orderby s.id descending
                                   select new site()
                                   {
                                       id = s.id,
                                       store_guid = s.store_guid,
                                       store_name = s.site_name,
                                       store_address = s.site_address,
                                       store_village = s.village,
                                       store_moo = s.moo,
                                       store_street = s.street,
                                       store_sub_district = (from a in db.tb_amphures where a.amphur_id == s.sub_district select a.amphur_name).FirstOrDefault(),
                                       store_district = (from d in db.tb_districts where d.district_id == s.district select d.district_name).FirstOrDefault(),
                                       store_postcode = s.postcode,
                                       store_province = (from p in db.tb_provinces where p.province_id == s.province select p.province_name).FirstOrDefault(),
                                       store_contact1 = s.contact1,
                                       store_contact2 = s.contact2,
                                       store_contact3 = s.contact3,
                                       store_tel1 = s.tel1,
                                       store_tel2 = s.tel2,
                                       store_tel3 = s.tel3,
                                       store_email1 = s.email1,
                                       store_email12 = s.email2,
                                       store_email13 = s.email3,
                                       count = db.tb_store.Where(w => w.is_delete == 0 && w.province == province).Count()
                                   }).Skip(skipPage).Take(20).ToList();
            }
            else
            {
                var getName = User.Identity.Name;
                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                Guid convertGuid = new Guid(CheckUser.ToString());
                var idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();

                data = province == 0 ? (from s_all in db.tb_store
                                        where s_all.is_delete == 0 && s_all.id == idStore
                                        orderby s_all.id descending
                                        select new site()
                                        {
                                            id = s_all.id,
                                            store_guid = s_all.store_guid,
                                            store_name = s_all.site_name,
                                            store_address = s_all.site_address,
                                            store_village = s_all.village,
                                            store_moo = s_all.moo,
                                            store_street = s_all.street,
                                            store_sub_district = (from a in db.tb_amphures where a.amphur_id == s_all.sub_district select a.amphur_name).FirstOrDefault(),
                                            store_district = (from d in db.tb_districts where d.district_id == s_all.district select d.district_name).FirstOrDefault(),
                                            store_postcode = s_all.postcode,
                                            store_province = (from p in db.tb_provinces where p.province_id == s_all.province select p.province_name).FirstOrDefault(),
                                            store_contact1 = s_all.contact1,
                                            store_contact2 = s_all.contact2,
                                            store_contact3 = s_all.contact3,
                                            store_tel1 = s_all.tel1,
                                            store_tel2 = s_all.tel2,
                                            store_tel3 = s_all.tel3,
                                            store_email1 = s_all.email1,
                                            store_email12 = s_all.email2,
                                            store_email13 = s_all.email3,
                                            count = db.tb_store.Where(w => w.is_delete == 0 && w.id == idStore).Count()
                                        }).Skip(skipPage).Take(20).ToList()
                                  :
                                  (from s in db.tb_store
                                   where s.is_delete == 0 && s.province == province && s.id == idStore
                                   orderby s.id descending
                                   select new site()
                                   {
                                       id = s.id,
                                       store_guid = s.store_guid,
                                       store_name = s.site_name,
                                       store_address = s.site_address,
                                       store_village = s.village,
                                       store_moo = s.moo,
                                       store_street = s.street,
                                       store_sub_district = (from a in db.tb_amphures where a.amphur_id == s.sub_district select a.amphur_name).FirstOrDefault(),
                                       store_district = (from d in db.tb_districts where d.district_id == s.district select d.district_name).FirstOrDefault(),
                                       store_postcode = s.postcode,
                                       store_province = (from p in db.tb_provinces where p.province_id == s.province select p.province_name).FirstOrDefault(),
                                       store_contact1 = s.contact1,
                                       store_contact2 = s.contact2,
                                       store_contact3 = s.contact3,
                                       store_tel1 = s.tel1,
                                       store_tel2 = s.tel2,
                                       store_tel3 = s.tel3,
                                       store_email1 = s.email1,
                                       store_email12 = s.email2,
                                       store_email13 = s.email3,
                                       count = db.tb_store.Where(w => w.is_delete == 0 && w.province == province && w.id == idStore).Count()
                                   }).Skip(skipPage).Take(20).ToList();
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