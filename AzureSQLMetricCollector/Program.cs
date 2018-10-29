using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading;
using Newtonsoft.Json;

namespace AzureSQLMetricCollector
{
    class Program
    {
        private static string aadInstance = ConfigurationManager.AppSettings["AAD:Instance"];
        private static string aadTenant = ConfigurationManager.AppSettings["AAD:Tenant"];
        private static string aadClientId = ConfigurationManager.AppSettings["AAD:ClientId"];
        private static string aadAppKey = ConfigurationManager.AppSettings["AAD:AppKey"];

        private static string dtuUrlFormat = ConfigurationManager.AppSettings["DTU:UrlFormat"];
        private static string dtuSubscriptionId = ConfigurationManager.AppSettings["DTU:SubscriptionId"];
        private static string dtuResourceGroupName = ConfigurationManager.AppSettings["DTU:ResourceGroupName"];
        private static string dtuSQLServerName = ConfigurationManager.AppSettings["DTU:SQLServerName"];
        private static string dtuSQLDatabaseName = ConfigurationManager.AppSettings["DTU:SQLDatabaseName"];

        private static string tokenResourceId = ConfigurationManager.AppSettings["Token:ResourceId"];

        private static HttpClient httpClient = new HttpClient();
        private static AuthenticationContext authContext = null;
        private static ClientCredential clientCredential = null;

        static void Main(string[] args)
        {
            GetDTUMetric().Wait();
            Console.Read();
        }

        private static async Task GetDTUMetric()
        {
            var tokenIssuingAuthority = string.Format(aadInstance, aadTenant);
            var dtuUrl = string.Format(dtuUrlFormat, dtuSubscriptionId, dtuResourceGroupName, dtuSQLServerName, dtuSQLDatabaseName);

            authContext = new AuthenticationContext(tokenIssuingAuthority);
            clientCredential = new ClientCredential(aadClientId, aadAppKey);

            var bearerToken = await GetBearerToken();

            Console.WriteLine($"Bearer {bearerToken}");

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            var response = await httpClient.GetAsync(dtuUrl);

            var dtuResultData = await response.Content.ReadAsAsync<MetricResult>();

            Console.WriteLine($"DTU at: {dtuResultData.DTU_Percent.TimeStamp} is {GetMetric(dtuResultData.DTU_Percent)}");
        }

        static double GetMetric(TimeStampData data)
        {
            if (data.Maximum > -1)
            {
                return data.Maximum;
            }
            if (data.Average > -1)
            {
                return data.Average;
            }
            if (data.Minimum > -1)
            {
                return data.Minimum;
            }
            if (data.Total > -1)
            {
                return data.Total;
            }
            if (data.Count > -1)
            {
                return data.Count;
            }
            if (data.None > -1)
            {
                return data.None;
            }

            return data.Average;
        }

        static async Task<string> GetBearerToken()
        {
            AuthenticationResult result = null;
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    result = await authContext.AcquireTokenAsync(tokenResourceId, clientCredential);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    Console.WriteLine(
                        String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString()));
                }

            } while ((retry == true) && (retryCount < 3));

            if (result == null)
            {
                return string.Empty;
            }
            else
            {
                return result.AccessToken;
            }
        }
    }
}
