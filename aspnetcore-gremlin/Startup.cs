using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using aspnetcore_gremlin.Services;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace aspnetcore_gremlin
{
    public class Startup
    {
        private const string GremlinAccountEndpointConnectionStringKey = "AccountEndpoint";
        private const string GremlinPasswordConnectionStringKey = "AccountKey";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            CosmosDbService sev = InitializeCosmosClientInstance();
            services.AddSingleton<ICosmosDbService>(sev);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=MyItem}/{action=Index}/{id?}");
            });
        }

        private static CosmosDbService InitializeCosmosClientInstance()
        {

            string subscriptionId = "937bc588-a144-4083-8612-5f9ffbbddb14";
            string resourceGroupName = "servicelinker-test-win-group";
            string accountName = "servicelinker-gremlin-cosmos";
            string databaseName = "coreDB";
            string graphName = "MyItem";

            string resourceEndpoint = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_RESOURCEENDPOINT");
            string scope = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_SCOPE");
            string clientId = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_CLIENTID");

            string accessToken = GetAccessTokenByMsIdentity(scope, clientId);

            string endpoint = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/listConnectionStrings?api-version=2019-12-12";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage result = httpClient.PostAsync(endpoint, new StringContent("")).Result;
            DatabaseAccountListConnectionStringsResult connStrResult = result.Content.ReadAsAsync<DatabaseAccountListConnectionStringsResult>().Result;

            string connectionString = null;
            foreach (DatabaseAccountConnectionString connStr in connStrResult.ConnectionStrings)
            {
                if (connStr.Description.Contains("Primary") && connStr.Description.Contains("Gremlin"))
                {
                    connectionString = connStr.ConnectionString;
                }
            }

            IDictionary<string, string> connStrDict = ParseConnectionString(connectionString);
            string dbEndpoint = connStrDict[GremlinAccountEndpointConnectionStringKey];
            string hostname = ParseGremlinHostNameFromEndpoint(dbEndpoint);
            int port = int.Parse(ParseGremlinPortFromEndpoint(dbEndpoint));
            string username = $"/dbs/{databaseName}/colls/{graphName}";
            string password = connStrDict[GremlinPasswordConnectionStringKey];

            CosmosDbService cosmosDbService = new CosmosDbService(
                hostname,
                port,
                username,
                password);
            return cosmosDbService;
        }

        private static string GetAccessTokenByMsIdentity(string scope, string clientId)
        {
            ManagedIdentityCredential cred = new ManagedIdentityCredential(clientId);
            TokenRequestContext reqContext = new TokenRequestContext(new string[] { scope });
            AccessToken token = cred.GetTokenAsync(reqContext).Result;
            return token.Token;
        }

        private static IDictionary<string, string> ParseConnectionString(string connectionString)
        {
            // connection string is in format: HostName={hostname};Username={username};Password={password};Port={port}
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string seg in connectionString.Split(";"))
            {
                int index = seg.IndexOf("=");
                if (index < 0)
                {
                    continue;
                }
                string key = seg.Substring(0, index);
                string value = seg.Substring(index + 1);
                dict.Add(key, value);
            }
            return dict;
        }

        private static string ParseGremlinHostNameFromEndpoint(string endpoint)
        {
            // endpoint is like: https://servicelinker-gremlin-cosmos.documents.azure.com:443/
            string tempString = endpoint;
            int colonIndex = tempString.LastIndexOf(":");
            tempString = tempString.Substring(0, colonIndex);
            int slashIndex = tempString.LastIndexOf("/");
            tempString = tempString.Substring(slashIndex + 1);
            string[] segs = tempString.Split(".");
            string hostname = "";
            foreach (string seg in segs)
            {
                // Cosmos host name is like servicelinker-gremlin-cosmos.documents.azure.com
                // Gremlin host name is like servicelinker-gremlin-cosmos.gremlin.cosmos.azure.com
                // Change documents to gremlin.cosmos
                if (string.Equals(seg, "documents"))
                {
                    hostname += "gremlin.cosmos.";
                }
                else
                {
                    hostname += seg + ".";
                }
            }
            return hostname.Remove(hostname.Length - 1);
        }

        private static string ParseGremlinPortFromEndpoint(string endpoint)
        {
            // endpoint is like: https://servicelinker-gremlin-cosmos.documents.azure.com:443/
            int index = endpoint.LastIndexOf(":");
            string port = endpoint.Substring(index + 1);
            while (port.EndsWith("/"))
            {
                port = port.Remove(port.Length - 1);
            }
            return port;
        }
    }
}
