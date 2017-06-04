using System.Web.Mvc;
using System.Web.Routing;

namespace Unbreakable.Demo {
    public static class RouteConfig {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapMvcAttributeRoutes();
        }
    }
}

