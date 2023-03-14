using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Middleware;
using Sms.Web.Service;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Sms.Web.BackgroundTask;
using Sms.Web.Middleware.Filters;
using Microsoft.AspNetCore.Diagnostics;
using System;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Logging;

namespace Sms.Web
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options =>
                {
                    options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
            services.AddHostedService<OrderProcessing>();
            services.AddHostedService<OrderProposedPhoneNumberProcessing>();
            services.AddHostedService<OrderExpiredProcessing>();
            services.AddHostedService<OrderPreloadCacheProcessing>();
            services.AddHostedService<OrderProposedProcessing>();
            services.AddHostedService<DiscountProcessing>();
            services.AddHostedService<SystemHealthCheckProcessing>();
            // services.AddHostedService<AlertProcessing>();
            services.AddHostedService<ReferFeeProcessing>();
            services.AddHostedService<Bank512Processing>();
            services.AddHostedService<AvailableServiceProcessing>();
            services.AddHostedService<UserTokensProcessing>();
            services.AddHostedService<UserBalanceSnapshotProcessing>();
            services.AddHostedService<PhoneEfficiencyProcessing>();
            services.AddHostedService<ServiceProviderPhoneNumberLiveCheckProcessing>();

            services.AddHostedService<InternationalOrderFindPhoneNumberProcessing>();
            services.AddHostedService<InternationalOrderConfirmPhoneNumberProcessing>();
            services.AddHostedService<InternationalOrderExpiredProcessing>();
            services.AddHostedService<InternationalOrderProposedProcessing>();

            services.AddHostedService<ArchiveDataProcessing>();
         

            var connection = Configuration.GetConnectionString("SmsDatabase");
            services.AddDbContext<SmsDataContext>
                (options => options.UseSqlServer(connection));

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.JwtSecret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddScoped<DevelopmentOnly>();
            services.AddScoped<PortalAuthorize>();
            // Inject current UserId
            services.AddScoped<IAuthService, AuthService>();
            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGsmDeviceService, GsmDeviceService>();
            services.AddScoped<IComService, ComService>();
            services.AddScoped<IComHistoryService, ComHistoryService>();
            services.AddScoped<ISmsHistoryService, SmsHistoryService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IUserTokenService, UserTokenService>();
            services.AddScoped<IServiceProviderService, ServiceProviderService>();
            services.AddScoped<INewServiceSuggestionService, NewServiceSuggestionService>();
            services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IUserTransactionService, UserTransactionService>();
            services.AddScoped<IOrderResultReportService, OrderResultReportService>();
            services.AddScoped<IStatisticService, StatisticService>();
            services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
            services.AddScoped<IOrderComplaintService, OrderComplaintService>();
            services.AddScoped<IOrderResultService, OrderResultService>();
            services.AddScoped<IOrderStatusService, OrderStatusService>();
            services.AddScoped<IPhoneFailedCountService, PhoneFailedCountService>();
            services.AddScoped<ISmsService, SmsService>();
            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IProviderHistoryService, ProviderHistoryService>();
            services.AddScoped<IMomoPaymentService, MomoPaymentService>();
            services.AddScoped<INganLuongPaymentService, NganLuongPaymentService>();
            services.AddScoped<IPerfectMoneyPaymentService, PerfectMoneyPaymentService>();
            services.AddScoped<IUserPaymentTransactionService, UserPaymentTransactionService>();
            services.AddScoped<IPaymentMethodConfigurationService, PaymentMethodConfigurationService>();
            services.AddScoped<IUserOfflinePaymentReceiptService, UserOfflinePaymentReceiptService>();
            services.AddScoped<IOfflinePaymentNoticeService, OfflinePaymentNoticeService>();
            services.AddScoped<IErrorPhoneLogService, ErrorPhoneLogService>();
            services.AddScoped<IOrderExportJobService, OrderExportJobService>();
            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IClientTimezoneService, ClientTimezoneService>();
            services.AddScoped<ICheckoutRequestService, CheckoutRequestService>();
            //services.Configure<RecaptchaSettings>(Configuration.GetSection("RecaptchaSettings"));
            services.AddScoped<IRecaptcharService, RecaptcharService>();
            services.AddScoped<IDiscountService, DiscountService>();
            services.AddScoped<ISystemAlertService, SystemAlertService>();
            services.AddScoped<ISystemHealthCheckService, SystemHealthCheckService>();
            services.AddScoped<IReferalService, ReferalService>();
            services.AddScoped<IUserReferalFeeService, UserReferalFeeService>();
            services.AddScoped<IUserReferredFeeService, UserReferredFeeService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IPortalService, PortalService>();
            services.AddScoped<ISimCountryService, SimCountryService>();
            services.AddScoped<IInternationalSimService, InternationalSimService>();
            services.AddScoped<IInternationalSimOrderService, InternationalSimOrderService>();
            services.AddScoped<IServiceProviderPhoneNumberLiveCheckService, ServiceProviderPhoneNumberLiveCheckService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IFileService, FileService>();
            services.AddSingleton<IPreloadOrderServiceProviderQueue, PreloadOrderServiceProviderQueue>();
            services.AddSingleton<IPreloadOrderSimCountryQueue, PreloadOrderSimCountryQueue>();
            // portal connectors
            services.AddScoped<IPortalTkaoConnector, PortalTkaoConnector>();
            services.AddSingleton<AsyncLocker, AsyncLocker>();

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = Configuration.GetConnectionString("RedisCache");
                options.InstanceName = "Rentcode";
            });

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Rental Sms API",
                    Description = "This is API description for Rental Sms API",
                    TermsOfService = new Uri("https://rentcode.co"),
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
            {
              new OpenApiSecurityScheme
              {
                Reference = new OpenApiReference
                {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
              },
              new List<string>()
            }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                //app.UseHsts();
                //app.UseHttpsRedirection();
            }
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMiddleware<UserTokenChecking>();
            //app.UseMiddleware<RequestLogging>();

            app.UseCors("AllowOrigin");
            app.Use(async (httpContext, next) =>
            {
                var corsHeaders = new HeaderDictionary();
                foreach (var pair in httpContext.Response.Headers)
                {
                    if (!pair.Key.StartsWith("access-control-", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                    corsHeaders[pair.Key] = pair.Value;
                }

                httpContext.Response.OnStarting(o =>
            {
                var ctx = (HttpContext)o;
                var headers = ctx.Response.Headers;
                foreach (var pair in corsHeaders)
                {
                    if (headers.ContainsKey(pair.Key)) { continue; }
                    headers.Add(pair.Key, pair.Value);
                }
                return Task.CompletedTask;
            }, httpContext);

                await next();
            });
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.EnableDeepLinking();
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
