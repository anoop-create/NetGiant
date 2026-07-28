using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Payments;
using PayPalHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using netGiant.Intranet.BusinessLayer.Utilities;

namespace netGiant.Intranet.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            return View();
        }

        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public ActionResult PayPal()
        {
            return View();
        }

        //public async Task<ActionResult> PayPalAuthorise(string trans)
        //{
        //    SaveReturn sr = new SaveReturn();

        //    //var result = await GetOrder(orderid, true);

        //    return View();
        //}

        [HttpPost]
        public async Task<JsonResult> PayPalCapture(string trans)
        //public static SaveReturn PayPalCapture(string trans)
        {
            SaveReturn sr = new SaveReturn();
            JObject transaction = JsonConvert.DeserializeObject<JObject>(trans);

            // Check for failed transaction
            sr.IsSuccess = true;
            if (transaction["status"].ToString() != "COMPLETED")
            {
                sr.IsSuccess = false;
                sr.Message = "PayPal transaction failed";
                return Json(sr);
            }

            string paypalAuthId = transaction["purchase_units"].First()["payments"]["authorizations"].First()["id"].ToString();
            //WritePayPalLog("AUTHORIZE", paypalAuthId, trans);

            string paypalRecipientName = transaction["purchase_units"].First()["shipping"]["name"]["full_name"].ToString();
            string paymentTitle = transaction["payer"]["name"]["title"]?.ToString() ?? "Mr";
            string paymentFirstname = transaction["payer"]["name"]["given_name"].ToString();
            string paymentSurname = transaction["payer"]["name"]["surname"].ToString();
            string telno = transaction["payer"]["phone"]?["phone_number"]["national_number"].ToString() ?? "0";
            string email = transaction["payer"]["email_address"].ToString();


            // Capture the paypal authorised payment
            Capture result = await CaptureAuthorisation(paypalAuthId, false);
            if (result.Status != "COMPLETED")
            {
                sr.IsSuccess = false;
                sr.Message = "Failed";
            }

            return Json(sr);
        }

        //public async Task<ActionResult> PayPalGetOrder(string orderid)
        //{
        //    SaveReturn sr = new SaveReturn();

        //    var result = await GetOrder(orderid, true);

        //    return View(result);
        //}

        //----------------------------------------

        private static PayPalEnvironment environment()
        {
            // Beta TG
            return new SandboxEnvironment(
                "AXMhgyTTWykWuNUlM0S-QXukKpSbNTH_DdAf6iizSMnk_2jewfBeuSOJX6PQnrdxeUQrNLJB4dGOj91C",
                "EKOshcm9hMe9nrCdyP11vjYIupLqhjjoFAdguRllPWFSOS_GZ2pCh8g_u09KcBCaEt_vB3OkIKvRz_na");

            // Live NG
            //return new PayPalEnvironment(
            //    "AYtakgVcKF1qU-B4950VZSP9sVhKUJICebdjOS9EIYx7c1EIrQ2K495R9zaNE4mGg1m2AlTOkR3b4Ii4",
            //    "EOhs07bbymvV25EXX3AvWhgT2KUxB4y_Fv8qAxknQaDszupI5S49lPuQymMs4tG8yXf46rvUnlKUrlwF",
            //    //"", "");
            //    "https://api-m.paypal.com",
            //    "https://www.paypal.com");

            // Live CM
            //return new PayPalEnvironment(
            //    "AQTclui8qPICj2pvaI4kGJQXseQaphMb9Ztt9HJzkHDoCM1MzZ0qJhK58up93ZAqJA1cOHaVG0R9jTau",
            //    "EN0hjzpnqtKbLx538mDNAC6HxQxt1h_VdOSQoZTVEGKLXIo6iyDNeJ53bAdfPzVf-o7bcdNWWDXSSWIL",
            //    //"", "");
            //    "https://api-m.paypal.com",
            //    "https://www.paypal.com");

            // Live TG
            //return new PayPalEnvironment(
            //    "AS9nuEYJAWWf6c4QzOWVlTo-IO8IjT-0BGfVO6VmbaiDMYN9vIkfD35iY_vIJc6Ayl5MnHoRqtKMIGmm",
            //    "EA9x65xvAx1fuHpVlEVKmAh_hUpZyJ8zLrP4vWm5K0G4M6CJVQZMdN7aWxzr0k4I9agSuQR8GTlWKh0c",
            //    //"", "");
            //    "https://api-m.paypal.com",
            //    "https://www.paypal.com");
        }

        /**
            Returns PayPalHttpClient instance to invoke PayPal APIs.
         */
        private static HttpClient client()
        {
            return new PayPalHttpClient(environment());
        }

        private static HttpClient client(string refreshToken)
        {
            return new PayPalHttpClient(environment(), refreshToken);
        }

        /**
            Use this method to serialize Object to a JSON string.
        */

        //private async Task<PayPalCheckoutSdk.Orders.Order> GetOrder(string orderId, bool debug = false)
        //{
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //    OrdersGetRequest request = new OrdersGetRequest(orderId);
        //    var response = await client().Execute(request);
        //    var result = response.Result<Order>();

        //    return result;
        //}

        private static async Task<Capture> CaptureAuthorisation(string authId, bool debug = false)
        {
            OtherUtilities.SetTlsVersion();
            Capture result = new Capture();

            try
            {
                AuthorizationsCaptureRequest request = new AuthorizationsCaptureRequest(authId);

                request.Prefer("return=representation");
                request.RequestBody(new CaptureRequest());

                var response = await client().Execute(request);
                result = response.Result<Capture>();
            }
            catch (Exception e)
            {
                // Exception
            }

            return result;
        }
    }
}