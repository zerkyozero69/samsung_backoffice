using CommonLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Configuration;
using System.Collections;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.IO;
using s_tracking_mobile.Models;
using byi_common;
using System.Text.RegularExpressions;
using System.Data.Entity.Migrations;
using System.Threading.Tasks;

namespace s_tracking_mobile.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        MembershipUser user;
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();

        // GET: Register
        public ActionResult Index()
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        public ActionResult AddUser()
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                var get_site = (from s in db.tb_store where s.is_delete == 0 select new getSite() { id = s.id, name = s.site_name }).ToList();
                ViewData["All-Site"] = get_site;
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            return View();
        }

        public ActionResult all()
        {
            if (User.IsInRole("admin"))
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                //var test = Request.Cookies;
                ViewData["get_Role"] = User.IsInRole("admin");
                //var data = Membership.GetAllUsers().Cast<MembershipUser>().Skip(0).Take(10);
                //int totalUsers;

                //var name_admin = Roles.GetUsersInRole("admin").Skip(0).Take(5);
                //var name_site = Roles.GetUsersInRole("shop");


                //var etst = Membership.GetAllUsers().Cast<MembershipUser>().Select(m => m.UserName).Skip(0).Take(10);


                var data = (from u in Membership.GetAllUsers().Cast<MembershipUser>()
                            orderby u.UserName
                            select new get_Member()
                            {
                                name = u.UserName,
                                create_date = u.CreationDate,
                                last_login = u.LastLoginDate,
                                providerUserKey = u.ProviderUserKey,
                                IsLockedOut = u.IsLockedOut,
                                roles = (from r in Roles.GetRolesForUser(u.UserName) select r).ToList()
                            }).Skip(0).Take(20).ToList();
                int count = 0;
                count = (from u in Membership.GetAllUsers().Cast<MembershipUser>() select u).Count();
                //var userList = new List<get_Member>();

                //foreach (var item in data)
                //{
                //    var getRole = Roles.GetRolesForUser(item.name).ToList();
                //    int engineer = getRole.IndexOf("engineer");
                //    int admin = getRole.IndexOf("admin");
                //    int shop = getRole.IndexOf("shop");
                //    if (engineer < 0 || (engineer >= 0 && admin >= 0 || shop >= 0))
                //    {
                //        if (userList.Count() < 19)
                //        {
                //            userList.Add(item);
                //        }
                //        count++;
                //    }
                //}

                //SELECT TOP 1000
                //UR.userId, U.UserName

                //  FROM[dbo].[UsersInRoles]
                //        UR
                // inner join[dbo].[Roles] R on UR.RoleId = R.RoleId
                //inner join[dbo].[Users] U on UR.UserId = U.UserId
                //WHERE R.RoleName IN('admin','engineer')



                //string[] result = name_admin.Union(name_site).ToArray();
                //var userList = new List<get_Member>();
                //result = result.Distinct().ToArray();

                //for (int i = 0; i < 20; i++)
                //{
                //    MembershipUser u = Membership.GetUser(result[i]);
                //    var getRole = Roles.GetRolesForUser(result[i]).ToList();
                //    userList.Add(new get_Member()
                //    {
                //        name = u.UserName,
                //        create_date = u.CreationDate,
                //        last_login = u.LastLoginDate,
                //        providerUserKey = u.ProviderUserKey,
                //        roles = getRole
                //    });

                //}



                //var data = Membership.GetAllUsers(0, 40, out totalUsers);
                //var userList = new List<MembershipUser>();
                //var saveRole = new List<object>();
                //int count = 0;
                //foreach (MembershipUser user in data)
                //{
                //    var getRole = Roles.GetRolesForUser(user.UserName).ToList();
                //    int engineer = getRole.IndexOf("engineer");
                //    int admin = getRole.IndexOf("admin");
                //    int shop = getRole.IndexOf("shop");
                //    if (engineer < 0 || (engineer >= 0 && admin >= 0 || shop >= 0))
                //    {
                //        if (userList.Count() < 19)
                //        {
                //            userList.Add(user);
                //            saveRole.Add(getRole);
                //        }
                //        count++;
                //    }
                //}

                ViewData["data"] = data;
                ViewData["count"] = count;
                //ViewData["AllRole"] = saveRole;
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            return View();
        }

        public ActionResult edit(string username ="")
        {
            if (User.IsInRole("admin") && username != "")
            {
                ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
                var getUser = Membership.GetUser(username);
                var userList = new List<MembershipUser>();
                userList.Add(getUser);
                ViewData["user"] = userList;
                var getRole = Roles.GetRolesForUser(username).ToList();

                //test

                
                //end test

                Guid convertGuid = new Guid(getUser.ProviderUserKey.ToString());
                ViewData["Id-Site"] = getRole.Contains("shop") == false ? 0 : db.tb_mapping_store.Where(w => w.account_guid == convertGuid).Select(s => s.site_id).FirstOrDefault();
                var get_site = (from s in db.tb_store where s.is_delete == 0 select new getSite() { id = s.id, name = s.site_name }).ToList();
                ViewData["All-Site"] = get_site;
                ViewData["Role-Admin"] = getRole.Contains("admin");
                ViewData["Role-Shop"] = getRole.Contains("shop");
                ViewData["Role-Engineer"] = getRole.Contains("engineer");
                ViewData["Role-admin-installer"] = getRole.Contains("admin_installer");
                ViewData["Role-installer"] = getRole.Contains("installer");
            }
            else
            {
                //wait redirect
                var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
                Response.Redirect(url);
            }
            
            
            return View();
        }

        [HttpPost]
        public object Regis(string user , string email , string pass, string conpass , List<string> role, int id_site)
        {
            List<validate_all> validates = new List<validate_all>();
            Regex regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})");
            Match match = regex.Match(pass);
            Match match2 = regex.Match(conpass);

            if (user != "" && user != null) //user
            {
                var checkUser = common.xss_input_string(user, user.Length);
                if (checkUser == false) { validates.Add(new validate_all { name_div = "#txtName", text = "รูปแบบชื่อไม่ถูกต้อง" }); } else
                {
                    var getUser = Membership.GetUser(user);
                    if(getUser != null)
                    {
                        validates.Add(new validate_all { name_div = "#txtName", text = "Username ซ้ำกับในระบบ" });
                    }
                }
            }
            else { validates.Add(new validate_all { name_div = "#txtName", text = "กรุณาระบุชื่อ" }); }

            if (email != "" && email != null) //email
            {
                var checkEmail = common.isEmailFormat(email);
                if (checkEmail == false) { validates.Add(new validate_all { name_div = "#txtEmail", text = "รูปแบบอีเมล์ไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtEmail", text = "กรุณาระบุอีเมล์" }); }

            if (pass != "" && pass != null) //pass
            {
                var checkPassword = common.xss_input_string(pass, pass.Length);
                if (checkPassword == false || !match.Success) { validates.Add(new validate_all { name_div = "#txtPassword", text = "รูปแบบรหัสผ่านไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtPassword", text = "กรุณาระบุรหัสผ่าน" }); }

            if (conpass != "" && conpass != null) //conpass
            {
                var checkConfirmPassword = common.xss_input_string(conpass, conpass.Length);
                if (checkConfirmPassword == false || !match2.Success) { validates.Add(new validate_all { name_div = "#txtConfirmPassword", text = "รูปแบบยืนยันรหัสผ่านไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtConfirmPassword", text = "กรุณาระบุยืนยันรหัสผ่าน" }); }

            if (validates.Count() == 0 && User.IsInRole("admin"))
            {
                MembershipUser newUser = Membership.CreateUser(user, pass, email);
                Membership.UpdateUser(newUser);

                foreach (var item2 in role)
                {
                    Roles.AddUserToRole(user, item2);
                }

                if (role.Contains("shop"))
                {
                    //set mapping and save
                    var CheckUser = Membership.GetUser(user).ProviderUserKey;
                    Guid convertGuid = new Guid(CheckUser.ToString());

                    tb_mapping_store obj_new = new tb_mapping_store();
                    obj_new.account_guid = convertGuid;
                    obj_new.site_id = id_site;
                    db.tb_mapping_store.Add(obj_new);
                    db.SaveChanges();
                }

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
        public void btn_Edit(string email, string us, List<string> role, int id_site)
        {
            if (User.IsInRole("admin"))
            {
                user = Membership.GetUser(us);
                user.Email = email;
                Membership.UpdateUser(user);
                var getRole = Roles.GetRolesForUser(us).ToList();
                foreach (var item in getRole)
                {
                    Roles.RemoveUserFromRole(us, item);
                }

                foreach (var item2 in role)
                {
                    Roles.AddUserToRole(us, item2);
                }

                if (role.Contains("shop"))
                {
                    //set mapping and save
                    var mapping = db.tb_mapping_store.Where(w => w.account_guid.ToString() == user.ProviderUserKey.ToString()).FirstOrDefault();
                    if (mapping != null)
                    {
                        mapping.account_guid = mapping.account_guid;
                        mapping.site_id = id_site;
                        db.tb_mapping_store.AddOrUpdate(mapping);
                        db.SaveChanges();
                    }
                    // fix change error
                    else
                    {
                
                        Guid convertGuid = new Guid(user.ProviderUserKey.ToString());
                        tb_mapping_store obj_new = new tb_mapping_store();
                        obj_new.account_guid = convertGuid;
                        obj_new.site_id = id_site;
                        db.tb_mapping_store.Add(obj_new);
                        db.SaveChanges();
                    }
       
                }
            }
        }

        [HttpPost]
        public void delete(string user)
        {
            if (User.IsInRole("admin"))
            {
                var user1 = Membership.GetUser(user);
                var mapping = db.tb_mapping_store.Where(w => w.account_guid.ToString() == user1.ProviderUserKey.ToString()).FirstOrDefault();
                if (mapping != null)
                {
                    db.tb_mapping_store.Remove(mapping);
                    db.SaveChanges();
                }

                Membership.DeleteUser(user);
            }
            
        }

        [HttpPost]
        public void change_Password(string oldPass, string newPass, string username) {
            if (User.IsInRole("admin"))
            {
                MembershipUser user22 = Membership.GetUser(username);
                user22.ChangePassword(oldPass, newPass);
            }
        }

        [HttpPost]
        public object reset_password(string newpass, string confirmpassword, string username)
        {
            List<validate_all> validates = new List<validate_all>();
            Regex regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})");
            Match match = regex.Match(newpass);
            Match match2 = regex.Match(confirmpassword);

            if (newpass != "" && newpass != null) //pass
            {
                var checkPassword = common.xss_input_string(newpass, newpass.Length);
                if (checkPassword == false || !match.Success) { validates.Add(new validate_all { name_div = "#ResetPassword", text = "รูปแบบรหัสผ่านไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtPassword", text = "กรุณาระบุรหัสผ่าน" }); }

            if (confirmpassword != "" && confirmpassword != null) //pass
            {
                var checkPassword = common.xss_input_string(confirmpassword, confirmpassword.Length);
                if (checkPassword == false || !match2.Success) { validates.Add(new validate_all { name_div = "#txtConfirmPassword", text = "รูปแบบรหัสผ่านไม่ถูกต้อง" }); }
            }
            else { validates.Add(new validate_all { name_div = "#txtPassword", text = "กรุณาระบุรหัสผ่าน" }); }


            if (validates.Count() == 0 && User.IsInRole("admin"))
            {
                MembershipUser user22 = Membership.GetUser(username);
                user22.ChangePassword(user22.ResetPassword(), newpass);
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
        public object IsLockedOut(string username)
        {
            var getUser = Membership.GetUser(username);
            getUser.UnlockUser();
            Membership.UpdateUser(getUser);
            return true;
        }

        [HttpGet]
        public object Search_User(string username, string role, int page)
        {
            var skipPage = page == 1 ? 0 : page == 2 ? page * 10 : page == 3 ? (page * 10) + 10 : ((page * 10) + (page * 10)) - 20;
            //var limit = 19;
            string jsonString = "";
            var userList = new List<get_Member>();
            int count = 0;
            if (User.IsInRole("admin"))
            {
                //check username
                if(username != "")
                {
                    var data = (from u in Membership.GetAllUsers().Cast<MembershipUser>()
                                where u.UserName.Contains(username.Trim())
                                orderby u.UserName
                                select new get_Member()
                                {
                                    name = u.UserName,
                                    create_date = u.CreationDate,
                                    last_login = u.LastLoginDate,
                                    providerUserKey = u.ProviderUserKey,
                                    IsLockedOut = u.IsLockedOut,
                                    roles = (from r in Roles.GetRolesForUser(u.UserName) select r).ToList()
                                }).ToList();

                    //all

                    if(role != "all")
                    {
                        foreach (var item in data)
                        {
                            var getRole = Roles.GetRolesForUser(item.name).ToList();
                            int check_role = getRole.IndexOf(role);
                            if (check_role >= 0)
                            {
                                userList.Add(item);
                                count++;
                            }
                        }
                        userList = userList.Skip(skipPage).Take(20).ToList();
                    }
                    else
                    {
                        count = data.Count();
                        foreach (var item in data)
                        {
                            userList.Add(item);
                        }
                        userList = userList.Skip(skipPage).Take(20).ToList();
                    }

                    //if (role == "all")
                    //{
                    //    foreach (var item in data)
                    //    {
                    //        var getRole = Roles.GetRolesForUser(item.name).ToList();
                    //        int engineer = getRole.IndexOf("engineer");
                    //        int admin = getRole.IndexOf("admin");
                    //        int shop = getRole.IndexOf("shop");
                    //        if (engineer < 0 || (engineer >= 0 && admin >= 0 || shop >= 0))
                    //        {
                    //            if (userList.Count() < 19)
                    //            {
                    //                userList.Add(item);
                    //            }
                    //            count++;
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    foreach (var item in data)
                    //    {
                    //        var getRole = Roles.GetRolesForUser(item.name).ToList();
                    //        int check_role = getRole.IndexOf(role);
                    //        if (check_role>=0)
                    //        {
                    //            if (userList.Count() < 19)
                    //            {
                    //                userList.Add(item);
                    //            }
                    //            count++;
                    //        }
                    //    }
                    //}
                    
                }
                else
                {
                    if(role != "all")
                    {
                        var data = (from u in Membership.GetAllUsers().Cast<MembershipUser>()
                                    //where u.UserName.Contains(username.Trim())
                                    orderby u.UserName
                                    select new get_Member()
                                    {
                                        name = u.UserName,
                                        create_date = u.CreationDate,
                                        last_login = u.LastLoginDate,
                                        providerUserKey = u.ProviderUserKey,
                                        IsLockedOut = u.IsLockedOut,
                                        roles = (from r in Roles.GetRolesForUser(u.UserName) select r).ToList()
                                    }).ToList();

                        //count = (from u in Membership.GetAllUsers().Cast<MembershipUser>() select u).Count();

                        foreach (var item in data)
                        {
                            var getRole = Roles.GetRolesForUser(item.name).ToList();
                            int check_role = getRole.IndexOf(role);
                            if (check_role >= 0)
                            {
                                userList.Add(item);
                                count++;
                            }
                        }

                        userList = userList.Skip(skipPage).Take(20).ToList();
                    }
                    else
                    {
                        var data = (from u in Membership.GetAllUsers().Cast<MembershipUser>()
                                    //where u.UserName.Contains(username.Trim()) 
                                    orderby u.UserName
                                    select new get_Member()
                                    {
                                        name = u.UserName,
                                        create_date = u.CreationDate,
                                        last_login = u.LastLoginDate,
                                        providerUserKey = u.ProviderUserKey,
                                        IsLockedOut = u.IsLockedOut,
                                        roles = (from r in Roles.GetRolesForUser(u.UserName) select r).ToList()
                                    }).ToList();

                        //count = (from u in Membership.GetAllUsers().Cast<MembershipUser>() where u.UserName.Contains(username.Trim()) select u).Count();
                        //var datatest = data.Count();
                        //limit = (skipPage + 20) > datatest ? (datatest - skipPage) : limit;

                        //data = data.Skip(skipPage).Take(limit).ToList();

                        foreach (var item in data)
                        {
                            //var getRole = Roles.GetRolesForUser(item.name).ToList();
                            //int engineer = getRole.IndexOf("engineer");
                            //int admin = getRole.IndexOf("admin");
                            //int shop = getRole.IndexOf("shop");
                            //if (engineer < 0 || (engineer >= 0 && admin >= 0 || shop >= 0))
                            //{
                            //    if (userList.Count() < 19)
                            //    {
                            //        userList.Add(item);
                            //    }

                            //}
                            userList.Add(item);
                        }
                        count = data.Count();
                        userList = userList.Skip(skipPage).Take(20).ToList();
                    }
                }

                userList.Add(new get_Member()
                {
                    name = null,
                    create_date = new DateTime(),
                    last_login = new DateTime(),
                    roles = null,
                    count = count
                });

                jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(userList);
            }

            return new ContentResult()
            {
                Content = jsonString,
                ContentType = "application/json"
            };
        }

    }
}