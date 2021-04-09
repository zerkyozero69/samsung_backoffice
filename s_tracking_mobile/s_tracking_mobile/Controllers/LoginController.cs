using CommonLib;
using Jose;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;

namespace s_tracking_mobile.Controllers
{
    public class LoginController : Controller
    {

        // private_key from the Service Account JSON file
        public string firebasePrivateKey = @"-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQD0DBRLmNjgoTRI\nP54i0St3R7FGQMZDPMT3xpJnEgsLG4dFi+7rrpAVOcSlwTQ9Dn0NyUuLLhQQ93t7\ngvehQuTkRqxivY641cW6IqalyzL6qqcdNQdMZ6RqqPuMsl/FxsVziy3PRkr2PLQW\nfvIPnPbLGu/wGdrKrI6axUVMLZJGBf5y44TACfX5O8vmaCYImxyMAFtI0SOFcIGc\niQdcyNPcYCB6/Lm4YYqNn/9m7ee55ID6tZLFgQZsZ6rMUJ+1Lf3QvDP7foVYnasM\n06Vr/Rpt8u9P6FcdNhA762k7zVL7l2D4rB6P1nNpuxhwv/OB4np4GsyPlBRLMM1A\nqCaKJLTfAgMBAAECggEAX3p6VjkuYHOxKLL7A1QiVUBPMtUUvCmCRg4CKSD+ARJ8\nEdF1X++jnypCrTaxYVHRyxr92y3j2993CBNtHkI1mrmrp9XRiK7Z9MCpwiOFhlxN\nlTex60qBINmL0LfYkF/70ACbo4Q0v7FwI3z25vTZ+QxI4v66gqCQCi0zt2H84Dek\nflJaCWpc7YPHDFOv4AAiJhTVW9iEIPM2UPpjfTczbn9fyQ0MUp7V+wXqRWfdTVA7\n8rbcv3vuNxH8QKfhJf7fkeW0O4xC074n6G7lI+/M/Nq2YRuqJ++IkVEbSVFDfW6n\nrzpLXguXZumt546qyU5XXxM+xF6eQpZFVBuiC7xeQQKBgQD/B/1BVqmBPZDbx1mK\nLFmGM7aIXaUwarJqNDhJ4h2ZNwctkfXXSjoiSdvdjnQ8vXzClccNRdnNEhyQ4RFt\nLpkM3fzfrM3rpjs6kJ2feOtMBQl7fWzHxSSmKr67Z75wCW3usEfkKX70r0Kjwcuh\ngG5nCxTMbwMqXm5tEvm5AeOA4wKBgQD0+WiJQ8qgbRBBCKop6+liNEZa3ARAfSfG\nDX2Bar8xAQQjeFrHmHTM4465BCVK7J1Tz7Jhfc8vQitHGhekISPcu2vMbVtjx/Dm\neZZMDTgjHGT8sKzPcqZL8hYbtmEHs5kkBrbff5e9fDypOpxGQ6igguIUBR13xWpa\npK+Qhh0o1QKBgBmb/sVx42GUAhjfwtEKNQ8x4DF/XdgnzrS1e7WHnDtHeFQOJSay\nUHYi/o4YAPoceQu9KJjfm8ho+i9eOnbBSCMvo3X+j0sLjUULQpjB9rbShqo85RCG\nbnC1OCPvfgMYH07pqr5xoEsH0mRJUJ5uHCgCx9rjhujZRLN1RVhbpTHLAoGBANdw\n0IKHYBgeUoEfQaeElceL+aPGgubEKsp+6rV6T5KlNiKLoiqO5YmbRtVn0/REi0g+\nDL64ihEhvDXXuJrbmlJxcahjNFBYcn/+xjM0HP6j5hxktFXsmluIF/FfP44qYK/S\nR9neuHAoZqDdrroMnqwq7vB0XyoKMDJvSwdrKxHRAoGBAL25xgOj2zwfV59scnRl\n9YCjO4Pnjov3TwHxw8LWvCbO2GPJ/VyOWyoqB1hUN8fY9xTn7TOVqS+b6DI55xH6\nSgjGUHZnfUwyRK8elNYqNSbMl7nNVLNlVtCYWugx/DCNEtW8qOKQhX5WsKnegmNT\nMznzS151GicpHyXLYJP45T/z\n-----END PRIVATE KEY-----\n";

        // Same for everyone
        public string firebasePayloadAUD = "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit";

        // client_email from the Service Account JSON file
        public string firebasePayloadISS = "firebase-adminsdk-x3wx2@samsung-engineer-tracking.iam.gserviceaccount.com";
        public string firebasePayloadSUB = "firebase-adminsdk-x3wx2@samsung-engineer-tracking.iam.gserviceaccount.com";

