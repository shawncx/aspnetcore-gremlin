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
            string scope = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_SCOPE");
            string clientId = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_CLIENTID");
            string username = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_USERNAME");
            string listKeyUrl = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_LISTKEYURL");

            string accessToken = GetAccessTokenByMsIdentity(scope, clientId);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage result = httpClient.PostAsync(listKeyUrl, new StringContent("")).Result;
            DatabaseAccountListKeysResult connStrResult = result.Content.ReadAsAsync<DatabaseAccountListKeysResult>().Result;

            string password = connStrResult.PrimaryMasterKey;
            string hostname = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_HOSTNAME");
            int port = int.Parse(Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPUSERASSIGNEDIDENTITYCONNECTIONSUCCEEDED_PORT"));

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

    }
}
