using CommonLib;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json; //test import
using OfficeOpenXml; //test import
using RestSharp; //test import
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations; //test import
using System.Globalization; //test import
using System.IO; //test import
using System.Linq;
using System.Text.RegularExpressions; //test import
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class StandardController : Controller
    {
        // GET: Standard
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();

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

        //public ActionResult Test_import()
        //{
        //    ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
        //    return View();
        //}

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

        public string GetBodyLogin(string username, string password, bool remember)
        {

            var date = DateTime.Now.AddMinutes(1).ToString();
            string str_json = "{ \"key\"  : \"SecretKeyByi\" , \"exp\" : \"" + date + "\" , \"username\" : \"" + username + "\" , \"password\" : \"" + password + "\" , \"remember\" : \"" + remember + "\"}";
            string en_json = byi_common.encryption.Encrypt(str_json);
            return en_json;
        }

        public string GetLogout()
        {

            var date = DateTime.Now.AddMinutes(1).ToString();
            string str_json = "{ \"key\"  : \"SecretKeyByi\" , \"exp\" : \"" + date + "\"}";
            string en_json = byi_common.encryption.Encrypt(str_json);
            return en_json;
        }

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

        //test method import controller job
        [HttpPost]
        public object import_file()
        {
            var excelfile = Request.Files[0];
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            var save_job = new List<job>();
            //check file import
            if (excelfile == null || excelfile.ContentLength == 0)
            {
                ViewBag.Error = "Please select a excel file";
                return View();
            }
            else
            {
                //check type file excel
                if (excelfile.FileName.EndsWith("xlsx"))
                {
                    Guid nameFile = Guid.NewGuid();
                    string path = Server.MapPath("~/import/" + nameFile.ToString() + excelfile.FileName);

                    //check Duplicate file and delete
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    excelfile.SaveAs(path);
                    FileInfo excel2 = new FileInfo(Server.MapPath("~/import/" + nameFile.ToString() + excelfile.FileName));
                    using (var package = new ExcelPackage(excel2))
                    {
                        //logic import
                        var workbook = package.Workbook;
                        var worksheet = workbook.Worksheets.First();
                        int totalRows = worksheet.Dimension.End.Row;
                        int totalColumn = worksheet.Dimension.End.Column;

                        var error_serial = "";
                        var getName = User.Identity.Name;
                        Guid CheckUser = (Guid)Membership.GetUser(getName).ProviderUserKey;
                        //get id store user login
                        var idStore = db.tb_mapping_store.Where(w => w.account_guid == CheckUser).Select(s => s.site_id).FirstOrDefault();

                        //set int column
                        var col_code_store = 0; var col_service_order = 0; var col_code_engineer = 0; var col_code_category = 0; var col_code_model = 0;
                        var col_serial_no = 0; var col_status_code = 0; var col_status = 0; var col_reason_code = 0; var col_reason = 0;
                        var col_customer_fullname = 0; var col_street = 0; var col_city = 0; var col_district = 0; var col_zipcode = 0;
                        var col_phone_home = 0; var col_phone_office = 0; var col_phone_mobile = 0; var col_service_type = 0; var col_warranty_flag = 0;
                        var col_remark = 0; var col_repair_description = 0; var col_defect_description = 0; var col_status_comment = 0; var col_customer_comment = 0;
                        var col_store_name = 0; var col_engineer_name = 0; var col_name_category = 0; var col_request_date = 0; var col_purchase_date = 0;
                        var col_getParts = 0; var col_1st_appointment_date = 0; var col_1st_appointment_time = 0; var col_last_appointment_date = 0;
                        var col_customer_date = 0; var col_customer_time = 0;

                        //get header column
                        for (int i = 1; i <= totalColumn; i++)
                        {
                            var header = worksheet.Cells[1, i];
                            if (header.Value != null)
                            {
                                var text = header.Value.ToString().Replace("  ", String.Empty);
                                text = Regex.Replace(text, @"\s+", String.Empty).ToLower().Trim();
                                //sort by excel
                                switch (text)
                                {
                                    case "serviceorderno.": col_service_order = i; break;
                                    case "asccode": col_code_store = i; break;
                                    case "ascname": col_store_name = i; break;
                                    case "purchasedate": col_purchase_date = i; break; // datetime
                                    case "model": col_code_model = i; break;
                                    case "serialno": col_serial_no = i; break;
                                    case "statuscode": col_status_code = i; break;
                                    case "status": col_status = i; break;
                                    case "reasoncode": col_reason_code = i; break;
                                    case "reason": col_reason = i; break;
                                    case "customername": col_customer_fullname = i; break;
                                    case "street": col_street = i; break;
                                    case "city": col_city = i; break;
                                    case "district": col_district = i; break;
                                    case "zipcode": col_zipcode = i; break;
                                    case "requestdate": col_request_date = i; break; // datetime
                                    case "customerpreferreddate": col_customer_date = i; break; // datetime
                                    case "customerpreferredtime": col_customer_time = i; break; // datetime
                                    case "asc1stappointmentdate": col_1st_appointment_date = i; break; //datetime
                                    case "asc1stappointmenttime": col_1st_appointment_time = i; break; //datetime
                                    case "asclastappointmentdate": col_last_appointment_date = i; break; //datetime
                                    case "phoneno(home)": col_phone_home = i; break;
                                    case "phoneno(office)": col_phone_office = i; break;
                                    case "phoneno(mobile)": col_phone_mobile = i; break;
                                    case "servicetype": col_service_type = i; break;
                                    case "inoutwarrantyflag": col_warranty_flag = i; break;
                                    case "engineercode": col_code_engineer = i; break;
                                    case "engineername": col_engineer_name = i; break;
                                    case "remark": col_remark = i; break;
                                    case "repairdescription": col_repair_description = i; break;
                                    case "defectdescription": col_defect_description = i; break;
                                    case "statuscomment": col_status_comment = i; break;
                                    case "customercomment": col_customer_comment = i; break;
                                    case "serviceproductcode": col_code_category = i; col_name_category = (i + 1); break;
                                    case "serviceproductdescription": col_name_category = i; break;
                                    case "partsrepairlocation01": col_getParts = i; break; //parts
                                }
                            }
                        }

                        //check header column isvalid
                        if (col_code_store == 0 || col_service_order == 0 || col_store_name == 0 || col_code_engineer == 0 || col_engineer_name == 0 || col_code_category == 0 || col_name_category == 0 || col_code_model == 0 || col_serial_no == 0 || col_status_code == 0 || col_status == 0 ||
                            col_reason_code == 0 || col_reason == 0 || col_customer_fullname == 0 || col_street == 0 || col_city == 0 || col_district == 0 || col_zipcode == 0 || col_phone_home == 0 || col_phone_office == 0 || col_phone_mobile == 0 || col_service_type == 0 ||
                            col_warranty_flag == 0 || col_remark == 0 || col_repair_description == 0 || col_defect_description == 0 || col_status_comment == 0 || col_customer_comment == 0 || col_request_date == 0 || col_purchase_date == 0 || col_getParts == 0 ||
                            col_1st_appointment_date == 0 || col_1st_appointment_time == 0 || col_last_appointment_date == 0 || col_customer_date == 0 || col_customer_time == 0)
                        {
                            var return_data = new List<error_import>();
                            var text = new List<string>();

                            if (col_code_store == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Code");
                            if (col_service_order == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Order No.");
                            if (col_store_name == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Name");
                            if (col_code_engineer == 0) text.Insert(0, "ไม่พบคอลัมน์ Engineer Code");
                            if (col_engineer_name == 0) text.Insert(0, "ไม่พบคอลัมน์ Engineer Name");
                            if (col_code_category == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Product Code");
                            if (col_code_model == 0) text.Insert(0, "ไม่พบคอลัมน์ Model");
                            if (col_serial_no == 0) text.Insert(0, "ไม่พบคอลัมน์ Serial No");
                            if (col_status_code == 0) text.Insert(0, "ไม่พบคอลัมน์ Status Code");
                            if (col_status == 0) text.Insert(0, "ไม่พบคอลัมน์ Status");
                            if (col_reason_code == 0) text.Insert(0, "ไม่พบคอลัมน์ Reason Code");
                            if (col_reason == 0) text.Insert(0, "ไม่พบคอลัมน์ Reason");
                            if (col_customer_fullname == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Name");
                            if (col_street == 0) text.Insert(0, "ไม่พบคอลัมน์ Street");
                            if (col_city == 0) text.Insert(0, "ไม่พบคอลัมน์ City");
                            if (col_district == 0) text.Insert(0, "ไม่พบคอลัมน์ District");
                            if (col_zipcode == 0) text.Insert(0, "ไม่พบคอลัมน์ Zip Code");
                            if (col_phone_home == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(home)");
                            if (col_phone_office == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(office)");
                            if (col_phone_mobile == 0) text.Insert(0, "ไม่พบคอลัมน์ Phone No(mobile)");
                            if (col_service_type == 0) text.Insert(0, "ไม่พบคอลัมน์ Service Type");
                            if (col_warranty_flag == 0) text.Insert(0, "ไม่พบคอลัมน์ In Out Warranty Flag");
                            if (col_remark == 0) text.Insert(0, "ไม่พบคอลัมน์ Remark");
                            if (col_repair_description == 0) text.Insert(0, "ไม่พบคอลัมน์ Repair Description");
                            if (col_defect_description == 0) text.Insert(0, "ไม่พบคอลัมน์ Defect Description");
                            if (col_status_comment == 0) text.Insert(0, "ไม่พบคอลัมน์ Status comment");
                            if (col_customer_comment == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Comment");
                            if (col_request_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Request Date");
                            if (col_purchase_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Purchase Date");
                            if (col_getParts == 0) text.Insert(0, "ไม่พบคอลัมน์ Parts Repair location 01");
                            if (col_1st_appointment_date == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC 1st Appointment Date");
                            if (col_1st_appointment_time == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC 1st Appointment Time");
                            if (col_last_appointment_date == 0) text.Insert(0, "ไม่พบคอลัมน์ ASC Last Appointment Date");
                            if (col_customer_date == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Preferred Date");
                            if (col_customer_time == 0) text.Insert(0, "ไม่พบคอลัมน์ Customer Preferred Time");

                            foreach (string value in text)
                            {
                                return_data.Add(new error_import
                                {
                                    text = value
                                });
                            }

                            string jsonString2 = Newtonsoft.Json.JsonConvert.SerializeObject(return_data);
                            return new ContentResult()
                            {
                                Content = jsonString2,
                                ContentType = "application/json"
                            };
                        }

                        //loop logic save
                        var isNew = false;
                        for (int row = 2; row <= totalRows; row++)
                        {
                            var ex_serviceOrder = worksheet.Cells[row, col_service_order].Text.ToString().Trim();
                            if(ex_serviceOrder != "")
                            {
                                Guid new_Guid_job = Guid.NewGuid(); // guid new job
                                var text_error = "";
                                var table_error = "";
                                var return_id = "";
                                //get data from excel

                                //nomal data sort by column excel
                                var ex_StoreCode = worksheet.Cells[row, col_code_store].Text.ToString().Trim();
                                var ex_storeName = worksheet.Cells[row, col_store_name].Text.ToString().Trim();
                                var ex_model = worksheet.Cells[row, col_code_model].Text.ToString().Trim();
                                var ex_serialNo = worksheet.Cells[row, col_serial_no].Text.ToString().Trim();
                                var ex_statusCode = worksheet.Cells[row, col_status_code].Text.ToString().Trim();
                                var ex_status = worksheet.Cells[row, col_status].Text.ToString().Trim();
                                var ex_reasonCode = worksheet.Cells[row, col_reason_code].Text.ToString().Trim();
                                var ex_reason = worksheet.Cells[row, col_reason].Text.ToString().Trim();
                                var ex_customerName = worksheet.Cells[row, col_customer_fullname].Text.ToString().Trim();
                                var ex_street = worksheet.Cells[row, col_street].Text.ToString().Trim();
                                var ex_city = worksheet.Cells[row, col_city].Text.ToString().Trim();
                                var ex_district = worksheet.Cells[row, col_district].Text.ToString().Trim();
                                int ex_zipcode = worksheet.Cells[row, col_zipcode].Text == "" ? 0 : int.Parse(worksheet.Cells[row, col_zipcode].Text);
                                var ex_phoneHome = worksheet.Cells[row, col_phone_home].Text.ToString().Trim();
                                var ex_phoneOffice = worksheet.Cells[row, col_phone_office].Text.ToString().Trim();
                                var ex_phoneMobile = worksheet.Cells[row, col_phone_mobile].Text.ToString().Trim();
                                var ex_serviceType = worksheet.Cells[row, col_service_type].Text.ToString().Trim();
                                var ex_warrantyFlag = worksheet.Cells[row, col_warranty_flag].Text.ToString().Trim();
                                var ex_engineerCode = worksheet.Cells[row, col_code_engineer].Text.ToString().Trim();
                                var ex_engineerName = worksheet.Cells[row, col_engineer_name].Text.ToString().Trim();
                                var ex_remark = worksheet.Cells[row, col_remark].Text.ToString().Trim();
                                var ex_repairDesc = worksheet.Cells[row, col_repair_description].Text.ToString().Trim();
                                var ex_defectDesc = worksheet.Cells[row, col_defect_description].Text.ToString().Trim();
                                var ex_statusComment = worksheet.Cells[row, col_status_comment].Text.ToString().Trim();
                                var ex_customerComment = worksheet.Cells[row, col_customer_comment].Text.ToString().Trim();
                                var ex_serviceProductCode = worksheet.Cells[row, col_code_category].Text.ToString().Trim();
                                var ex_serviceProductDesc = worksheet.Cells[row, col_name_category].Text.ToString().Trim();

                                //datetime data sort by column excel

                                //purchase date
                                DateTime? purchaseDate = (DateTime?)null;
                                if (worksheet.Cells[row, col_purchase_date].Text != "")
                                {
                                    var ex_purchaseDate = worksheet.Cells[row, col_purchase_date];
                                    (text_error, table_error, purchaseDate) = Validate(text_error, table_error, "Purchase Date", ex_purchaseDate);
                                }

                                //request date
                                DateTime? requestDate = (DateTime?)null;
                                if (worksheet.Cells[row, col_request_date].Text != "")
                                {
                                    var ex_requestDate = worksheet.Cells[row, col_request_date];
                                    (text_error, table_error, requestDate) = Validate(text_error, table_error, "Request Date", ex_requestDate);
                                }

                                //customer prefer date
                                DateTime? customerPreferDate = (DateTime?)null;
                                if (worksheet.Cells[row, col_customer_date].Text != "")
                                {
                                    var ex_customerPreferDate = worksheet.Cells[row, col_customer_date];
                                    (text_error, table_error, customerPreferDate) = Validate(text_error, table_error, "Customer Preferred Date", ex_customerPreferDate);
                                }

                                //customer prefer time
                                var ex_customerPreferTime = worksheet.Cells[row, col_customer_time].Text.ToString().Trim();

                                //Appointment Datetime
                                DateTime? appointmentDate = (DateTime?)null;
                                var ex_lastAppointmentDate = worksheet.Cells[row, col_last_appointment_date].Text;
                                if (ex_lastAppointmentDate != "" && ex_lastAppointmentDate.IndexOf("00") == -1)
                                {
                                    var ex_lastAppDate = worksheet.Cells[row, col_last_appointment_date];
                                    (text_error, table_error, appointmentDate) = Validate(text_error, table_error, "ASC Last Appointment Date", ex_lastAppDate);
                                }
                                else
                                {
                                    //not have last app date
                                    var Check_ex_1stAppDate = worksheet.Cells[row, col_1st_appointment_date].Text;
                                    if (Check_ex_1stAppDate != "")
                                    {
                                        var ex_1stAppDate = worksheet.Cells[row, col_1st_appointment_date];
                                        (text_error, table_error, appointmentDate) = Validate(text_error, table_error, "ASC 1st Appointment Date", ex_1stAppDate);

                                        if (appointmentDate != null && worksheet.Cells[row, col_1st_appointment_time].Text != "")
                                        {
                                            try
                                            {
                                                var ex_1stAppTime = worksheet.Cells[row, col_1st_appointment_time].Value.ToString();
                                                ex_1stAppTime = ex_1stAppTime.Substring(ex_1stAppTime.IndexOf(" ") + 1);
                                                appointmentDate = DateTime.ParseExact(appointmentDate.Value.ToString("dd/MM/yyyy") + " " + ex_1stAppTime, "dd/MM/yyyy h:mm:ss tt", null);
                                            }
                                            catch (Exception e)
                                            {
                                                text_error += text_error == "" ? "เช็ค Format ASC 1st Appointment Time" : " , เช็ค Format ASC 1st Appointment Time";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        text_error += Check_ex_1stAppDate == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ ASC 1st Appointment Date" : " , ไม่พบข้อมูล คอลัมน์ ASC 1st Appointment Date" : "";
                                    }
                                }


                                //check data do not null
                                text_error += ex_serviceOrder == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Service Order No." : " ,ไม่พบข้อมูล คอลัมน์ Service Order No." : "";
                                table_error += ex_serviceOrder == "" ? table_error == "" ? "Service Order No." : " ,Service Order No." : "";
                                text_error += ex_StoreCode == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ ASC Code" : " ,ไม่พบข้อมูล คอลัมน์ ASC Code" : "";
                                table_error += ex_StoreCode == "" ? table_error == "" ? "ASC Code" : " ,ASC Code" : "";
                                text_error += ex_storeName == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ ASC Name" : " ,ไม่พบข้อมูล คอลัมน์ ASC Name" : "";
                                table_error += ex_storeName == "" ? table_error == "" ? "ASC Name" : " ,ASC Name" : "";
                                text_error += ex_model == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Model" : " ,ไม่พบข้อมูล คอลัมน์ Model" : "";
                                table_error += ex_model == "" ? table_error == "" ? "Model" : " ,Model" : "";
                                text_error += ex_serialNo == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Serial No" : " ,ไม่พบข้อมูล คอลัมน์ Serial No" : "";
                                table_error += ex_serialNo == "" ? table_error == "" ? "Serial No" : " ,Serial No" : "";
                                //text_error += ex_statusCode == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Status Code" : " ,ไม่พบข้อมูล คอลัมน์ Status Code" : "";
                                //table_error += ex_statusCode == "" ? table_error == "" ? "Status Code" : " ,Status Code" : "";
                                text_error += ex_customerName == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Customer Name" : " ,ไม่พบข้อมูล คอลัมน์ Customer Name" : "";
                                table_error += ex_customerName == "" ? table_error == "" ? "Customer Name" : " ,Customer Name" : "";
                                text_error += ex_engineerCode == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Engineer Code" : " ,ไม่พบข้อมูล คอลัมน์ Engineer Code" : "";
                                table_error += ex_engineerCode == "" ? table_error == "" ? "Engineer Code" : " ,Engineer Code" : "";
                                text_error += ex_engineerName == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Engineer Name" : " ,ไม่พบข้อมูล คอลัมน์ Engineer Name" : "";
                                table_error += ex_engineerName == "" ? table_error == "" ? "Engineer Name" : " ,Engineer Name" : "";
                                text_error += ex_serviceProductCode == "" ? text_error == "" ? "ไม่พบข้อมูล คอลัมน์ Service Product Code" : " ,ไม่พบข้อมูล คอลัมน์ Service Product Code" : "";
                                table_error += ex_serviceProductCode == "" ? table_error == "" ? "Service Product Code" : " ,Service Product Code" : "";

                                error_serial = ex_serviceOrder == "" ? " - " : ex_serviceOrder;

                                //check success
                                if (text_error == "")
                                {
                                    try
                                    {
                                        //get data from db
                                        var db_storeId = db.tb_store.Where(w => w.is_delete == 0 && w.code_store == ex_StoreCode).Select(s => s.id).FirstOrDefault();
                                        var db_engineerId = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer == ex_engineerCode).Select(s => s.id).FirstOrDefault();
                                        var db_engineerName = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer == ex_engineerCode).Select(s => s.engineer_name).FirstOrDefault();
                                        var db_job = db.tb_jobs.Where(w => w.is_delete == 0 && w.service_order_no == ex_serviceOrder).FirstOrDefault();
                                        var db_category = db.tb_jobsl_category.Where(w => w.is_delete == 0 && w.code == ex_serviceProductCode).Select(s => s.parent_id).FirstOrDefault();
                                        var db_subCategory = db.tb_jobsl_category.Where(w => w.is_delete == 0 && w.code == ex_serviceProductCode).Select(s => s.id).FirstOrDefault();
                                        var db_province = db.tb_provinces.Where(w => w.province_name == ex_city).Select(s => s.province_id).FirstOrDefault();
                                        var db_distrince = db.tb_districts.Where(w => w.district_name == ex_district).Select(s => s.district_id).FirstOrDefault();

                                        bool canSave = ((idStore == db_storeId) || User.IsInRole("admin")) ? true : false;

                                        //check role can save
                                        if (canSave)
                                        {
                                            //check store
                                            if (ex_StoreCode != "" && db_storeId == 0)
                                            {
                                                var CheckUserStore = Membership.GetUser(ex_StoreCode);
                                                //check user in membership when null new user store
                                                if (CheckUserStore == null)
                                                {
                                                    var email = ex_StoreCode + "@mail.com";
                                                    MembershipUser newUserStore = Membership.CreateUser(ex_StoreCode, "Pa@sswd2019", email);
                                                    Membership.UpdateUser(newUserStore);
                                                    Roles.AddUserToRole(ex_StoreCode, "shop");
                                                }

                                                Guid getGuidStore = (Guid)Membership.GetUser(ex_StoreCode).ProviderUserKey;
                                                Guid new_Guid_Store = Guid.NewGuid();

                                                //create store
                                                tb_store obj_new_store = new tb_store();
                                                obj_new_store.store_guid = new_Guid_Store;
                                                obj_new_store.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                obj_new_store.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                obj_new_store.user_update = getName;
                                                obj_new_store.is_delete = 0;
                                                obj_new_store.code_store = ex_StoreCode;
                                                obj_new_store.province = 0;
                                                obj_new_store.district = 0;
                                                obj_new_store.sub_district = 0;
                                                obj_new_store.site_name = ex_storeName;

                                                db.tb_store.Add(obj_new_store);
                                                db.SaveChanges();

                                                //create mapping store
                                                tb_mapping_store obj_map = new tb_mapping_store();
                                                obj_map.account_guid = getGuidStore;
                                                obj_map.site_id = obj_new_store.id;
                                                db.tb_mapping_store.Add(obj_map);
                                                db.SaveChanges();

                                                db_storeId = db.tb_store.Where(w => w.is_delete == 0 && w.code_store == ex_StoreCode).Select(s => s.id).FirstOrDefault();
                                            }

                                            //check engineer
                                            if (ex_engineerCode != "" && db_engineerId == 0)
                                            {
                                                var CheckUserEngineer = Membership.GetUser(ex_engineerCode);
                                                //check user in membership when null new user engineer
                                                if (CheckUserEngineer == null)
                                                {
                                                    var email = ex_engineerCode + "@mail.com";
                                                    MembershipUser newUserEngineer = Membership.CreateUser(ex_engineerCode, "Pa@sswd2019", email);
                                                    Membership.UpdateUser(newUserEngineer);
                                                    Roles.AddUserToRole(ex_engineerCode, "engineer");
                                                }

                                                Guid getGuidEngineer = (Guid)Membership.GetUser(ex_engineerCode).ProviderUserKey;
                                                Guid new_Guid_Engineer = Guid.NewGuid();

                                                //create engineer
                                                tb_engineer obj_new_en = new tb_engineer();
                                                obj_new_en.en_guid = new_Guid_Engineer;
                                                obj_new_en.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                obj_new_en.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                obj_new_en.user_update = getName;
                                                obj_new_en.is_delete = 0;
                                                obj_new_en.province = 0;
                                                obj_new_en.engineer_name = ex_engineerName;
                                                obj_new_en.site_id = db_storeId;
                                                obj_new_en.code_engineer = ex_engineerCode;
                                                obj_new_en.account_guid = getGuidEngineer;

                                                db.tb_engineer.Add(obj_new_en);
                                                db.SaveChanges();

                                                var new_dbEngineer = db.tb_engineer.Where(w => w.is_delete == 0 && w.code_engineer == ex_engineerCode).Select(s => new { id = s.id, name = s.engineer_name }).FirstOrDefault();
                                                db_engineerId = new_dbEngineer.id;
                                                db_engineerName = new_dbEngineer.name;
                                            }

                                            //check category
                                            if (ex_serviceProductCode != "" && (db_category == 0 || db_category == null))
                                            {
                                                Guid newGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

                                                //check category other in db
                                                var db_categoryOther = db.tb_jobsl_category.Where(w => w.guid_category == newGuid).Select(s => s.id).FirstOrDefault();
                                                int idOther = db_categoryOther;
                                                if (db_categoryOther == 0)
                                                {
                                                    tb_jobsl_category new_other = new tb_jobsl_category();
                                                    new_other.create_date = DateTime.Now;
                                                    new_other.update_date = DateTime.Now;
                                                    new_other.is_delete = 0;
                                                    new_other.name = "Other";
                                                    new_other.code = "other";
                                                    new_other.description = "Other";
                                                    new_other.guid_category = newGuid;
                                                    new_other.user_update = getName;
                                                    new_other.parent_id = 0;

                                                    db.tb_jobsl_category.Add(new_other);
                                                    db.SaveChanges();

                                                    idOther = new_other.id;
                                                }

                                                Guid new_Guid_Category = Guid.NewGuid();
                                                tb_jobsl_category obj_new_category = new tb_jobsl_category();
                                                obj_new_category.create_date = DateTime.Now;
                                                obj_new_category.update_date = DateTime.Now;
                                                obj_new_category.is_delete = 0;
                                                obj_new_category.name = ex_serviceProductDesc;
                                                obj_new_category.code = ex_serviceProductCode;
                                                obj_new_category.description = ex_serviceProductDesc;
                                                obj_new_category.guid_category = new_Guid_Category;
                                                obj_new_category.user_update = getName;
                                                obj_new_category.parent_id = idOther;

                                                db.tb_jobsl_category.Add(obj_new_category);
                                                db.SaveChanges();

                                                db_category = idOther;
                                                db_subCategory = obj_new_category.id;
                                            }

                                            //check status job
                                            int ex_status_job = 0;
                                            switch (ex_statusCode)
                                            {
                                                case "ST010": ex_status_job = 0; break;
                                                case "ST015": ex_status_job = 0; break;
                                                case "ST025": ex_status_job = 6; break;
                                                case "ST030": ex_status_job = 2; break;
                                                case "ST035": ex_status_job = 3; break;
                                                default: ex_status_job = 6; break;
                                            }

                                            if (ex_status_job == 2 && (ex_reasonCode == "HP015" || ex_reasonCode == "HP020" || ex_reasonCode == "HP065"))
                                            {
                                                ex_status_job = 6;
                                            }


                                            //check phone number
                                            ex_phoneMobile = (ex_phoneMobile == "" || ex_phoneMobile.Length == 10) ? ex_phoneMobile : ex_phoneMobile[0] == 0 ? ex_phoneMobile : "0" + ex_phoneMobile;
                                            ex_phoneOffice = ex_phoneOffice == "" ? ex_phoneOffice : ex_phoneOffice[0] == 0 ? ex_phoneOffice : "0" + ex_phoneOffice;
                                            ex_phoneHome = ex_phoneHome == "" ? ex_phoneHome : ex_phoneHome[0] == 0 ? ex_phoneHome : "0" + ex_phoneHome;

                                            //key map
                                            var keymap = ConfigurationManager.AppSettings["googleMap_key_forserver"];

                                            //if not data in db
                                            if (db_job == null)
                                            {
                                                //create job
                                                CommonLib.tb_jobs job = new CommonLib.tb_jobs();

                                                job.job_guid = new_Guid_job;
                                                job.create_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                job.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                job.user_update = getName;
                                                job.is_delete = 0;
                                                job.service_order_no = ex_serviceOrder;
                                                job.store_code = ex_StoreCode;
                                                job.store_id = db_storeId;
                                                job.purchase_date = purchaseDate; //datetime
                                                job.model = ex_model;
                                                job.serial_no = ex_serialNo;
                                                job.status_code = ex_statusCode;
                                                job.status = ex_status;
                                                job.reason_code = ex_reasonCode;
                                                job.reason = ex_reason;
                                                job.customer_prefer_date = customerPreferDate; //datetime
                                                job.customer_perfer_time = ex_customerPreferTime; //datetime
                                                job.customer_fullname = ex_customerName;
                                                job.street = ex_street;
                                                job.im_province = ex_city;
                                                job.province = db_province;
                                                job.im_district = ex_district;
                                                job.district = db_distrince;
                                                job.zipcode = ex_zipcode;
                                                job.request_date = requestDate; //datetime
                                                job.phone_home = ex_phoneHome;
                                                job.phone_office = ex_phoneOffice;
                                                job.phone_mobile = ex_phoneMobile;
                                                job.service_type = ex_serviceType;
                                                job.warranty_flag = ex_warrantyFlag;
                                                job.engineer_code = ex_engineerCode;
                                                job.engineer_id = db_engineerId;
                                                job.remark = ex_remark;
                                                job.repair_description = ex_repairDesc;
                                                job.defect_description = ex_defectDesc;
                                                job.status_comment = ex_statusComment;
                                                job.customer_comment = ex_customerComment;
                                                job.is_change = "excel";
                                                job.job_category_id = db_category;
                                                job.sub_category_id = db_subCategory;
                                                job.change_user = getName;
                                                job.appointment_datetime = appointmentDate; //datetime
                                                job.status_job = ex_status_job;

                                                db.tb_jobs.Add(job);
                                                db.SaveChanges();
                                                isNew = true;
                                                return_id = job.id.ToString();

                                                //call api google map and save map
                                                var ex_address = ex_street + " " + ex_district + " " + ex_city + " " + ex_zipcode;
                                                var client = new RestClient("https://maps.googleapis.com/maps/api/geocode/json?address=" + ex_address + "&key=" + keymap);
                                                var request = new RestRequest(Method.GET);
                                                request.AddHeader("cache-control", "no-cache");
                                                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                                IRestResponse response = client.Execute(request);
                                                api_map data_map = JsonConvert.DeserializeObject<api_map>(response.Content);

                                                var lat_api = data_map.results.Select(s => s.geometry.location.lat).FirstOrDefault();
                                                var long_api = data_map.results.Select(s => s.geometry.location.lng).FirstOrDefault();
                                                job.customer_lat = lat_api.ToString();
                                                job.customer_long = long_api.ToString();
                                                db.tb_jobs.AddOrUpdate(job);
                                                db.SaveChanges();
                                            }
                                            else
                                            {
                                                //compare data between excel and db

                                                //purchase date
                                                purchaseDate = purchaseDate == db_job.purchase_date ? db_job.purchase_date : purchaseDate;
                                                //request date
                                                requestDate = requestDate == db_job.request_date ? db_job.request_date : requestDate;
                                                //customer prefer date
                                                customerPreferDate = customerPreferDate == db_job.customer_prefer_date ? db_job.customer_prefer_date : customerPreferDate;
                                                //customer prefer time
                                                ex_customerPreferTime = ex_customerPreferTime == db_job.customer_perfer_time ? db_job.customer_perfer_time : ex_customerPreferTime;

                                                //asc 1st app date
                                                appointmentDate = appointmentDate == db_job.appointment_datetime ? db_job.appointment_datetime : (db_job.status_job == 2 || db_job.status_job == 0 || db_job.status_job == 6) ? appointmentDate : db_job.appointment_datetime;

                                                //asc last app date
                                                var appointmentDateto = appointmentDate == db_job.appointment_datetime ? db_job.appointment_to_datetime : (db_job.status_job == 2 || db_job.status_job == 0 || db_job.status_job == 6) ? (DateTime?)null : db_job.appointment_to_datetime;

                                                ex_serviceOrder = ex_serviceOrder == db_job.service_order_no ? db_job.service_order_no : ex_serviceOrder;
                                                ex_model = ex_model == db_job.model ? db_job.model : ex_model;
                                                ex_serialNo = ex_serialNo == db_job.serial_no ? db_job.serial_no : ex_serialNo;
                                                ex_customerName = ex_customerName == db_job.customer_fullname ? db_job.customer_fullname : ex_customerName;
                                                ex_street = ex_street == db_job.street ? db_job.street : ex_street;
                                                ex_city = ex_city == db_job.im_province ? db_job.im_province : ex_city;
                                                int? compare_province = db_province == db_job.province ? db_job.province : db_province;
                                                ex_district = ex_district == db_job.im_district ? db_job.im_district : ex_district;
                                                int? compare_distrince = db_distrince == db_job.district ? db_job.district : db_distrince;
                                                int? compare_zipcode = ex_zipcode == db_job.zipcode ? db_job.zipcode : ex_zipcode;
                                                ex_statusComment = ex_statusComment == db_job.status_comment ? db_job.status_comment : ex_statusComment;
                                                ex_customerComment = ex_customerComment == db_job.customer_comment ? db_job.customer_comment : ex_customerComment;
                                                ex_phoneHome = ex_phoneHome == db_job.phone_home ? db_job.phone_home : ex_phoneHome;
                                                ex_phoneOffice = ex_phoneOffice == db_job.phone_office ? db_job.phone_office : ex_phoneOffice;
                                                ex_phoneMobile = ex_phoneMobile == db_job.phone_mobile ? db_job.phone_mobile : ex_phoneMobile;
                                                ex_warrantyFlag = ex_warrantyFlag == db_job.warranty_flag ? db_job.warranty_flag : ex_warrantyFlag;
                                                ex_reasonCode = ex_reasonCode == db_job.reason_code ? db_job.reason_code : ex_reasonCode;
                                                ex_statusCode = ex_statusCode == db_job.status_code ? db_job.status_code : ex_statusCode;
                                                ex_status = ex_status == db_job.status ? db_job.status : ex_status;
                                                ex_reason = ex_reason == db_job.reason ? db_job.reason : ex_reason;
                                                ex_remark = ex_remark == db_job.remark ? db_job.remark : ex_remark;
                                                ex_serviceType = ex_serviceType == db_job.service_type ? db_job.service_type : ex_serviceType;
                                                ex_repairDesc = ex_repairDesc == db_job.repair_description ? db_job.repair_description : ex_repairDesc;
                                                ex_defectDesc = ex_defectDesc == db_job.defect_description ? db_job.defect_description : ex_defectDesc;
                                                int? status_job = db_job.status_job == 3 || db_job.status_job == 7 || db_job.status_job == 8 || db_job.status_job == 9 || db_job.status_job == 10 || db_job.status_job == 11 ? db_job.status_job : ex_status_job;

                                                //update job

                                                db_job.update_date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
                                                db_job.user_update = getName;
                                                db_job.service_order_no = ex_serviceOrder;
                                                db_job.store_code = ex_StoreCode;
                                                db_job.store_id = db_storeId;
                                                db_job.purchase_date = purchaseDate; //datetime
                                                db_job.model = ex_model;
                                                db_job.serial_no = ex_serialNo;
                                                db_job.status_code = ex_statusCode;
                                                db_job.status = ex_status;
                                                db_job.reason_code = ex_reasonCode;
                                                db_job.reason = ex_reason;
                                                db_job.customer_prefer_date = customerPreferDate; //datetime
                                                db_job.customer_perfer_time = ex_customerPreferTime; //datetime
                                                db_job.customer_fullname = ex_customerName;
                                                db_job.street = ex_street;
                                                db_job.im_province = ex_city;
                                                db_job.province = compare_province;
                                                db_job.im_district = ex_district;
                                                db_job.district = compare_distrince;
                                                db_job.zipcode = compare_zipcode;
                                                db_job.request_date = requestDate; //datetime
                                                db_job.phone_home = ex_phoneHome;
                                                db_job.phone_office = ex_phoneOffice;
                                                db_job.phone_mobile = ex_phoneMobile;
                                                db_job.service_type = ex_serviceType;
                                                db_job.warranty_flag = ex_warrantyFlag;
                                                db_job.engineer_code = ex_engineerCode;
                                                db_job.engineer_id = db_engineerId;
                                                db_job.remark = ex_remark;
                                                db_job.repair_description = ex_repairDesc;
                                                db_job.defect_description = ex_defectDesc;
                                                db_job.status_comment = ex_statusComment;
                                                db_job.customer_comment = ex_customerComment;
                                                db_job.is_change = "excel";
                                                db_job.job_category_id = db_category;
                                                db_job.sub_category_id = db_subCategory;
                                                db_job.change_user = getName;
                                                db_job.appointment_datetime = appointmentDate; //datetime
                                                db_job.appointment_to_datetime = appointmentDateto; //datetime
                                                db_job.status_job = status_job;

                                                db.tb_jobs.AddOrUpdate(db_job);
                                                db.SaveChanges();
                                                isNew = false;
                                                return_id = db_job.id.ToString();

                                                //jobs update lat lng customer (if customer lat lng is null) lat , lng call api
                                                if ((db_job.customer_lat == null || db_job.customer_lat == "0") && (db_job.customer_long == null || db_job.customer_long == "0"))
                                                {
                                                    //---------------MAP------------------
                                                    var tem_address = ex_street + " " + ex_district + " " + ex_city + " " + ex_zipcode;
                                                    var client = new RestClient("https://maps.googleapis.com/maps/api/geocode/json?address=" + tem_address + "&key=" + keymap);
                                                    var request = new RestRequest(Method.GET);
                                                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                                    IRestResponse response = client.Execute(request);
                                                    api_map data_map = JsonConvert.DeserializeObject<api_map>(response.Content);
                                                    var data_lat = data_map.results.Select(s => s.geometry.location.lat).FirstOrDefault();
                                                    var data_long = data_map.results.Select(s => s.geometry.location.lng).FirstOrDefault();

                                                    if (data_lat != 0) db_job.customer_lat = data_lat.ToString();
                                                    if (data_long != 0) db_job.customer_long = data_long.ToString();

                                                    db.tb_jobs.AddOrUpdate(db_job);
                                                    db.SaveChanges();
                                                }
                                            }

                                            //add parts
                                            if (col_getParts != 0)
                                            {
                                                var ex_parts = worksheet.Cells[row, col_getParts].Text;
                                                if (ex_parts != "")
                                                {
                                                    tb_jobs_parts obj_new_parts = new tb_jobs_parts();
                                                    int col_parts = col_getParts;

                                                    //get data from database
                                                    var db_parts = db.tb_jobs_parts.Where(w => w.job_id.ToString() == return_id).ToList();

                                                    //delete parts from db
                                                    for (int i = 0; i < db_parts.Count; i++)
                                                    {
                                                        db.tb_jobs_parts.Remove(db_parts[i]);
                                                        db.SaveChanges();
                                                    }

                                                    //add parts in db
                                                    for (int i = 0; i <= (col_getParts - 1); i += 9)
                                                    {
                                                        if (worksheet.Cells[row, col_parts].Text != "" && (i + 2 != (col_getParts - 1)))
                                                        {
                                                            //var ship_date = worksheet.Cells[row, col_parts + 5].Text != "" ? worksheet.Cells[row, col_parts + 5].Value.ToString() == "00/00/0000" || worksheet.Cells[row, col_parts + 5].Value.ToString() == null ? "00/00/0000" : worksheet.Cells[row, col_parts + 5].Value.ToString() : "00/00/0000";
                                                            var db_location = worksheet.Cells[row, col_parts].Text;
                                                            var db_patrs_no = worksheet.Cells[row, col_parts + 1].Text;
                                                            //ship_date = ship_date.Length > 10 ? ship_date.Substring(0, ship_date.IndexOf(" ")) : ship_date;

                                                            var ex_ship_date = worksheet.Cells[row, col_parts + 5].Text != "" ? worksheet.Cells[row, col_parts + 5].Value.ToString() : "";
                                                            ex_ship_date = ex_ship_date.Length > 10 ? ex_ship_date.Substring(0, ex_ship_date.IndexOf(" ")) : ex_ship_date;
                                                            ex_ship_date = ex_ship_date.IndexOf("00") == -1 ? ex_ship_date : "";
                                                            DateTime? shipDate = (DateTime?)null;
                                                            if (ex_ship_date != "")
                                                            {
                                                                try
                                                                {
                                                                    shipDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ex_ship_date, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                                                                }
                                                                catch (Exception e)
                                                                {

                                                                }
                                                            }

                                                            obj_new_parts.job_id = Convert.ToInt32(return_id);
                                                            obj_new_parts.parts_repair_location = db_location;
                                                            obj_new_parts.parts_no = worksheet.Cells[row, col_parts + 1].Text;
                                                            obj_new_parts.material_serial_no = worksheet.Cells[row, col_parts + 2].Text;
                                                            obj_new_parts.quantity = worksheet.Cells[row, col_parts + 3].Text != "" ? int.Parse(worksheet.Cells[row, col_parts + 3].Text) : 0;
                                                            obj_new_parts.parts_status = worksheet.Cells[row, col_parts + 4].Text;
                                                            //obj_new_parts.ship_date = ship_date != "00/00/0000" && ship_date.IndexOf("00") == -1 ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(ship_date, new string[] { "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone) : (DateTime?)null;
                                                            obj_new_parts.ship_date = shipDate;
                                                            obj_new_parts.parts_description = worksheet.Cells[row, col_parts + 6].Text;
                                                            obj_new_parts.parts_in_out = worksheet.Cells[row, col_parts + 7].Text;
                                                            obj_new_parts.so_no = worksheet.Cells[row, col_parts + 8].Text;

                                                            db.tb_jobs_parts.Add(obj_new_parts);
                                                            db.SaveChanges();

                                                            col_parts += 9;
                                                        }
                                                    }
                                                }
                                            }

                                            //save success
                                            save_job.Add(new job
                                            {
                                                Id = return_id,
                                                Engineer = ex_engineerName,
                                                Store = ex_storeName,
                                                Serial = ex_serviceOrder,
                                                Update = isNew == false ? "Update" : "Is New"
                                            });
                                        }
                                        else //can't save because you is not role admin
                                        {
                                            save_job.Add(new job
                                            {
                                                Id = "-",
                                                Engineer = "-",
                                                Store = "-",
                                                Serial = error_serial,
                                                Update = "คุณไม่ใช่ Admin ใหญ่ไม่สามารถบันทึกงานต่าง ศูนย์บริการได้"
                                            });
                                        }
                                    }
                                    catch (Exception e) //error logic return serial id    
                                    {
                                        save_job.Add(new job
                                        {
                                            Id = "-",
                                            Engineer = "-",
                                            Store = "-",
                                            Serial = error_serial,
                                            Update = "Error เช็คข้อมูล หมายเลขงาน " + error_serial + " ใน file excel"
                                        });
                                    }
                                }
                                else
                                {
                                    save_job.Add(new job
                                    {
                                        Id = "-",
                                        Engineer = "-",
                                        Store = "-",
                                        Serial = error_serial,
                                        Update = text_error,
                                        table_error = table_error
                                    });
                                }
                            }
                        }
                    }
                    System.IO.File.Delete(path);
                }
                else
                {
                    //return type file excel invalid
                    save_job.Add(new job
                    {
                        Id = "111111111111111111111111111",
                    });
                }

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(save_job);
                return new ContentResult()
                {
                    Content = jsonString,
                    ContentType = "application/json"
                };
            }
        }

        public (string, string, DateTime?) Validate(string text_error, string table_error, string column, ExcelRange ex_data)
        {
            var data = ex_data.Value.ToString();
            var checkType = ex_data.Value.GetType();

            var type = checkType.FullName.ToString().ToLower();
            DateTime? convertDate = (DateTime?)null;
            
            //check type
            if (type == "system.datetime")
            {
                try
                {
                    convertDate = ((System.DateTime)ex_data.Value).Date;
                }
                catch (Exception e)
                {
                    text_error += text_error == "" ? "เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี" : ",เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี";
                    table_error += table_error == "" ? column : "," + column;
                }
            }
            else
            {
                if(data.IndexOf("00") == -1)
                {
                    //try
                    //{
                    //    convertDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(data, new string[] { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "d/MM/yyyy", "M/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None), zone);
                    //}
                    //catch (Exception e)
                    //{
                    //    text_error = text_error == "" ? "เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี" : ", เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี";
                    //}

                    text_error += text_error == "" ? "เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี" : ",เช็คข้อมูล " + column + " รูปแบบต้องเป็น  เดือน / วัน / ปี";
                    table_error += table_error == "" ? column : "," + column;
                }
            }

            return (text_error , table_error, convertDate);
        }

    }
}