        // the token 'exp' - max 3600 seconds - see https://firebase.google.com/docs/auth/server/create-custom-tokens
        public int firebaseTokenExpirySecs = 3600;

        private static RsaPrivateCrtKeyParameters _rsaParams;
        private object _rsaParamsLocker = new object();




        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // MembershipUser user;
        s_tracking_mobileEntities db = new s_tracking_mobileEntities();
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        // GET: Login
        public ActionResult Index()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            var tets = CreateCaptcha();
            return View();
        }

        [Authorize]
        public ActionResult App()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];
            ViewData["Child_URL"] = ConfigurationManager.AppSettings["Child_URL"];
            return View();
        }

        [HttpGet]
        public ActionResult CreateCaptcha()
        {
            ViewData["Current_Path"] = ConfigurationManager.AppSettings["Base_URL"];

            Bitmap objBMP = new System.Drawing.Bitmap(60, 20);
            Graphics objGraphics = System.Drawing.Graphics.FromImage(objBMP);
            objGraphics.Clear(Color.Gray);

            objGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            //' Configure font to use for text
            Font objFont = new Font("Arial", 11, FontStyle.Bold);
            string randomStr = "";
            int[] myIntArray = new int[5];
            int x;

            //That is to create the random # and add it to our string
            Random autoRand = new Random();

            for (x = 0; x < 5; x++)
            {
                myIntArray[x] = System.Convert.ToInt32(autoRand.Next(0, 9));
                randomStr += (myIntArray[x].ToString());
            }

            //This is to add the string to session cookie, to be compared later
            Session.Add("randomStr", randomStr);

            //' Write out the text
            objGraphics.DrawString(randomStr, objFont, Brushes.White, 3, 3);

            //' Set the content type and return the image
            //Response.ContentType = "image/GIF";
            //objBMP.Save(Response.OutputStream, ImageFormat.Gif);
            using (var stream = new MemoryStream())
            {
                objBMP.Save(stream, ImageFormat.Gif);
                var getImg = stream.ToArray();
                var ConvertImg = Convert.ToBase64String(getImg);
                ViewBag.num = randomStr;
                ViewData["img"] = ConvertImg;

                return File(stream.ToArray(), "image/jpeg");
            }

        }

        [HttpPost]
        public async Task<ActionResult> btn_login(string username, string password, string capcha, bool remember)
        {
            string strreturn = "";
            var value = "false";
            var check_role = false;
            if (Session["randomStr"] != null)
            {
                string getSession = Session["randomStr"].ToString();
                var list_role = Roles.GetRolesForUser(username);
                if (list_role.Length > 0)
                {
                    for (var i = 0; i < list_role.Length; i++)
                    {
                        if (list_role[i] != "" && list_role[i] != "engineer") check_role = true;
                    }
                }

                if (check_role)
                {
                    if (Membership.ValidateUser(username, password) && capcha == getSession)
                    {
                        var dnow = DateTime.Now.AddDays(1);
                        string formsAuthSalt = Membership.GeneratePassword(20, 2);
                        // string userData = string.Join("|", GetCustomUserRoles());

                        FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                            1,                                     // ticket version
                            username,                              // authenticated username
                            DateTime.Now,                          // issueDate
                            new DateTime(dnow.Year, dnow.Month, dnow.Day, 0, 5, 0),           // expiryDate
                            true,                          // true to persist across browser sessions
                            formsAuthSalt,                              // can be used to store additional user data
                        FormsAuthentication.FormsCookiePath);  // the path for the cookie

                        // Encrypt the ticket using the machine key
                        string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                        // Add the cookie to the request to save it

                        HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                        cookie.Expires = remember ? DateTime.Now.AddDays(30) : DateTime.Now.AddDays(1);

                        cookie.HttpOnly = true;
                        cookie.Secure = true;

                        Response.Cookies.Add(cookie);

                        // Your redirect logic
                        // Response.Redirect(FormsAuthentication.GetRedirectUrl(username, chkremember.Checked));

                        MembershipUser user = Membership.GetUser(username);
                        user.Comment = formsAuthSalt;
                        Membership.UpdateUser(user);
                        value = "true";


                        // login child site
                        if (Roles.IsUserInRole(username, "admin") || Roles.IsUserInRole(username, "admin_installer"))
                        {
                            var std = new StandardController();
                            var model = new
                            {
                                data = std.GetBodyLogin(username, password, remember)
                            };

                            //string apiUrl = ConfigurationManager.AppSettings["Child_URL"] + "member/login";
                            string url = ConfigurationManager.AppSettings["Child_URL"] + "member/login?token=" + std.GetBodyLogin(username, password, remember);
                            strreturn = "{ \"status\"  : \"1\" ,  \"autourl\" : \"" + url + "\"}";

                            // Response.Redirect(apiUrl);
                            //   HttpClient client = new HttpClient();
                            //client.BaseAddress = new Uri(apiUrl);
                            //client.DefaultRequestHeaders.Accept.Clear();
                            //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                            ////var json = JsonConvert.SerializeObject(model);
                            ////var json_body = new StringContent(json, Encoding.UTF8, "application/json");
                            ////HttpResponseMessage response = await client.PostAsync(apiUrl, json_body);
                            //HttpResponseMessage response = await client.GetAsync(apiUrl);
                            //string responseContent = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            strreturn = "{ \"status\"  : \"3\" ,  \"autourl\" : \"" + "" + "\"}";
                        }
                    }
                }
                else
                {
                    strreturn = "{ \"status\"  : \"2\" ,  \"autourl\" : \"" + "" + "\"}";
                    //value = "engineer";
                }
            }
            else
            {
                // value = "false";
                strreturn = "{ \"status\"  : \"0\" ,  \"autourl\" : \"" + "" + "\"}";
            }

            //  return value;
            return Content(strreturn, "application/json");
        }

        [HttpGet]
        public async Task logout()
        {

            var std = new StandardController();
            string url_logout = ConfigurationManager.AppSettings["Child_URL"] + "member/logout?token=" + std.GetLogout();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url_logout);
            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(url_logout);
            string responseContent = await response.Content.ReadAsStringAsync();


            FormsAuthentication.SignOut();
            var url = ConfigurationManager.AppSettings["Base_URL"].ToString() + "login";
            Response.Redirect(url);
                       
        }

        [HttpGet]
        public async Task logoutv2()
        {

            var std = new StandardController();

            //HttpClient client = new HttpClient();
            //client.BaseAddress = new Uri(url_logout);
            ////client.DefaultRequestHeaders.Accept.Clear();
            ////client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            //HttpResponseMessage response = await client.GetAsync(url_logout);
            //string responseContent = await response.Content.ReadAsStringAsync();

            FormsAuthentication.SignOut();
            string url_logout = ConfigurationManager.AppSettings["Child_URL"] + "member/Logoutv2";

        
            Response.Redirect(url_logout);

        }

        [HttpGet]
        public bool api_logout()
        {

            var check_token = Request.QueryString["token"].ToString();
            check_token = check_token.Replace(" ", "+");
            var de_token = byi_common.encryption.Decrypt(check_token);
            JObject json = JObject.Parse(de_token);
            var key = json["key"].ToString();
            var date = json["exp"].ToString();

            var check_date = DateTime.TryParse(date, out DateTime exp);

            if (check_date && exp >= now)
            {
                FormsAuthentication.SignOut();
                return true;
            }
            else
            {
                return false;
            }


        }


        //Login Firebase
        [HttpGet]
        public string EncodeToken()
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
                var uid = "TsWxiKGYrpSoLyOPXtBsccWkTwz2";

                var claims = new Dictionary<string, object> {
                    { "is_admin", User.IsInRole("admin") ? true : false},
                    { "site_id" , idStore.ToString()}
                };

                if (_rsaParams == null)
                {
                    lock (_rsaParamsLocker)
                    {
                        if (_rsaParams == null)
                        {
                            StreamReader sr = new StreamReader(GenerateStreamFromString(firebasePrivateKey.Replace(@"\n", "\n")));
                            var pr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
                            _rsaParams = (RsaPrivateCrtKeyParameters)pr.ReadObject();
                        }
                    }
                }

                var payload = new Dictionary<string, object> {
                    {"claims", claims}
                    ,{"uid", uid}
                    ,{"iat", secondsSinceEpoch(DateTime.UtcNow)}
                    ,{"exp", secondsSinceEpoch(DateTime.UtcNow.AddSeconds(firebaseTokenExpirySecs))}
                    ,{"aud", firebasePayloadAUD}
                    ,{"iss", firebasePayloadISS}
                    ,{"sub", firebasePayloadSUB}
                };

                return Jose.JWT.Encode(payload, Org.BouncyCastle.Security.DotNetUtilities.ToRSA(_rsaParams), JwsAlgorithm.RS256);
            }
            else
            {
                return "";
            }

        }

        private static long secondsSinceEpoch(DateTime dt)
        {
            TimeSpan t = dt - new DateTime(1970, 1, 1);
            return (long)t.TotalSeconds;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}