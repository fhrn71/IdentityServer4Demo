using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ImageGallery.Client.Services;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace ImageGallery.Client
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddAuthorization((options) => {
                options.AddPolicy("CanOrderFrame", policybuilder =>
                {
                    policybuilder.RequireAuthenticatedUser();
                    policybuilder.RequireClaim("subscriptionlevel", "PayingUser");
                    policybuilder.RequireClaim("country", "be");
                });
            });

            // register an IHttpContextAccessor so we can access the current
            // HttpContext in services by injecting it
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register an IImageGalleryHttpClient
            services.AddScoped<IImageGalleryHttpClient, ImageGalleryHttpClient>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies", (options) => {
                options.AccessDeniedPath = "/Authorization/AccessDenied";
            })
            .AddOpenIdConnect("oidc", (options) => {
                options.SignInScheme = "Cookies";
                options.Authority = "https://localhost:44373";
                options.RequireHttpsMetadata = true;
                options.ClientId = "imagegalleryclient";
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("address");
                options.Scope.Add("roles");
                options.Scope.Add("courses");
                options.Scope.Add("imagegalleryapi");
                options.Scope.Add("subscriptionlevel");
                options.Scope.Add("country");

                options.ResponseType = "code id_token";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.ClientSecret = "ItsMySecret";
                options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
                {
                    OnTicketReceived = ticketReceivedContext =>
                    {
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = tokenValidatedContext =>
                    {
                        var identity = tokenValidatedContext.Principal.Identity
                            as ClaimsIdentity;

                        var targetClaims = identity.Claims.Where(z => new[] { "role", "sub" }.Contains(z.Type));

                        var newClaimsIdentity = new ClaimsIdentity(
                          targetClaims,
                          ClaimTypes.Authentication,
                          "given_name",
                          "role");

                        tokenValidatedContext.Principal =
                            new ClaimsPrincipal(newClaimsIdentity);

                        return Task.CompletedTask;
                    },

                    OnUserInformationReceived = userInformationReceivedContext =>
                    {
                        userInformationReceivedContext.User.Remove("address");
                        return Task.FromResult(0);
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultInboundClaimFilter.Clear();
            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }         
    }
}
