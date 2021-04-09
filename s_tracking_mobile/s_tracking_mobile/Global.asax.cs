using Hangfire;
using s_tracking_mobile.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace s_tracking_mobile
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RegisterRoutes(RouteTable.Routes);
            //MvcHandler.DisableMvcResponseHeader = true;
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);   
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app != null && app.Context != null)
            {
                app.Context.Response.Headers.Remove("Server");
                app.Context.Response.Headers.Remove("server");
            }
        }

        protected void Application_PreSendRequestHeaders(object source, EventArgs e)
        {
            HttpContext.Current.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubdomains;");
            HttpContext.Current.Response.Headers.Add("X-Frame-Options", "DENY");
            HttpContext.Current.Response.Headers.Add("Cache-Contro", " no-cache, no-store");
            HttpContext.Current.Response.Headers.Add("Pragma", " no-cache");
            HttpContext.Current.Response.Headers.Add("Expires", "-1");
            HttpContext.Current.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            HttpContext.Current.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            HttpContext.Current.Response.Headers.Remove("Server");
            if (Response.Cookies.Count > 0)
            {
                foreach (string s in Response.Cookies.AllKeys)
                {
                    if (s == FormsAuthentication.FormsCookieName || s.ToLower() == "asp.net_sessionid")
                    {
                        Response.Cookies[s].Secure = true;
                    }
                }
            }
        }

        private static void RegisterRoutes(RouteCollection routes)
        {
            // System.Web.Routing.RouteTable.Routes.MapPageRoute("", "speedtest/en/", "~/TestSpeed/inter_Default.aspx", false, new RouteValueDictionary { { "lang", "en" } });
            routes.MapRoute(name: "t_index", url: "t/{jobsid}", defaults: new { Controller = "t", Action = "Index"  });
            routes.MapRoute(name: "t_edit", url: "t/edit/location", defaults: new { Controller = "t", Action = "edit" });
        }

        //test config 24/7
        public class Global : HttpApplication
        {
            protected void Application_Start(object sender, EventArgs e)
            {
                HangfireBootstrapper.Instance.Start();
            }

            protected void Application_End(object sender, EventArgs e)
            {
                HangfireBootstrapper.Instance.Stop();
            }
        }



    }
}
