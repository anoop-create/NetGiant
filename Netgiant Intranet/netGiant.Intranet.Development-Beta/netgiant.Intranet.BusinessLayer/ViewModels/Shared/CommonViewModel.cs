using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class CommonViewModel
    {
        public CommonViewModel()
        {
            MainMenu = DataCache.GetMainMenuItems();
            SideMenu = DataCache.GetSideMenuItems();
            AllMenu = MainMenu.Concat(SideMenu).ToList();
            IntranetData = DataCache.GetSectionData("IntranetData");
            PwaSettings = new Pwa()
            {
                IsPwa = false,
                Scope = HttpContext.Current.Request.Url.AbsoluteUri.Replace("http://", "https://"),
                StartUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace("http://", "https://"),
                Root = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, "").Replace("http://", "https://") + HttpContext.Current.Request.ApplicationPath,
                Description = ""
            };
        }

        public Dictionary<string, string> IntranetData { get; set; }
        public List<actionLink> AllMenu { get; private set; }
        public List<actionLink> MainMenu { get; private set; }
        public List<actionLink> SideMenu { get; private set; }
        public string Layout { get; set; }
        public bool IsPopup { get; set; }
        public Pwa PwaSettings { get; set; }
    }

    public class Pwa
    {
        public bool IsPwa { get; set; }
        public string Scope { get; set; }
        public string StartUrl { get; set; }
        public string Root { get; set; }
        public string Description { get; set; }
    }
}
