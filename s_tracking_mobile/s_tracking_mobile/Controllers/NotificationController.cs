using byi_common;
using CommonLib;
using Hangfire;
using Hangfire.Server;
using Newtonsoft.Json;
using RestSharp;
using s_tracking_mobile.App_Start;
using s_tracking_mobile.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        int show = 20; //เลือกจำนวน row ที่แสดง แก้ในฟังก์ชั่น pagination ด้วย

        // GET: Notification
        public ActionResult create()
        {
            if (User.Identity.Name != "")
            {
                ViewBag.name = User.Identity.Name;
                ViewBag.roleUser = User.IsInRole("admin"); //Get Role
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                ViewData["Site"] = db.tb_store.Where(w => w.is_delete == 0).ToList();

                if (User.IsInRole("admin"))
                {
                    ViewData["Engineer"] = db.tb_engineer.Where(w => w.is_delete == 0).ToList();
                }
                else
                {
                    var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                    int id_store = db.tb_mapping_store.Where(w => w.account_guid == userId).Select(s => s.site_id).FirstOrDefault();
                    ViewData["Engineer"] = db.tb_engineer.Where(w => w.is_delete == 0 && w.site_id == id_store).ToList();
                }
                return View();
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                return Redirect(url);
            }
        }

        public ActionResult all()
        {
            if (User.Identity.Name != "")
            {
                var user_name = User.Identity.Name;
                ViewBag.name = user_name;
                ViewBag.roleUser = User.IsInRole("admin"); //Get Role
                var minus = new TimeSpan(90, 0, 0, 0);
                DateTime start_senddate = DateTime.Today.Subtract(minus);
                DateTime end_senddate = DateTime.Today.AddDays(90);
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];

                if (User.IsInRole("admin"))
                {
                    ViewData["Data"] = (from n in db.tb_noti_campaign
                                        where n.is_delete == 0 && (n.send_date >= start_senddate && n.send_date <= end_senddate)
                                        select new notification
                                        {
                                            wait = DbFunctions.DiffMinutes(DateTime.Now, n.send_date),
                                            create_date = n.create_date,
                                            id = n.id,
                                            send_date = n.send_date,
                                            guid_noti = n.guid_noti,
                                            status = n.status,
                                            category = n.category,
                                            campaign_name = n.campaign_name,
                                            description = n.description,
                                            noti_status = ((CommonLib.Status_noti)n.status).ToString()
                                        }).OrderByDescending(s => s.send_date).Skip(0).Take(show).ToList();

                    ViewData["Count"] = (from c in db.tb_noti_campaign
                                         where c.is_delete == 0 && (c.send_date >= start_senddate && c.send_date <= end_senddate)
                                         select c).Count();
                }
                else
                {
                    var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                    ViewData["Data"] = (from n in db.tb_noti_campaign
                                        where n.is_delete == 0 && (n.send_date >= start_senddate && n.send_date <= end_senddate) && n.guid_owner == userId
                                        select new notification
                                        {
                                            wait = DbFunctions.DiffMinutes(DateTime.Now, n.send_date),
                                            create_date = n.create_date,
                                            id = n.id,
                                            send_date = n.send_date,
                                            guid_noti = n.guid_noti,
                                            status = n.status,
                                            category = n.category,
                                            campaign_name = n.campaign_name,
                                            description = n.description,
                                            noti_status = ((CommonLib.Status_noti)n.status).ToString()
                                        }).OrderByDescending(s => s.send_date).Skip(0).Take(show).ToList();

                    ViewData["Count"] = (from c in db.tb_noti_campaign
                                         where c.is_delete == 0 && (c.send_date >= start_senddate && c.send_date <= end_senddate) && c.guid_owner == userId
                                         select c).Count();
                }
                return View();
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                return Redirect(url);
            }
        }

        public ActionResult edit(Guid id)
        {
            tb_noti_campaign noti = (from e in db.tb_noti_campaign where e.guid_noti == id && e.is_delete == 0 select e).FirstOrDefault();
            if (User.Identity.Name != "" && noti != null)
            {
                ViewBag.name = User.Identity.Name;
                ViewBag.roleUser = User.IsInRole("admin"); //Get Role
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];

                var all_store = (from a in db.tb_store where a.is_delete == 0 select a).ToList();

                var store = (from ms in db.tb_mapping_noti
                             join s in db.tb_store on ms.store_id equals s.id
                             where ms.noti_id == noti.id && ms.store_id != 0 && ms.engineer_id == 0 && s.is_delete == 0
                             select s).ToList();

                var engineer = (from ms in db.tb_mapping_noti
                                join e in db.tb_engineer on ms.engineer_id equals e.id
                                where ms.noti_id == noti.id && ms.engineer_id != 0 && ms.store_id == 0 && e.is_delete == 0
                                select e).ToList();
                if (User.IsInRole("admin"))
                {
                    ViewData["Allengineer"] = (from a in db.tb_engineer where a.is_delete == 0 select a).ToList();
                }
                else
                {
                    var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                    int id_store = db.tb_mapping_store.Where(w => w.account_guid == userId).Select(s => s.site_id).FirstOrDefault();
                    ViewData["Allengineer"] = (from a in db.tb_engineer where a.is_delete == 0 && a.site_id == id_store select a).ToList();
                }
                ViewData["Data"] = noti;
                ViewData["Store"] = store;
                ViewData["Allstore"] = all_store;
                ViewData["Engineer"] = engineer;
                return View();
            }
            else
            {
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                return Redirect(url);
            }

        }

        [HttpGet]
        public object page_skip(string date, string end_date, string name = "", int status = 0, int page = 1)
        {
            DateTime start_senddate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_senddate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_senddate = new DateTime(start_senddate.Year, start_senddate.Month, start_senddate.Day, 0, 0, 0);
            end_senddate = new DateTime(end_senddate.Year, end_senddate.Month, end_senddate.Day, 0, 0, 0);

            if (page < 1) page = 1;
            var skip_page = (page - 1) * show;

            var noti_role = db.tb_noti_campaign.Where(w => w.is_delete == 0);
            if (User.IsInRole("admin"))
            {

            }
            else if (User.IsInRole("shop"))
            {
                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                noti_role = noti_role.Where(w => w.guid_owner == userId);
            }

            var db_noti = name == "" ? noti_role : noti_role.Where(w => w.campaign_name.Contains(name));
            db_noti = status == 0 ? db_noti : db_noti.Where(w => w.status == status);

            var data = (from n in db_noti
                        where (n.send_date >= start_senddate && n.send_date <= end_senddate)
                        select new
                        {
                            n.create_date,
                            n.id,
                            n.send_date,
                            n.guid_noti,
                            n.status,
                            n.category,
                            n.campaign_name,
                            n.description,
                            wait = DbFunctions.DiffMinutes(DateTime.Now, n.send_date),
                            noti_status = ((CommonLib.Status_noti)n.status).ToString(),
                            count = (from c in db_noti
                                     where (c.send_date >= start_senddate && c.send_date <= end_senddate)
                                     select c).Count()
                        }).OrderByDescending(s => s.send_date).Skip(skip_page).Take(show).ToList();

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return jsonString;
        }

        public object btn_search(string date, string end_date, string name = "", int status = 0)
        {
            DateTime start_senddate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_senddate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_senddate = new DateTime(start_senddate.Year, start_senddate.Month, start_senddate.Day, 0, 0, 0);
            end_senddate = new DateTime(end_senddate.Year, end_senddate.Month, end_senddate.Day, 0, 0, 0);

            var noti_role = db.tb_noti_campaign.Where(w => w.is_delete == 0);

            if (User.IsInRole("admin"))
            {

            }
            else if (User.IsInRole("shop"))
            {
                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                noti_role = noti_role.Where(w => w.guid_owner == userId);
            }

            var db_noti = name == "" ? noti_role : noti_role.Where(w => w.campaign_name.Contains(name));
            db_noti = status == 0 ? db_noti : db_noti.Where(w => w.status == status);

            var data = (from n in db_noti
                        where (n.send_date >= start_senddate && n.send_date <= end_senddate)
                        select new
                        {
                            n.create_date,
                            n.id,
                            n.send_date,
                            n.guid_noti,
                            n.status,
                            n.category,
                            n.campaign_name,
                            n.description,
                            wait = DbFunctions.DiffMinutes(DateTime.Now, n.send_date),
                            noti_status = ((CommonLib.Status_noti)n.status).ToString(),
                            count = (from c in db_noti
                                     where (c.send_date >= start_senddate && c.send_date <= end_senddate)
                                     select c).Count()
                        }).OrderByDescending(s => s.send_date).Skip(0).Take(show).ToList();

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return jsonString;
        }

        [HttpPost]
        public object btn_edit()
        {
            List<vali_noti> validates = new List<vali_noti>();

            bool check_int_id = int.TryParse(Request.Form["id"], out int form_id);
            if (check_int_id == false)
            {
                validates.Add(new vali_noti { name = "#title", text = "ID ผิดพลาด" });
            }

            edit_noti val = new edit_noti();
            val.id = form_id;
            val.campaign_name = Request.Form["campaign_name"];
            val.description = Request.Form["description"];
            val.send_date = Request.Form["send_date"];
            val.hour = Request.Form["hour"];
            val.minute = Request.Form["minute"];
            val.title = Request.Form["title"];
            val.body = Request.Form["body"];
            val.store = null;
            val.engineer = null;

            var string_store = Request.Form["store"];
            var string_engi = Request.Form["engineer"];
            if (string_store != "null")
            {
                string[] ar_store = string_store.Split(',');
                val.store = ar_store.ToList();
            }

            if (string_engi != "null")
            {
                string[] ar_engi = string_engi.Split(',');
                val.engineer = ar_engi.ToList();
            }

       
            //Check
            if (val.campaign_name.Trim() != "" && val.campaign_name != null) //code
            {
                var check = common.xss_input_string(val.campaign_name, val.campaign_name.Length);
                if (check == false) { validates.Add(new vali_noti { name = "#campaign_name", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#campaign_name", text = "กรุณาระบุ" }); }

            if (val.title.Trim() != "" && val.title != null) //code
            {
                var check = common.xss_input_string(val.title, val.title.Length);
                if (check == false) { validates.Add(new vali_noti { name = "#title", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#title", text = "กรุณาระบุ" }); }

            if (val.body.Trim() != "" && val.body != null) //code
            {
                //var check = common.xss_input_string(val.body, val.body.Length);
                //if (check == false) { validates.Add(new vali_noti { name = "#body", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#body", text = "กรุณาระบุ" }); }

            if (validates.Count() == 0)
            {
                var remove_map_noti = db.tb_mapping_noti.Where(w => w.noti_id == val.id).ToList();
                foreach (var d in remove_map_noti)
                {
                    db.tb_mapping_noti.Remove(d);
                    db.SaveChanges();
                }
                var noti = db.tb_noti_campaign.Where(w => w.id == val.id && w.is_delete == 0).FirstOrDefault();
                noti.id = val.id;
                noti.campaign_name = val.campaign_name;
                noti.description = val.description;
                noti.title = val.title;
                noti.body = val.body;
                string username = User.Identity.Name;
                noti.user_update = username;
                noti.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

                BackgroundJob.Delete(noti.hangfire_id);
                var string_date = val.send_date + " " + val.hour + ":" + val.minute;
                DateTime send_datetime = Convert.ToDateTime(string_date);
                noti.send_date = send_datetime;

                db.tb_noti_campaign.AddOrUpdate(noti);
                db.SaveChanges();
                tb_mapping_noti map_noti = new tb_mapping_noti();

                var site = val.store;
                if (site != null)
                {
                    if (User.IsInRole("admin"))
                    {

                        foreach (var i in site)
                        {

                            bool check_int = int.TryParse(i, out int id_site);
                            if (check_int)
                            {

                                map_noti.noti_id = val.id;
                                map_noti.engineer_id = 0;
                                //int id_site = Convert.ToInt32(i);
                                map_noti.store_id = id_site;
                                map_noti.is_read = 0;
                                db.tb_mapping_noti.Add(map_noti);
                                db.SaveChanges();

                                var engi_list = db.tb_engineer.Where(w => w.site_id == id_site && w.is_delete == 0).Select(s => s.id).ToList();
                                foreach (var j in engi_list)
                                {
                                    map_noti.noti_id = val.id;
                                    map_noti.engineer_id = j;
                                    map_noti.store_id = id_site;
                                    map_noti.is_read = 0;
                                    db.tb_mapping_noti.Add(map_noti);
                                    db.SaveChanges();
                                }

                            }
                        }
                    }
                }

                var engi = val.engineer;
                if (engi != null)
                {
                    foreach (var i in engi)
                    {
                        bool check_int = int.TryParse(i, out int id_engi);
                        if (check_int)
                        {
                            map_noti.noti_id = val.id;
                            map_noti.engineer_id = id_engi;
                            map_noti.store_id = 0;
                            map_noti.is_read = 0;
                            db.tb_mapping_noti.Add(map_noti);
                            db.SaveChanges();
                        }
                    }
                }

                var hangfire_id = BackgroundJob.Schedule(() => send_noti(val.id), send_datetime);
                var update_noti = db.tb_noti_campaign.Where(w => w.id == val.id && w.is_delete == 0).FirstOrDefault();
                update_noti.hangfire_id = hangfire_id;
                db.tb_noti_campaign.AddOrUpdate(update_noti);
                db.SaveChanges();
                return validates.Count;
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
        public object btn_delete_notification(int id = 0)
        {
            if (id != 0)
            {
                var data = db.tb_noti_campaign.SingleOrDefault(w => w.id == id && w.is_delete == 0);
                data.is_delete = 1;
                BackgroundJob.Delete(data.hangfire_id);
                db.tb_noti_campaign.AddOrUpdate(data);
                db.SaveChanges();
            }
            return true;
        }

        [HttpPost]
        public void send_noti(int id)
        {
            var noti_camp = db.tb_noti_campaign.Where(w => w.id == id && w.is_delete == 0).FirstOrDefault();
            if (noti_camp.is_delete != 1 && noti_camp != null)
            {
                var map_noti = (from map in db.tb_mapping_noti where map.noti_id == id && map.engineer_id != 0 select map).ToList();
                List<tb_mapping_noti> store_engi = new List<tb_mapping_noti>();
                List<tb_mapping_noti> engi = new List<tb_mapping_noti>();

                foreach (var i in map_noti)
                {
                    if (i.store_id != 0) store_engi.Add(i);
                    else engi.Add(i);
                }

                List<tb_mapping_noti> en_id_message = new List<tb_mapping_noti>();
                // check same engineer id
                foreach (var i in engi)
                {
                    var check = 0;
                    foreach (var j in store_engi)
                    {
                        if (i.engineer_id == j.engineer_id) check = 1;
                    }
                    if (check == 0) en_id_message.Add(i);
                }

                var url = "https://fcm.googleapis.com/fcm/send";
                var firebase_key = ConfigurationManager.AppSettings["firebase_key"];
                var client = new HttpClient();

                // store + engi -> sent noti to app
                store_engi.AddRange(en_id_message);
                foreach (var i in store_engi)
                {
                    var en_firebase_token = (from e in db.tb_engineer where e.id == i.engineer_id && e.is_delete == 0 select e.firebase_noti_token).FirstOrDefault();
                    firebase_noti content = new firebase_noti();
                    content.to = en_firebase_token;
                    content.priority = "high";
                    content.notification = new Dictionary<string, string>
                    {
                        { "body", noti_camp.body }, { "title", noti_camp.title }, {"icon", "WebIcon" }
                    };
                    content.data = new Dictionary<string, string>
                    {
                        { "noti_camp_id", Convert.ToString(noti_camp.id) },
                        { "send_date", Convert.ToString(noti_camp.send_date) },
                        { "user_update", noti_camp.user_update },
                        { "campaign_name", noti_camp.campaign_name },
                        { "description", noti_camp.description },
                        {"engineer_id", Convert.ToString(i.engineer_id)},
                        { "map_noti_id",Convert.ToString(i.id)},
                        { "is_read",Convert.ToString(i.is_read) }
                    };
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Headers = {
                        { HttpRequestHeader.Authorization.ToString(),"key "+firebase_key},
                        { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    },
                        Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
                    };
                    var response = client.SendAsync(request).Result;
                    tb_log log = new tb_log();
                    log.noti_id = id;
                    log.engineer_id = i.engineer_id;
                    log.send_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                    log.message_noti = noti_camp.title + " , " + noti_camp.body;
                    db.tb_log.Add(log);
                }
                noti_camp.status = 2; //ส่งแล้ว
                db.tb_noti_campaign.AddOrUpdate(noti_camp);
                db.SaveChanges();
            }
        }

        [HttpPost]
        public object btn_save()
        {

            create_noti item = new create_noti();

            item.campaign_name = Request.Form["campaign_name"];
            item.description = Request.Form["description"];
            item.send_date = Request.Form["send_date"];
            item.hour = Request.Form["hour"];
            item.minute = Request.Form["minute"];
            item.title = Request.Form["title"];
            item.body = Request.Form["body"];
            item.store = null;
            item.engineer = null;

            var string_store = Request.Form["store"];
            var string_engi = Request.Form["engineer"];
            if (string_store != "null")
            {
                string[] ar_store = string_store.Split(',');
                item.store = ar_store.ToList();
            }

            if (string_engi != "null")
            {
                string[] ar_engi = string_engi.Split(',');
                item.engineer = ar_engi.ToList();
            }


            List<vali_noti> validates = new List<vali_noti>();
            //Check
            if (item.campaign_name.Trim() != "" && item.campaign_name != null) //code
            {
                var check = common.xss_input_string(item.campaign_name, item.campaign_name.Length);
                if (check == false) { validates.Add(new vali_noti { name = "#campaign_name", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#campaign_name", text = "กรุณาระบุ" }); }

            if (item.title.Trim() != "" && item.title != null) //code
            {
                var check = common.xss_input_string(item.title, item.title.Length);
                if (check == false) { validates.Add(new vali_noti { name = "#title", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#title", text = "กรุณาระบุ" }); }

            if (item.body.Trim() != "" && item.body != null) //code
            {
                //var check = common.xss_input_string(item.body, item.body.Length);
                //if (check == false) { validates.Add(new vali_noti { name = "#body", text = "รูปแบบไม่ถูกต้อง" }); }
            }
            else { validates.Add(new vali_noti { name = "#body", text = "กรุณาระบุ" }); }

            if (validates.Count() == 0)
            {
                tb_noti_campaign obj_new = new tb_noti_campaign();
                Guid guid_id = Guid.NewGuid();
                obj_new.guid_noti = guid_id;
                Guid latest_noti = obj_new.guid_noti;
                obj_new.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                obj_new.is_delete = 0;
                obj_new.campaign_name = item.campaign_name;
                //obj_new.category = Convert.ToInt32(item.category);
                obj_new.description = item.description;
                obj_new.status = 1; // ยังไม่ส่ง
                obj_new.title = item.title;
                obj_new.body = item.body;

                var string_date = item.send_date + " " + item.hour + ":" + item.minute;
                DateTime send_datetime = Convert.ToDateTime(string_date);
                obj_new.send_date = send_datetime;
                obj_new.setorder = 0;

                string username = User.Identity.Name;
                obj_new.user_update = username;

                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                obj_new.guid_owner = userId;
                db.tb_noti_campaign.Add(obj_new);
                db.SaveChanges();

                tb_mapping_noti map_noti = new tb_mapping_noti();
                var site = item.store;
                int noti = db.tb_noti_campaign.Where(w => w.guid_noti == latest_noti && w.is_delete == 0).Select(s => s.id).FirstOrDefault();

                if (site != null)
                { 

                    if (User.IsInRole("admin"))
                    {
                        foreach (var i in site)
                        {


                            bool check_int = int.TryParse(i, out int id_site);
                            if (check_int)
                            {

                                map_noti.noti_id = noti;
                                map_noti.engineer_id = 0;
                                map_noti.store_id = id_site;
                                map_noti.is_read = 0;
                                db.tb_mapping_noti.Add(map_noti);
                                db.SaveChanges();

                                var engi_list = db.tb_engineer.Where(w => w.site_id == id_site && w.is_delete == 0).Select(s => s.id).ToList();
                                foreach (var j in engi_list)
                                {
                                    map_noti.noti_id = noti;
                                    map_noti.engineer_id = j;
                                    map_noti.store_id = id_site;
                                    map_noti.is_read = 0;
                                    db.tb_mapping_noti.Add(map_noti);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }

                var engi = item.engineer;
                if (engi != null)
                {
                    foreach (var i in engi)
                    {
                        bool check_int = int.TryParse(i, out int id_engi);
                        if (check_int)
                        {

                            map_noti.noti_id = noti;
                            map_noti.engineer_id = id_engi;
                            map_noti.store_id = 0;
                            map_noti.is_read = 0;
                            db.tb_mapping_noti.Add(map_noti);
                            db.SaveChanges();
                        }
                    }
                }

                var hangfire_id = BackgroundJob.Schedule(() => send_noti(noti), send_datetime);
                var update_noti = db.tb_noti_campaign.Where(w => w.id == noti && w.is_delete == 0).FirstOrDefault();
                update_noti.hangfire_id = hangfire_id;
                db.tb_noti_campaign.AddOrUpdate(update_noti);
                db.SaveChanges();
                return validates.Count;
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

        public async Task CheckJob()
        {
            string url_main = ConfigurationManager.AppSettings["Base_URL"];
            string str_delay = System.Configuration.ConfigurationManager.AppSettings["delay"];
            string str_noti_firebase = System.Configuration.ConfigurationManager.AppSettings["noti_firebase"];
            int delay = Convert.ToInt32(str_delay);
            TimeSpan minus = new TimeSpan(0, delay, 0);
            DateTime hour_ago = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Subtract(minus);
            var all_tb_job = db.tb_jobs.Where(w => w.is_delete == 0);

            var job_delay = all_tb_job.Where(w => w.appointment_datetime < hour_ago && w.appointment_to_datetime != null && (w.status_job == 0 || w.status_job == 6));
            foreach (var i in job_delay)
            {
                tb_notification objnew = new tb_notification();
                Guid newguid = Guid.NewGuid();
                objnew.noti_guid = newguid;
                objnew.setorder = 0;
                objnew.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.user_update = "admin";
                objnew.is_delete = 0;
                objnew.header = "แจ้งเตือน สถานะงานล่าช้า";
                objnew.detail = "หมายเลขแจ้งซ่อมที่ " + i.service_order_no + " พบการล่าช้า";
                objnew.is_read = 0;
                objnew.is_admin_read = 0;
                objnew.type_noti = 1;
                objnew.url_noti = url_main + "job/edit?id=" + i.job_guid;
                objnew.site_id = Convert.ToInt32(i.store_id);
                db.tb_notification.Add(objnew);

                i.status_job = 9;
                i.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                i.user_update = "admin";
                db.tb_jobs.AddOrUpdate(i);
            }

            string str_overtime = System.Configuration.ConfigurationManager.AppSettings["overtime"];
            int ot = Convert.ToInt32(str_overtime);
            minus = new TimeSpan(0, ot, 0);
            hour_ago = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Subtract(minus);

            var job_ot = all_tb_job.Where(w => w.job_repair < hour_ago && w.job_end == null && w.status_job == 8);
            foreach (var i in job_ot)
            {
                tb_notification objnew = new tb_notification();
                Guid newguid = Guid.NewGuid();
                objnew.noti_guid = newguid;
                objnew.setorder = 0;
                objnew.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.user_update = "admin";
                objnew.is_delete = 0;
                objnew.header = "แจ้งเตือน สถานะงานเลยเวลา";
                objnew.detail = "หมายเลขแจ้งซ่อมที่ " + i.service_order_no + " พบงานเลยเวลา";
                objnew.is_read = 0;
                objnew.is_admin_read = 0;
                objnew.type_noti = 2;
                objnew.url_noti = url_main + "job/edit?id=" + i.job_guid;
                objnew.site_id = Convert.ToInt32(i.store_id);
                db.tb_notification.Add(objnew);

                i.status_job = 10;
                i.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                i.user_update = "admin";
                db.tb_jobs.AddOrUpdate(i);
            }

            string longtime = System.Configuration.ConfigurationManager.AppSettings["LongTimeJourney"];
            int longtime_min = Convert.ToInt32(longtime);
            TimeSpan minus_longtime = new TimeSpan(0, longtime_min, 0);
            DateTime late = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Subtract(minus_longtime);

            var job_longtime = all_tb_job.Where(w => w.status_job == 7 && w.job_start < late && w.job_repair == null);
            foreach (var i in job_longtime)
            {
                tb_notification objnew = new tb_notification();
                Guid newguid = Guid.NewGuid();
                objnew.noti_guid = newguid;
                objnew.setorder = 0;
                objnew.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                objnew.user_update = "admin";
                objnew.is_delete = 0;
                objnew.header = "แจ้งเตือน สถานะงานเดินทางเกินเวลา";
                objnew.detail = "หมายเลขแจ้งซ่อมที่ " + i.service_order_no + " พบการเดินทางเกินเวลา";
                objnew.is_read = 0;
                objnew.is_admin_read = 0;
                objnew.type_noti = 3;
                objnew.url_noti = url_main + "job/edit?id=" + i.job_guid;
                objnew.site_id = Convert.ToInt32(i.store_id);
                db.tb_notification.Add(objnew);

                i.status_job = 11;
                i.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                i.user_update = "admin";
                db.tb_jobs.AddOrUpdate(i);
            }
            db.SaveChanges();

            //get Token
            var user_login = new
            {
                email = "byi@gmail.com",
                password = "1111111111",
                returnSecureToken = true
            };
            var jsonString_api = JsonConvert.SerializeObject(user_login);
            var client_api = new RestClient("https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyPassword?key=AIzaSyBqTR0TesxyI3cu68DOuOz4dlYiNGna1Pg");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", jsonString_api, ParameterType.RequestBody);
            IRestResponse response_api = client_api.Execute(request);
            api_firebase_auth data_api = JsonConvert.DeserializeObject<api_firebase_auth>(response_api.Content);
            //

            var top_ten = db.tb_notification.Where(w => w.is_delete == 0).OrderByDescending(s => s.update_date).Take(10);
            var separate_site = (from n in top_ten group n by n.site_id into site_group select new { site = site_group.Key });
            foreach (var i in separate_site)
            {
                var noti_by_id = top_ten.Where(w => w.site_id == i.site && w.is_delete == 0).ToList();
                using (var client = new HttpClient())
                {
                    var jsonString = JsonConvert.SerializeObject(noti_by_id);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                    string uri = str_noti_firebase + i.site + "/.json/?auth=" + data_api.idToken;
                    HttpResponseMessage response = await client.PutAsync(uri, content);
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public ActionResult all_notification()
        {
            ViewBag.roleUser = User.IsInRole("admin");
            var minus = new TimeSpan(90, 0, 0, 0);
            DateTime start_senddate = DateTime.Today.Subtract(minus);
            DateTime end_senddate = DateTime.Today.AddDays(90);
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];

            var filter_noti = db.tb_notification.Where(w => w.is_delete == 0 && (w.create_date >= start_senddate && w.create_date <= end_senddate));

            if (User.IsInRole("admin"))
            {
                //nothing to do
            }
            else
            {
                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                int id_store = db.tb_mapping_store.Where(w => w.account_guid == userId).Select(s => s.site_id).FirstOrDefault();
                filter_noti = filter_noti.Where(w => w.site_id == id_store);
            }

            ViewData["Data"] = (from n in filter_noti
                                select new all_notification
                                {
                                    create_date = n.create_date,
                                    id = n.id,
                                    site_id = db.tb_store.Where(w => w.id == n.site_id && w.is_delete == 0).Select(s => s.site_name).FirstOrDefault(),
                                    user_update = n.user_update,
                                    is_read = n.is_read,
                                    header = n.header,
                                    detail = n.detail,
                                    noti_status = ((CommonLib.Status_noti_job)n.type_noti).ToString(),
                                }).OrderByDescending(s => s.create_date).Skip(0).Take(show).ToList();
            ViewData["Count"] = (from c in filter_noti select c).Count();
            return View();
        }

        [HttpGet]
        public object page_skip_all_noti(string date, string str_end_date, int is_read = 10, int type_noti = 10, int page = 1)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(str_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            if (page < 1) { page = 1; }
            var skip_page = (page - 1) * show;

            var db_noti = db.tb_notification.Where(w => w.is_delete == 0 && (w.create_date >= start_date && w.create_date <= end_date));

            if (User.IsInRole("admin"))
            {
                //nothing to do
            }
            else if (User.IsInRole("shop"))
            {
                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                int id_store = db.tb_mapping_store.Where(w => w.account_guid == userId).Select(s => s.site_id).FirstOrDefault();
                db_noti = db_noti.Where(w => w.site_id == id_store);
            }

            if (is_read == 0) db_noti = db_noti.Where(w => w.is_delete == 0 && w.is_read == 0);
            else if (is_read == 1) db_noti = db_noti.Where(w => w.is_delete == 0 && w.is_read == 1);

            if (type_noti == 1) db_noti = db_noti.Where(w => w.type_noti == 1);
            else if (type_noti == 2) db_noti = db_noti.Where(w => w.type_noti == 2);
            else if (type_noti == 3) db_noti = db_noti.Where(w => w.type_noti == 3);

            var data = (from n in db_noti
                        select new
                        {
                            n.create_date,
                            n.id,
                            site_id = db.tb_store.Where(w => w.id == n.site_id && w.is_delete == 0).Select(s => s.site_name).FirstOrDefault(),
                            n.user_update,
                            n.is_read,
                            n.header,
                            n.detail,
                            noti_status = ((CommonLib.Status_noti_job)n.type_noti).ToString(),
                            count = (from c in db_noti select c).Count()
                        }).OrderByDescending(s => s.create_date).Skip(skip_page).Take(show).ToList();

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return jsonString;
        }

        public object btn_search_all_noti(string date, string str_end_date, int is_read = 10, int type_noti = 10)
        {
            DateTime start_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
            DateTime end_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(str_end_date, new string[] { "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1).Date, zone);

            start_date = new DateTime(start_date.Year, start_date.Month, start_date.Day, 0, 0, 0);
            end_date = new DateTime(end_date.Year, end_date.Month, end_date.Day, 0, 0, 0);

            var db_noti = db.tb_notification.Where(w => w.is_delete == 0 && (w.create_date >= start_date && w.create_date <= end_date));

            if (User.IsInRole("admin"))
            {
                //nothing to do
            }
            else if (User.IsInRole("shop"))
            {
                var userId = (Guid)Membership.GetUser(User.Identity.Name).ProviderUserKey;
                int id_store = db.tb_mapping_store.Where(w => w.account_guid == userId).Select(s => s.site_id).FirstOrDefault();
                db_noti = db_noti.Where(w => w.site_id == id_store);
            }

            if (is_read == 0) db_noti = db_noti.Where(w => w.is_delete == 0 && w.is_read == 0);
            else if (is_read == 1) db_noti = db_noti.Where(w => w.is_delete == 0 && w.is_read == 1);

            if (type_noti == 1) db_noti = db_noti.Where(w => w.type_noti == 1);
            else if (type_noti == 2) db_noti = db_noti.Where(w => w.type_noti == 2);
            else if (type_noti == 3) db_noti = db_noti.Where(w => w.type_noti == 3);

            var data = (from n in db_noti
                        select new
                        {
                            n.create_date,
                            n.id,
                            site_id = db.tb_store.Where(w => w.id == n.site_id && w.is_delete == 0).Select(s => s.site_name).FirstOrDefault(),
                            n.user_update,
                            n.is_read,
                            n.header,
                            n.detail,
                            noti_status = ((CommonLib.Status_noti_job)n.type_noti).ToString(),
                            count = (from c in db_noti select c).Count()
                        }).OrderByDescending(s => s.create_date).Skip(0).Take(show).ToList();

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return jsonString;
        }

        [HttpPost]
        public async Task<object> readNotificationAsync(bool isAdmin, int id, int site, int idx)
        {
            var data = db.tb_notification.SingleOrDefault(w => w.id == id && w.is_delete == 0);
            string str_noti_firebase = System.Configuration.ConfigurationManager.AppSettings["noti_firebase"];
            string jsonString = "{\"data\":\"false\"}";
            if (data != null)
            {
                var getName = User.Identity.Name;
                var isSite = false;
                if (getName != "")
                {
                    Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                    var idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
                    isSite = data.site_id == idStore ? true : false;
                }

                if (User.IsInRole("admin") || isSite)
                {
                    //get Token

                    var user_login = new
                    {
                        email = "byi@gmail.com",
                        password = "1111111111",
                        returnSecureToken = true
                    };

                    var jsonString_api = JsonConvert.SerializeObject(user_login);
                    var client_api = new RestClient("https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyPassword?key=AIzaSyBqTR0TesxyI3cu68DOuOz4dlYiNGna1Pg");
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddParameter("undefined", jsonString_api, ParameterType.RequestBody);
                    IRestResponse response_api = client_api.Execute(request);
                    api_firebase_auth data_api = JsonConvert.DeserializeObject<api_firebase_auth>(response_api.Content);

                    var test_data = new
                    {
                        create_date = data.create_date,
                        detail = data.detail,
                        header = data.header,
                        id = data.id,
                        is_admin_read = isAdmin ? 1 : data.is_admin_read,
                        is_delete = data.is_delete,
                        is_read = isAdmin ? data.is_read : 1,
                        noti_guid = data.noti_guid,
                        setorder = data.setorder,
                        site_id = data.site_id,
                        type_noti = data.type_noti,
                        update_date = data.update_date,
                        url_noti = data.url_noti,
                        user_update = data.user_update
                    };

                    using (var client = new HttpClient())
                    {
                        var jsonString_data = JsonConvert.SerializeObject(test_data);
                        var content = new StringContent(jsonString_data, Encoding.UTF8, "application/json");
                        string uri = str_noti_firebase + site + "/" + idx + "/" + ".json/?auth=" + data_api.idToken;
                        HttpResponseMessage response = await client.PutAsync(uri, content);
                        response.EnsureSuccessStatusCode();
                    }

                    if (isAdmin)
                    {
                        data.is_admin_read = 1;
                        db.tb_notification.AddOrUpdate(data);
                        db.SaveChanges();
                    }
                    else
                    {
                        data.is_read = 1;
                        db.tb_notification.AddOrUpdate(data);
                        db.SaveChanges();
                    }

                    jsonString = "{\"data\":\"pass\"}";

                }

            }
            else
            {
                jsonString = "{\"data\":\"false\"}";
            }

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }
    }
}