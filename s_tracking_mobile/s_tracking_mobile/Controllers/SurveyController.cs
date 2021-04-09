using CommonLib;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using s_tracking_mobile.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    //[Authorize(Roles = "admin")]
    public class SurveyController : Controller
    {
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        int show = 20;
        [Authorize(Roles = "admin")]
        // only question and cate question
        public ActionResult Index(int cate = 0)
        {
            var quest_cate = db.in_survey_question.Where(w => w.survey_type == 0 && w.is_delete == 0 && w.jobs_category_id > 0);

            if (cate <= 0)
            {
                var one_cate = db.tb_jobsl_category.Where(w => w.is_delete == 0).FirstOrDefault();
                if (one_cate != null)
                {
                    cate = one_cate.id;
                }
            }

            if (quest_cate != null)
            {
                quest_cate = quest_cate.Where(w => w.jobs_category_id == cate);
                var count = quest_cate.ToList().Count();
                var need = 10 - count;
                if (quest_cate.Count() < 10)
                {
                    for (int i = 1; i <= need; i++)
                    {
                        var tb_quest = new in_survey_question();
                        tb_quest.jobs_category_id = cate;
                        tb_quest.create_date = now;
                        tb_quest.update_date = now;
                        tb_quest.user_update = User.Identity.Name;
                        tb_quest.setorder = count + i;
                        //tb_quest.is_enable = 1;
                        db.in_survey_question.Add(tb_quest);
                        db.SaveChanges();
                    }
                }
            }

            ViewData["Cate"] = db.tb_jobsl_category.Where(w => w.is_delete == 0).ToList();
            ViewData["Survey_cate"] = quest_cate.ToList();
            ViewData["Survey"] = db.in_survey_question.Where(w => w.survey_type == 0 && w.is_delete == 0 && (w.jobs_category_id == null || w.jobs_category_id == 0)).ToList();
            return View("index");
        }

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Add(int id = 0)
        {
            if (id > 0)
            {
                var quest = db.in_survey_question.Where(w => w.survey_type == 0 && w.is_delete == 0 && w.id == id).FirstOrDefault();
                var check_answer = db.in_survey_answer.Where(w => w.is_delete == 0 && w.question_id == quest.id).ToList();
                var count = check_answer.Count();
                var need = 10 - count;
                if (count < 10)
                {
                    for (int i = 1; i <= need; i++)
                    {
                        var tb_survery = new in_survey_answer();
                        tb_survery.question_id = id;
                        tb_survery.setorder = count + i;
                        tb_survery.create_date = now;
                        tb_survery.update_date = now;
                        tb_survery.user_update = User.Identity.Name;
                        //tb_survery.is_enable = 1;
                        db.in_survey_answer.Add(tb_survery);
                        await db.SaveChangesAsync();
                    }
                    var answer = db.in_survey_answer.Where(w => w.is_delete == 0 && w.question_id == quest.id).ToList();
                    ViewData["answer"] = answer;
                }
                else
                {
                    ViewData["answer"] = check_answer;
                }

                ViewData["quest"] = quest;

                return View();
            }
            else
            {
                return Index();
            }

        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public object SaveQnA(int q_id, bool is_enable, string quest, bool is_check, int min, List<bool> ls_cb, List<string> ls_answer, List<decimal> ls_score, List<int> ls_ans_id)
        {
            if ((ls_cb.Count == ls_answer.Count) && (ls_cb.Count == ls_score.Count) && (ls_cb.Count == ls_ans_id.Count) && q_id > 0)
            {
                var qu = db.in_survey_question.Where(w => w.survey_type == 0 && w.is_delete == 0 && w.id == q_id).FirstOrDefault();

                if (qu != null)
                {

                    qu.is_enable = is_enable ? 1 : 0;
                    qu.question = quest;
                    qu.is_check = is_check ? 1 : 0;
                    qu.check_source_min = min;
                    qu.update_date = now;
                    qu.user_update = User.Identity.Name;

                    db.in_survey_question.AddOrUpdate(qu);
                    db.SaveChanges();

                    int cb_count = ls_cb.Count;
                    for (int i = 0; i < cb_count; i++)
                    {
                        int an_id = ls_ans_id[i];
                        var an = db.in_survey_answer.Where(w => w.is_delete == 0 && w.id == an_id).FirstOrDefault();
                        if (an != null)
                        {
                            an.update_date = now;
                            an.user_update = User.Identity.Name;
                            an.is_enable = ls_cb[i] ? 1 : 0;
                            an.answer = ls_answer[i];
                            an.answer_source = ls_score[i];
                            db.in_survey_answer.AddOrUpdate(an);
                            db.SaveChanges();
                        }
                    }


                    return new ContentResult()
                    {
                        Content = Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true }),
                        ContentType = "application/json"
                    };

                }
            }

            return new ContentResult()
            {
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false }),
                ContentType = "application/json"
            };

        }

        [Authorize(Roles = "admin")]
        public ActionResult Negative()
        {
            in_setting_survey setting = db.in_setting_survey.Where(w => w.is_delete == 0 && w.survey_type == 0).FirstOrDefault();
            ViewData["data"] = setting;
            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public object SaveNegative(int setting_id, bool is_enable, bool is_noti, string email1, string email2, string email3, string email4)
        {
            var tb = db.in_setting_survey.Where(w => w.is_delete == 0 && w.survey_type == 0 && w.id == setting_id).FirstOrDefault();
            if (tb != null)
            {
                tb.is_enable = is_enable ? 1 : 0;
                tb.is_notifi = is_noti ? 1 : 0;
                tb.notifi_email1 = email1;
                tb.notifi_email2 = email2;
                tb.notifi_email3 = email3;
                tb.notifi_email4 = email4;
                tb.update_date = now;
                tb.user_update = User.Identity.Name;
                db.in_setting_survey.AddOrUpdate(tb);
                db.SaveChanges();


                return new ContentResult()
                {
                    Content = Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true }),
                    ContentType = "application/json"
                };
            }
            else
            {
                return new ContentResult()
                {
                    Content = Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false }),
                    ContentType = "application/json"
                };
            }


        }

        // done survey
        [Authorize]
        public ActionResult All()
        {
            ViewBag.roleUser = User.IsInRole("admin");
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            int idStore = 0;
            IQueryable<tb_store> map_s = db.tb_store.Where(w => w.is_delete == 0 && w.id == 0);
            if (User.IsInRole("admin"))
            {
                map_s = db.tb_store.Where(w => w.is_delete == 0);
                ViewData["site"] = map_s.ToList();
            }
            else
            {
                var getName = User.Identity.Name;
                if (getName != "")
                {
                    var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                    if (CheckUser != null)
                    {
                        Guid convertGuid = new Guid(CheckUser.ToString());
                        idStore = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
                        map_s = db.tb_store.Where(w => w.is_delete == 0 && w.id == idStore);
                        ViewData["site"] = map_s.ToList();
                    }
                    else
                    {
                        ViewData["site"] = map_s;
                    }
                }
                else
                {
                    ViewData["site"] = map_s;
                }
            }

            DateTime start = now.Date.AddDays(-89);
            DateTime end = now.Date;
            end = end.AddHours(23).AddMinutes(59).AddSeconds(59);
            ViewData["input_start"] = start.ToString("dd/MM/yyyy");
            ViewData["input_end"] = end.ToString("dd/MM/yyyy");

            var data = (from s in db.tb_survey
                        join j in db.tb_jobs on s.job_guid equals j.job_guid
                        join e in db.tb_engineer on j.engineer_id equals e.id
                        join store in map_s on j.store_id equals store.id
                        where j.is_delete == 0 && e.is_delete == 0 && s.is_delete == 0 && s.create_date >= start && s.create_date <= end
                        select new survey_all
                        {
                            job_guid = s.job_guid,
                            service_no = j.service_order_no,
                            engineer_name = e.engineer_name,
                            site = store.site_name,
                            score = s.source,
                            negative = s.is_negative == 1 ? true : false,
                            is_feedback = s.is_feedback == 1 ? true : false,
                            cre_date = s.create_date,
                        }).OrderByDescending(s => s.cre_date).Skip(0).Take(show).ToList();

            var count = (from s in db.tb_survey
                         join j in db.tb_jobs on s.job_guid equals j.job_guid
                         join e in db.tb_engineer on j.engineer_id equals e.id
                         join store in map_s on j.store_id equals store.id
                         where j.is_delete == 0 && e.is_delete == 0 && s.is_delete == 0
                         select s.id
                        ).Count();

            ViewData["Data"] = data;
            ViewData["Count"] = count;

            return View("All");
        }

        [Authorize]
        [HttpGet]
        public object page_all(string service_no, int status = 0, int negative = 999, int page = 1, string str_start = "", string str_end = "", int site = 0)
        {
            if (str_start == "") str_start = now.Date.AddDays(-89).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);

            var getName = User.Identity.Name;
            if (getName != "" && s_check && e_check)
            {

                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                if (CheckUser != null && !User.IsInRole("admin"))
                {
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    site = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
                }

                end = end.AddHours(23).AddMinutes(59).AddSeconds(59);
                if (page < 1) { page = 1; }
                var skip_page = (page - 1) * show;

                var survey = db.tb_survey.Where(w => w.is_delete == 0 && w.create_date >= start && w.create_date <= end);
                if (status > 0)
                {
                    survey = survey.Where(w => w.survey_status == status);
                }

                if (negative != 999)
                {
                    if (negative == 0)
                    {
                        survey = survey.Where(w => w.is_negative == negative || w.is_negative == null);
                    }
                    else
                    {
                        survey = survey.Where(w => w.is_negative == negative);
                    }
                }

                var job = db.tb_jobs.Where(w => w.is_delete == 0);
                if (site > 0)
                {
                    job = job.Where(w => w.store_id == site);
                }

                var data = new List<survey_all>();

                if (service_no.Trim() != "")
                {
                    data = (from s in survey

                            join j in job on s.job_guid equals j.job_guid
                            join e in db.tb_engineer on j.engineer_id equals e.id
                            join store in db.tb_store on j.store_id equals store.id
                            where j.service_order_no.Contains(service_no) && e.is_delete == 0 && store.is_delete == 0

                            select new survey_all
                            {
                                job_guid = s.job_guid,
                                service_no = j.service_order_no,
                                engineer_name = e.engineer_name,
                                site = store.site_name,
                                score = s.source,
                                negative = s.is_negative == 1 ? true : false,
                                is_feedback = s.is_feedback == 1 ? true : false,
                                cre_date = s.create_date,

                                count = (from s in survey
                                         join j in job on s.job_guid equals j.job_guid
                                         join e in db.tb_engineer on j.engineer_id equals e.id
                                         join store in db.tb_store on j.store_id equals store.id
                                         where j.is_delete == 0 && j.service_order_no == service_no && e.is_delete == 0 && store.is_delete == 0
                                         select s.id).Count()
                            }).OrderByDescending(s => s.cre_date).Skip(skip_page).Take(show).ToList();
                }
                else
                {
                    data = (from s in survey
                            join j in job on s.job_guid equals j.job_guid
                            join e in db.tb_engineer on j.engineer_id equals e.id
                            join store in db.tb_store on j.store_id equals store.id
                            where e.is_delete == 0 && store.is_delete == 0

                            select new survey_all
                            {
                                job_guid = s.job_guid,
                                service_no = j.service_order_no,
                                engineer_name = e.engineer_name,
                                site = store.site_name,
                                score = s.source,
                                negative = s.is_negative == 1 ? true : false,
                                is_feedback = s.is_feedback == 1 ? true : false,
                                cre_date = s.create_date,
                                count = (from s in survey
                                         join j in job on s.job_guid equals j.job_guid
                                         join e in db.tb_engineer on j.engineer_id equals e.id
                                         join store in db.tb_store on j.store_id equals store.id
                                         where j.is_delete == 0 && e.is_delete == 0 && store.is_delete == 0
                                         select s.id).Count()
                            }).OrderByDescending(s => s.cre_date).Skip(skip_page).Take(show).ToList();
                }

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                return jsonString;

            }
            else
            {
                return null;
            }


        }


        public void export(string service_no, string status, string negative, string str_start = "", string str_end = "", int site = 0)
        {

            if (str_start == "") str_start = now.Date.AddDays(-89).ToString();
            if (str_end == "") str_end = now.Date.ToString();
            bool s_check = DateTime.TryParse(str_start, out DateTime start);
            bool e_check = DateTime.TryParse(str_end, out DateTime end);


            var getName = User.Identity.Name;
            if (getName != "" && s_check && e_check)
            {

                var CheckUser = Membership.GetUser(getName).ProviderUserKey;
                if (CheckUser != null && !User.IsInRole("admin"))
                {
                    Guid convertGuid = new Guid(CheckUser.ToString());
                    site = db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
                }



                bool check_sta = int.TryParse(status, out int int_status);
                bool check_neg = int.TryParse(negative, out int int_negative);
                var tb_survey = db.tb_survey.Where(w => w.is_delete == 0 && w.create_date >= start && w.create_date <= end);

                if (check_sta == true)
                {
                    tb_survey = tb_survey.Where(w => w.survey_status == int_status);
                }

                if (check_neg)
                {

                    if (int_negative == 0)
                    {
                        tb_survey = tb_survey.Where(w => w.is_negative == 0 && w.is_negative == null);
                    }
                    else
                    {
                        tb_survey = tb_survey.Where(w => w.is_negative == int_negative);
                    }

                }

                var job = db.tb_jobs.Where(w => w.is_delete == 0);
                if (site > 0)
                {
                    job = job.Where(w => w.store_id == site);
                }
                var sur_item = db.tb_survey_item.Where(w => w.is_delete == 0);

                var job_survey = (from j in job
                                  join s in tb_survey on j.job_guid equals s.job_guid
                                  join e in db.tb_engineer on j.engineer_id equals e.id
                                  join si in db.tb_store on j.store_id equals si.id
                                  where s.is_delete == 0 && e.is_delete == 0 && j.service_order_no.Contains(service_no) && si.is_delete == 0
                                  select new Survey_Export
                                  {
                                      id = j.id,
                                      job_guid = j.job_guid,
                                      service_no = j.service_order_no,
                                      engineer = e.engineer_name,
                                      engineer_code = j.engineer_code,
                                      customer = j.customer_fullname,
                                      customer_phone = j.phone_mobile,
                                      survey_date = s.create_date,
                                      sitecode = si.code_store,
                                      sitename = si.site_name,
                                      comment = s.comment_1,
                                      is_feedback = s.is_feedback,
                                      feedback_date = s.feedback_create_date,
                                      user_feedback = s.feeback_user_update,
                                      feedback_note = s.feedback_note,
                                      is_negative = s.is_negative,
                                      qna = (from item in sur_item
                                             where item.survey_id == s.id
                                             select new QnA_Export
                                             {
                                                 id = item.id,
                                                 quest = item.question,
                                                 answer = item.answer,
                                                 score = item.source,
                                                 negative = item.is_negative,
                                                 job_cate = item.jobs_category_id
                                             }).OrderBy(s => s.id).ToList()
                                  }).OrderByDescending(s => s.survey_date).ToList();

                string[] ar = { "หมายเลขงาน", "ชื่อศูนย์บริการ", "รหัสศูนย์บริการ", "ช่างที่ได้รับมอบหมาย","รหัสช่าง" ,"ข้อมูลลูกค้า", "เบอร์ติดต่อ", "วันที่ประเมิน",
                    "Q1","A1","Score", "Negative", "Q2", "A2", "Score", "Negative", "Q3", "A3", "Score", "Negative", "Q4", "A4", "Score", "Negative", "Q5", "A5", "Score", "Negative",
                    "Q6", "A6", "Score", "Negative", "Q7", "A7", "Score", "Negative", "Q8", "A8", "Score", "Negative", "Q9", "A9", "Score", "Negative", "Q10", "A10", "Score", "Negative",
                    "CQ1", "CA1", "Score", "Negative", "CQ2", "CA2", "Score", "Negative", "CQ3", "CA3", "Score", "Negative", "CQ4", "CA4", "Score", "Negative", "CQ5", "CA5", "Score", "Negative",
                 "CQ6", "CA6", "Score", "Negative", "CQ7", "CA7", "Score", "Negative", "CQ8", "CA8", "Score", "Negative", "CQ9", "CA9", "Score", "Negative", "CQ10", "CA10", "Score", "Negative",
                 "Comment ลูกค้า","อ่านแจ้งเตือน","ผลตอบรับ","วันที่ตอบ","ผู้ตอบ","แบบสอบถาม Negative"
                };

                ExcelPackage excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.Row(1).Style.Font.Bold = true;

                int idx = 1;
                foreach (string s in ar)
                {
                    workSheet.Cells[1, idx].Value = s;
                    idx++;
                }
                #region QnA
                //workSheet.Cells[1, 1].Value = "หมายเลขงาน";
                //workSheet.Cells[1, 2].Value = "ช่างที่ได้รับมอบหมาย";
                //workSheet.Cells[1, 3].Value = "รหัสช่าง";
                //workSheet.Cells[1, 4].Value = "ข้อมูลลูกค้า";
                //workSheet.Cells[1, 5].Value = "เบอร์ติดต่อ";
                //workSheet.Cells[1, 6].Value = "วันที่ประเมิน";
                //workSheet.Cells[1, 7].Value = "Q1";
                //workSheet.Cells[1, 8].Value = "A1";
                //workSheet.Cells[1, 9].Value = "Score";
                //workSheet.Cells[1, 10].Value = "Negative";



                //workSheet.Cells[1, 11].Value = "Q2";
                //workSheet.Cells[1, 12].Value = "A2";
                //workSheet.Cells[1, 13].Value = "Score";
                //workSheet.Cells[1, 14].Value = "Negative";

                //workSheet.Cells[1, 15].Value = "Q3";
                //workSheet.Cells[1, 16].Value = "A3";
                //workSheet.Cells[1, 17].Value = "Score";
                //workSheet.Cells[1, 18].Value = "Negative";
                //workSheet.Cells[1, 19].Value = "Q4";
                //workSheet.Cells[1, 20].Value = "A4";
                //workSheet.Cells[1, 21].Value = "Score";
                //workSheet.Cells[1, 22].Value = "Negative";

                //workSheet.Cells[1, 23].Value = "Q5";
                //workSheet.Cells[1, 24].Value = "A5";
                //workSheet.Cells[1, 25].Value = "Score";
                //workSheet.Cells[1, 26].Value = "Negative";
                //workSheet.Cells[1, 27].Value = "Q6";
                //workSheet.Cells[1, 28].Value = "A6";
                //workSheet.Cells[1, 29].Value = "Score";
                //workSheet.Cells[1, 30].Value = "Negative";

                //workSheet.Cells[1, 31].Value = "Q7";
                //workSheet.Cells[1, 32].Value = "A7";
                //workSheet.Cells[1, 33].Value = "Score";
                //workSheet.Cells[1, 34].Value = "Negative";
                //workSheet.Cells[1, 35].Value = "Q8";
                //workSheet.Cells[1, 36].Value = "A8";
                //workSheet.Cells[1, 37].Value = "Score";
                //workSheet.Cells[1, 38].Value = "Negative";

                //workSheet.Cells[1, 39].Value = "Q9";
                //workSheet.Cells[1, 40].Value = "A9";
                //workSheet.Cells[1, 41].Value = "Score";
                //workSheet.Cells[1, 42].Value = "Negative";
                //workSheet.Cells[1, 43].Value = "Q10";
                //workSheet.Cells[1, 44].Value = "A10";
                //workSheet.Cells[1, 45].Value = "Score";
                //workSheet.Cells[1, 46].Value = "Negative";

                //workSheet.Cells[1, 47].Value = "CQ1";
                //workSheet.Cells[1, 48].Value = "CA1";
                //workSheet.Cells[1, 49].Value = "Score";
                //workSheet.Cells[1, 50].Value = "Negative";

                //workSheet.Cells[1, 51].Value = "CQ2";
                //workSheet.Cells[1, 52].Value = "CA2";
                //workSheet.Cells[1, 53].Value = "Score";
                //workSheet.Cells[1, 54].Value = "Negative";

                //workSheet.Cells[1, 55].Value = "CQ3";
                //workSheet.Cells[1, 56].Value = "CA3";
                //workSheet.Cells[1, 57].Value = "Score";
                //workSheet.Cells[1, 58].Value = "Negative";

                //workSheet.Cells[1, 59].Value = "CQ4";
                //workSheet.Cells[1, 60].Value = "CA4";
                //workSheet.Cells[1, 61].Value = "Score";
                //workSheet.Cells[1, 62].Value = "Negative";

                //workSheet.Cells[1, 63].Value = "CQ5";
                //workSheet.Cells[1, 64].Value = "CA5";
                //workSheet.Cells[1, 65].Value = "Score";
                //workSheet.Cells[1, 66].Value = "Negative";

                //workSheet.Cells[1, 67].Value = "CQ6";
                //workSheet.Cells[1, 68].Value = "CA6";
                //workSheet.Cells[1, 69].Value = "Score";
                //workSheet.Cells[1, 70].Value = "Negative";

                //workSheet.Cells[1, 71].Value = "CQ7";
                //workSheet.Cells[1, 72].Value = "CA7";
                //workSheet.Cells[1, 73].Value = "Score";
                //workSheet.Cells[1, 74].Value = "Negative";

                //workSheet.Cells[1, 75].Value = "CQ8";
                //workSheet.Cells[1, 76].Value = "CA8";
                //workSheet.Cells[1, 77].Value = "Score";
                //workSheet.Cells[1, 78].Value = "Negative";

                //workSheet.Cells[1, 79].Value = "CQ9";
                //workSheet.Cells[1, 80].Value = "CA9";
                //workSheet.Cells[1, 81].Value = "Score";
                //workSheet.Cells[1, 82].Value = "Negative";

                //workSheet.Cells[1, 83].Value = "CQ10";
                //workSheet.Cells[1, 84].Value = "CA10";
                //workSheet.Cells[1, 85].Value = "Score";
                //workSheet.Cells[1, 86].Value = "Negative";
                #endregion

                int recordIndex = 2;
                foreach (var val in job_survey)
                {
                    var count = val.qna.Count;
                    workSheet.Cells[recordIndex, 1].Value = val.service_no;
                    workSheet.Cells[recordIndex, 2].Value = val.sitename;
                    workSheet.Cells[recordIndex, 3].Value = val.sitecode;
                    workSheet.Cells[recordIndex, 4].Value = val.engineer;
                    workSheet.Cells[recordIndex, 5].Value = val.engineer_code;
                    workSheet.Cells[recordIndex, 6].Value = val.customer;
                    workSheet.Cells[recordIndex, 7].Value = val.customer_phone;
                    workSheet.Cells[recordIndex, 8].Value = val.survey_date.ToString("dd/MM/yyyy HH:mm");

                    if (count >= 1)
                    {
                        workSheet.Cells[recordIndex, 9].Value = val.qna[0] != null ? val.qna[0].quest : "";
                        workSheet.Cells[recordIndex, 10].Value = val.qna[0] != null ? val.qna[0].answer : "";
                        workSheet.Cells[recordIndex, 11].Value = val.qna[0] != null ? val.qna[0].score.ToString() : "";
                        workSheet.Cells[recordIndex, 12].Value = val.qna[0] != null ? val.qna[0].negative.ToString() : "";

                        if (count >= 2)
                        {
                            workSheet.Cells[recordIndex, 13].Value = val.qna[1] != null ? val.qna[1].quest : "";
                            workSheet.Cells[recordIndex, 14].Value = val.qna[1] != null ? val.qna[1].answer : "";
                            workSheet.Cells[recordIndex, 15].Value = val.qna[1] != null ? val.qna[1].score.ToString() : "";
                            workSheet.Cells[recordIndex, 16].Value = val.qna[1] != null ? val.qna[1].negative.ToString() : "";

                            if (count >= 3)
                            {
                                workSheet.Cells[recordIndex, 17].Value = val.qna[2] != null ? val.qna[2].quest : "";
                                workSheet.Cells[recordIndex, 18].Value = val.qna[2] != null ? val.qna[2].answer : "";
                                workSheet.Cells[recordIndex, 19].Value = val.qna[2] != null ? val.qna[2].score.ToString() : "";
                                workSheet.Cells[recordIndex, 20].Value = val.qna[2] != null ? val.qna[2].negative.ToString() : "";
                                if (count >= 4)
                                {
                                    workSheet.Cells[recordIndex, 21].Value = val.qna[3] != null ? val.qna[3].quest : "";
                                    workSheet.Cells[recordIndex, 22].Value = val.qna[3] != null ? val.qna[3].answer : "";
                                    workSheet.Cells[recordIndex, 23].Value = val.qna[3] != null ? val.qna[3].score.ToString() : "";
                                    workSheet.Cells[recordIndex, 24].Value = val.qna[3] != null ? val.qna[3].negative.ToString() : "";
                                    if (count >= 5)
                                    {
                                        workSheet.Cells[recordIndex, 25].Value = val.qna[4] != null ? val.qna[4].quest : "";
                                        workSheet.Cells[recordIndex, 26].Value = val.qna[4] != null ? val.qna[4].answer : "";
                                        workSheet.Cells[recordIndex, 27].Value = val.qna[4] != null ? val.qna[4].score.ToString() : "";
                                        workSheet.Cells[recordIndex, 28].Value = val.qna[4] != null ? val.qna[4].negative.ToString() : "";

                                        if (count >= 6)
                                        {
                                            workSheet.Cells[recordIndex, 29].Value = val.qna[5] != null ? val.qna[5].quest : "";
                                            workSheet.Cells[recordIndex, 30].Value = val.qna[5] != null ? val.qna[5].answer : "";
                                            workSheet.Cells[recordIndex, 31].Value = val.qna[5] != null ? val.qna[5].score.ToString() : "";
                                            workSheet.Cells[recordIndex, 32].Value = val.qna[5] != null ? val.qna[5].negative.ToString() : "";

                                            if (count >= 7)
                                            {

                                                workSheet.Cells[recordIndex, 33].Value = val.qna[6] != null ? val.qna[6].quest : "";
                                                workSheet.Cells[recordIndex, 34].Value = val.qna[6] != null ? val.qna[6].answer : "";
                                                workSheet.Cells[recordIndex, 35].Value = val.qna[6] != null ? val.qna[6].score.ToString() : "";
                                                workSheet.Cells[recordIndex, 36].Value = val.qna[6] != null ? val.qna[6].negative.ToString() : "";

                                                if (count >= 8)
                                                {
                                                    workSheet.Cells[recordIndex, 37].Value = val.qna[7] != null ? val.qna[7].quest : "";
                                                    workSheet.Cells[recordIndex, 38].Value = val.qna[7] != null ? val.qna[7].answer : "";
                                                    workSheet.Cells[recordIndex, 39].Value = val.qna[7] != null ? val.qna[7].score.ToString() : "";
                                                    workSheet.Cells[recordIndex, 40].Value = val.qna[7] != null ? val.qna[7].negative.ToString() : "";
                                                    if (count >= 9)
                                                    {
                                                        workSheet.Cells[recordIndex, 41].Value = val.qna[8] != null ? val.qna[8].quest : "";
                                                        workSheet.Cells[recordIndex, 42].Value = val.qna[8] != null ? val.qna[8].answer : "";
                                                        workSheet.Cells[recordIndex, 43].Value = val.qna[8] != null ? val.qna[8].score.ToString() : "";
                                                        workSheet.Cells[recordIndex, 44].Value = val.qna[8] != null ? val.qna[8].negative.ToString() : "";
                                                        if (count >= 10)
                                                        {
                                                            workSheet.Cells[recordIndex, 45].Value = val.qna[9] != null ? val.qna[9].quest : "";
                                                            workSheet.Cells[recordIndex, 46].Value = val.qna[9] != null ? val.qna[9].answer : "";
                                                            workSheet.Cells[recordIndex, 47].Value = val.qna[9] != null ? val.qna[9].score.ToString() : "";
                                                            workSheet.Cells[recordIndex, 48].Value = val.qna[9] != null ? val.qna[9].negative.ToString() : "";

                                                            // Category Quest

                                                            if (count >= 11)
                                                            {
                                                                workSheet.Cells[recordIndex, 49].Value = val.qna[10] != null ? val.qna[10].quest : "";
                                                                workSheet.Cells[recordIndex, 50].Value = val.qna[10] != null ? val.qna[10].answer : "";
                                                                workSheet.Cells[recordIndex, 51].Value = val.qna[10] != null ? val.qna[10].score.ToString() : "";
                                                                workSheet.Cells[recordIndex, 52].Value = val.qna[10] != null ? val.qna[10].negative.ToString() : "";

                                                                if (count >= 12)
                                                                {
                                                                    workSheet.Cells[recordIndex, 53].Value = val.qna[11] != null ? val.qna[11].quest : "";
                                                                    workSheet.Cells[recordIndex, 54].Value = val.qna[11] != null ? val.qna[11].answer : "";
                                                                    workSheet.Cells[recordIndex, 55].Value = val.qna[11] != null ? val.qna[11].score.ToString() : "";
                                                                    workSheet.Cells[recordIndex, 56].Value = val.qna[11] != null ? val.qna[11].negative.ToString() : "";
                                                                    if (count >= 13)
                                                                    {
                                                                        workSheet.Cells[recordIndex, 57].Value = val.qna[12] != null ? val.qna[12].quest : "";
                                                                        workSheet.Cells[recordIndex, 58].Value = val.qna[12] != null ? val.qna[12].answer : "";
                                                                        workSheet.Cells[recordIndex, 59].Value = val.qna[12] != null ? val.qna[12].score.ToString() : "";
                                                                        workSheet.Cells[recordIndex, 60].Value = val.qna[12] != null ? val.qna[12].negative.ToString() : "";

                                                                        if (count >= 14)
                                                                        {
                                                                            workSheet.Cells[recordIndex, 61].Value = val.qna[13] != null ? val.qna[13].quest : "";
                                                                            workSheet.Cells[recordIndex, 62].Value = val.qna[13] != null ? val.qna[13].answer : "";
                                                                            workSheet.Cells[recordIndex, 63].Value = val.qna[13] != null ? val.qna[13].score.ToString() : "";
                                                                            workSheet.Cells[recordIndex, 64].Value = val.qna[13] != null ? val.qna[13].negative.ToString() : "";

                                                                            if (count >= 15)
                                                                            {
                                                                                workSheet.Cells[recordIndex, 65].Value = val.qna[14] != null ? val.qna[14].quest : "";
                                                                                workSheet.Cells[recordIndex, 66].Value = val.qna[14] != null ? val.qna[14].answer : "";
                                                                                workSheet.Cells[recordIndex, 67].Value = val.qna[14] != null ? val.qna[14].score.ToString() : "";
                                                                                workSheet.Cells[recordIndex, 68].Value = val.qna[14] != null ? val.qna[14].negative.ToString() : "";

                                                                                if (count >= 16)
                                                                                {
                                                                                    workSheet.Cells[recordIndex, 69].Value = val.qna[15] != null ? val.qna[15].quest : "";
                                                                                    workSheet.Cells[recordIndex, 70].Value = val.qna[15] != null ? val.qna[15].answer : "";
                                                                                    workSheet.Cells[recordIndex, 71].Value = val.qna[15] != null ? val.qna[15].score.ToString() : "";
                                                                                    workSheet.Cells[recordIndex, 72].Value = val.qna[15] != null ? val.qna[15].negative.ToString() : "";
                                                                                    if (count >= 17)
                                                                                    {
                                                                                        workSheet.Cells[recordIndex, 73].Value = val.qna[16] != null ? val.qna[16].quest : "";
                                                                                        workSheet.Cells[recordIndex, 74].Value = val.qna[16] != null ? val.qna[16].answer : "";
                                                                                        workSheet.Cells[recordIndex, 75].Value = val.qna[16] != null ? val.qna[16].score.ToString() : "";
                                                                                        workSheet.Cells[recordIndex, 76].Value = val.qna[16] != null ? val.qna[16].negative.ToString() : "";

                                                                                        if (count >= 18)
                                                                                        {
                                                                                            workSheet.Cells[recordIndex, 77].Value = val.qna[17] != null ? val.qna[17].quest : "";
                                                                                            workSheet.Cells[recordIndex, 78].Value = val.qna[17] != null ? val.qna[17].answer : "";
                                                                                            workSheet.Cells[recordIndex, 79].Value = val.qna[17] != null ? val.qna[17].score.ToString() : "";
                                                                                            workSheet.Cells[recordIndex, 80].Value = val.qna[17] != null ? val.qna[17].negative.ToString() : "";

                                                                                            if (count >= 19)
                                                                                            {
                                                                                                workSheet.Cells[recordIndex, 81].Value = val.qna[18] != null ? val.qna[18].quest : "";
                                                                                                workSheet.Cells[recordIndex, 82].Value = val.qna[18] != null ? val.qna[18].answer : "";
                                                                                                workSheet.Cells[recordIndex, 83].Value = val.qna[18] != null ? val.qna[18].score.ToString() : "";
                                                                                                workSheet.Cells[recordIndex, 84].Value = val.qna[18] != null ? val.qna[18].negative.ToString() : "";

                                                                                                if (count >= 20)
                                                                                                {
                                                                                                    workSheet.Cells[recordIndex, 85].Value = val.qna[19] != null ? val.qna[19].quest : "";
                                                                                                    workSheet.Cells[recordIndex, 86].Value = val.qna[19] != null ? val.qna[19].answer : "";
                                                                                                    workSheet.Cells[recordIndex, 87].Value = val.qna[19] != null ? val.qna[19].score.ToString() : "";
                                                                                                    workSheet.Cells[recordIndex, 88].Value = val.qna[19] != null ? val.qna[19].negative.ToString() : "";
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    workSheet.Cells[recordIndex, 89].Value = val.comment;
                    workSheet.Cells[recordIndex, 90].Value = (val.is_feedback == 1 ? "อ่านแล้ว" : "ยังไม่อ่าน");
                    workSheet.Cells[recordIndex, 91].Value = val.feedback_note;
                    workSheet.Cells[recordIndex, 92].Value = (val.feedback_date != null ? val.feedback_date.Value.ToString("dd/MM/yyyy HH:mm") : "");
                    workSheet.Cells[recordIndex, 93].Value = val.user_feedback;
                    workSheet.Cells[recordIndex, 94].Value = val.is_negative == 1 ? "Negative" : "Positive";

                    recordIndex++;
                }

                for (int i = 1; i <= 6; i++)
                {
                    workSheet.Column(i).AutoFit();
                }

                var name_file = "survey " + start.ToString("dd/MM/yyyy") + end.ToString("dd/MM/yyyy") + ".xlsx";
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

        }


        [AllowAnonymous]
        public ActionResult NegativeForm(string token, int survey = 0)
        {

            var check_token = token;
            check_token = check_token.Replace(" ", "+");

            try
            {
                var de_token = byi_common.encryption.Decrypt(check_token);
                JObject json = JObject.Parse(de_token);
                var key = json["key"].ToString();
                var date = json["exp"].ToString();

                // เช็ค Token เรื่องสิทธิ์ของเวลา
                if (key.ToString() == "SecretKeyByi")
                {

                    var save_open = db.tb_survey.Where(w => w.is_delete == 0 && w.id == survey).FirstOrDefault();
                    if (save_open != null && save_open.open_link_date == null)
                    {
                        save_open.update_date = now;
                        save_open.user_update = User.Identity.Name;
                        save_open.open_link_date = now;
                        db.tb_survey.AddOrUpdate(save_open);
                        db.SaveChanges();
                    }

                    if (survey > 0)
                    {
                        Survey_Export negative_sur = (from s in db.tb_survey

                                                      join j in db.tb_jobs on s.job_guid equals j.job_guid
                                                      join e in db.tb_engineer on j.engineer_id equals e.id
                                                      where s.is_delete == 0 && j.is_delete == 0 && e.is_delete == 0 && s.id == survey
                                                      select new Survey_Export
                                                      {
                                                          id = s.id,
                                                          service_no = j.service_order_no,
                                                          engineer = e.engineer_name,
                                                          engineer_code = j.engineer_code,
                                                          customer = j.customer_fullname,
                                                          customer_phone = j.phone_mobile,
                                                          survey_date = s.create_date,
                                                          total_score = s.source,
                                                          comment = s.comment_1,
                                                          is_feedback = s.is_feedback,
                                                          feedback_date = s.feedback_create_date,
                                                          user_feedback = s.feeback_user_update,

                                                          qna = (from item in db.tb_survey_item
                                                                 where item.survey_id == s.id && item.is_negative == 1
                                                                 select new QnA_Export
                                                                 {
                                                                     id = item.id,
                                                                     setorder = item.setorder,
                                                                     quest = item.question,
                                                                     answer = item.answer,
                                                                     score = item.source,
                                                                     negative = item.is_negative,
                                                                     cate = db.tb_jobsl_category.Where(w => w.is_delete == 0 && w.id == item.jobs_category_id).Select(s => s.name).FirstOrDefault() != null ? db.tb_jobsl_category.Where(w => w.is_delete == 0 && w.id == j.job_category_id).Select(s => s.name).FirstOrDefault() : "",
                                                                     sub_type = item.survey_sub_type
                                                                 }).ToList()
                                                      }).FirstOrDefault();

                        ViewData["survey"] = negative_sur;
                        return View("negative_form");
                    }
                    else
                    {
                        return Index();
                    }
                }
                else
                {
                    return Index();
                }
            }
            catch
            {
                return Index();
            }

        }

        [AllowAnonymous]
        public object SaveNegativeForm(int id, string textarea, string input)
        {
            var survey = db.tb_survey.Where(w => w.is_delete == 0 && w.id == id).FirstOrDefault();
            if (survey != null)
            {
                survey.is_feedback = 1;
                survey.feedback_note = textarea;
                survey.feeback_user_update = input;
                survey.feedback_create_date = now;
                survey.feedback_update_date = now;
                survey.update_date = now;
                survey.user_update = User.Identity.Name;
                db.tb_survey.AddOrUpdate(survey);
                db.SaveChanges();
                return "true";
            }
            else
            {
                return "false";
            }


            //string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            //return jsonString;
        }


    }
}