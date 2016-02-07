using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Security.Credentials;
using Windows.Security.Authentication.Web.Core;

namespace WhatToWearApp
{

    class AuthenticationHelper
    {
        // The Client ID is used by the application to uniquely identify itself to Microsoft Azure Active Directory (AD).
        static string clientId = App.Current.Resources["ida:ClientId"].ToString(); //"a762ff14-641c-4d62-ae2c-f2322d814578"

        // You'll create your tenant-specific authority from the tenant domain and AADInstance URI
        static string tenant = App.Current.Resources["ida:Domain"].ToString();
        static string AADInstance = App.Current.Resources["ida:AADInstance"].ToString();

        //Use the domain-specific authority when you're authenticating users from a single tenant only.
        //static string authority = AADInstance + tenant;

        //Use "organizations" as your authority when you want the app to work on any Azure Tenant.
        //static string authority = "organizations";

        // To authenticate to Microsoft Graph, the client needs to know its App ID URI.
        public const string ResourceUrl = "https://graph.microsoft.com/"; //https://outlook.office.com/

        private static WebAccountProvider aadAccountProvider = null;
        private static WebAccount userAccount = null;

        // Store account-specific settings so that the app can remember that a user has already signed in.
        public static ApplicationDataContainer _settings = ApplicationData.Current.RoamingSettings;

        public static async Task<string> GetAccessToken()
        {
            string token = null;

            //first try to get the token silently
            WebAccountProvider aadAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.net");
            WebTokenRequest webTokenRequest = new WebTokenRequest(aadAccountProvider, String.Empty, clientId, WebTokenRequestPromptType.Default);
            webTokenRequest.Properties.Add("authority", "https://login.windows.net");
            webTokenRequest.Properties.Add("resource", ResourceUrl);
            WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);
            //if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
            //{
            //    WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
            //    userAccount = webTokenResponse.WebAccount;
            //    token = webTokenResponse.Token;
            //}
     //       else if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.UserInteractionRequired)
        //    {
                //get token through prompt
                webTokenRequest = new WebTokenRequest(aadAccountProvider, String.Empty, clientId, WebTokenRequestPromptType.ForceAuthentication);
                webTokenRequest.Properties.Add("authority", "https://login.windows.net");
                webTokenRequest.Properties.Add("resource", ResourceUrl);
                webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
                    token = webTokenResponse.Token;
                    userAccount = webTokenResponse.WebAccount;
                }
         //   }
            if (userAccount != null)
            {
                // save user ID in local storage
                _settings.Values["userID"] = userAccount.Id;
                _settings.Values["userEmail"] = userAccount.UserName;
                _settings.Values["userName"] = userAccount.Properties["DisplayName"];              
            }
            return token;
        }    
        public static void SignOut()
        {
            //Clear stored values from last authentication.
            _settings.Values["userID"] = null;
            _settings.Values["userEmail"] = null;
            _settings.Values["userName"] = null;
        }

        public static string GetAppRedirectURI()
        {
            // Windows 10 universal apps require redirect URI in the format below. Add a breakpoint to this line and run the app before you register it, so that
            // you can supply the correct redirect URI value.
            return string.Format("ms-appx-web://microsoft.aad.brokerplugin/{0}", WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host).ToUpper();
        }
    }
}