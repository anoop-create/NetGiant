using netGiant.Intranet.BusinessLayer;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Shared
{
    public class MiscController : ApplicationController
    {
        [HttpPost]
        public JsonResult Popup(string popupname, string popupid, string popupwidth = "md", string replacements = "")
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            if (string.IsNullOrEmpty(popupname))
            {
                return Json(new
                {
                    savereturn = sr
                });
            }

            popupwidth = popupwidth == "" ? "md" : popupwidth;
            Dictionary<string, string> dict = DataCache.GetSectionData("PopupData");
            string html = "";
            if (dict.ContainsKey(popupname))
            {
                html = dict[popupname].ToString();
                sr.IsSuccess = true;
            }

            html = SharedFunctions.DoReplacements(html, replacements);

            // Create Modal
            sr.ReturnData = @"<section class=""modal fade"" id=""" + popupid +
                      @""" tabindex=""-1\"" role=""dialog"" aria-labelledby=""myModalLabel"">
                        <div class=""modal-dialog modal-" + popupwidth + @""" role=""document"">
                            <div class=""modal-content"">" + html + @"</div>
                        </div>
                    </section>";


            return Json(new
            {
                savereturn = sr
            });
        }

        [AllowAnonymous]
        public ActionResult GetManifest(string title)
        {
            return Content("{" +
        "\"name\": \"NetGiant " + title + "\"," +
        "\"short_name\": \"NetGiant " + title + "\"," +
        "\"start_url\": \"" + Request.UrlReferrer.AbsolutePath.Substring(1) + "\"," +
        "\"scope\": \"" + Request.UrlReferrer.AbsolutePath.Substring(1) + "\"," +
        "\"display\": \"standalone\"," +
        "\"background_color\": \"#ffffff\"," +
        "\"theme_color\": \"#000000\"," +
        "\"description\": \"NetGiant " + title + "\"," +
        "\"icons\": [{" +
                "\"src\": \"Content/images/favicon-192.png\"," +
                "\"sizes\": \"192x192\"," +
                "\"type\": \"image/png\"" +
            "},{" +
                "\"src\": \"Content/images/favicon-512.png\"," +
                "\"sizes\": \"512x512\"," +
                "\"type\": \"image/png\"" +
        "}]}",
        "application/json");
        }
    }
}