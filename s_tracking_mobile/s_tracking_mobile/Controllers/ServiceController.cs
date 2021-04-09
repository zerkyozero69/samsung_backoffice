using CommonLib;
using Microsoft.WindowsAzure.Storage;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class ServiceController : Controller
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        // GET: Service
        public ActionResult Get(Guid id)
        {
            ViewBag.roleUser = User.IsInRole("admin"); //Get Role
            var job = db.tb_jobs.Where(w => w.job_guid == id && w.is_delete == 0).FirstOrDefault();
            if (job != null)
            {
                var engineer = db.tb_engineer.Where(w => w.id == job.engineer_id && w.is_delete == 0).FirstOrDefault();
                var part = db.tb_jobs_parts.Where(w => w.job_id == job.id).ToList();

                var store = (from s in db.tb_store
                             where s.is_delete == 0 && s.id == job.store_id
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
                                 code_store = s.code_store
                             }).FirstOrDefault();

                ViewData["Azure_url"] = ConfigurationManager.AppSettings["Azure_url"];
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                ViewData["job"] = job;
                ViewData["engineer"] = engineer;
                ViewData["part"] = part;
                ViewData["store"] = store;
                string key_azure = GetKeyAzure();
                ViewData["Key_Azure"] = key_azure;
                return View("index");
            }
            return null;
        }

        public string GetKeyAzure(int ExtraTime = 0)
        {
            //string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AzureStorageConnectionString"].ConnectionString;
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
            //SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
            //{
            //    Permissions = SharedAccessAccountPermissions.Read,
            //    Services = SharedAccessAccountServices.Blob | SharedAccessAccountServices.File,
            //    ResourceTypes = SharedAccessAccountResourceTypes.Object,
            //    SharedAccessExpiryTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).AddMinutes(min),
            //    Protocols = SharedAccessProtocol.HttpsOnly
            //};
            //return storageAccount.GetSharedAccessSignature(policy);

            var date = DateTime.Now.AddMinutes(30).ToString();
            if (ExtraTime != 0)
            {
                date = DateTime.Now.AddDays(7).ToString();
            }

            string str_json = "{ \"key\"  : \"SecretKeyByi\" , \"exp\" : \"" + date + "\"}";
            string en_json = byi_common.encryption.Encrypt(str_json);
            return "?token=" + en_json;
        }

        //public void check_role()
        //{
        //    string url = ConfigurationManager.AppSettings["Path_CDN"];
        //    string login = url+"login";
        //    Response.Redirect(login);
        //}

        [HttpPost]
        public object firebaseUrl()
        {
            var getName = User.Identity.Name;
            var idStore = 0;
            string str_json = "{ \"status\"  : \"0\" , \"url\" : \"0\"}";
            if (getName != "")
            {
                Guid Checksite = (Guid)Membership.GetUser(getName).ProviderUserKey;
                idStore = db.tb_mapping_store.Where(w => w.account_guid == Checksite).Select(s => s.site_id).FirstOrDefault();
            }

            if (idStore != 0)
            {
                var url = "notifications/" + idStore.ToString();
                str_json = "{ \"status\" : " + 1 + ",\"url\" : \"" + url + "\"}";
            }
            
            return new ContentResult()
            {
                Content = str_json,
                ContentType = "application/json"
            };
        }
    }
}