using IdentityServer4;
using Harisoft.IDP.Entities;
using Harisoft.IDP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;

namespace Harisoft.IDP
{
    public class Startup
    {
        public static IConfigurationRoot Configuration;


        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var connectionString = Configuration["connectionStrings:marvinUserDBConnectionString"];
            services.AddDbContext<MarvinUserContext>(o => o.UseSqlServer(connectionString));

            services.AddScoped<IMarvinUserRepository, MarvinUserRepository>();

            var identityServerConnectionString = Configuration["connectionStrings:identityServerDataDBConnectionString"];
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;


            services.AddIdentityServer()
                .AddSigningCredential(LoadCertificateFromStore(Configuration["signingCredentialCertificateThumbPrint"]))
                .AddMarvinUserStore()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = (context) => context.UseSqlServer(identityServerConnectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                    });
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = (context) => context.UseSqlServer(identityServerConnectionString, sqlOptions =>
                     {
                         sqlOptions.MigrationsAssembly(migrationsAssembly);
                     });
                });

            services.AddAuthentication().AddFacebook("Facebook", "Facebook", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.AppId = "UserYourOwnAppId";
                options.AppSecret = "UseYourOwnSecret";
            }).AddCookie("idsrv.2FA");
        }

        public X509Certificate2 LoadCertificateFromStore(string thumbPrint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint,
                    thumbPrint, true);
                if (certCollection.Count == 0)
                {
                    throw new Exception("The specified certificate wasn't found. Check the specified thumbprint.");
                }
                return certCollection[0];
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            MarvinUserContext marvinUserContext, ConfigurationDbContext configurationDbContext,
            PersistedGrantDbContext persistedGrantDbContext)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                loggerFactory.AddDebug();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                persistedGrantDbContext.Database.Migrate();

                configurationDbContext.Database.Migrate();
                configurationDbContext.EnsureSeedDataForContext();

                marvinUserContext.Database.Migrate();
                marvinUserContext.EnsureSeedDataForContext();
            }

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
