/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using FluffySpoon.AspNet.LetsEncrypt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodeApp.Areas.Identity;
using NodeApp.Areas.Identity.Data;
using NodeApp.Areas.Identity.Models;
using NodeApp.Blockchain;
using NodeApp.CacheStorageClasses;
using NodeApp.CrossNodeClasses;
using NodeApp.CrossNodeClasses.Services;
using NodeApp.DbBackup;
using NodeApp.ExceptionClasses;
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.LicensorClasses;
using NodeApp.MessengerData.Entities;
using NodeApp.MessengerData.Services;
using NodeApp.MessengerData.Services.Channels;
using NodeApp.MessengerData.Services.Chats;
using NodeApp.MessengerData.Services.Dialogs;
using NodeApp.MessengerData.Services.Messages;
using NodeApp.MessengerData.Services.Users;
using NodeApp.NotificationServices;
using NodeApp.Objects;
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;
using NodeApp.SmsServiceClasses;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.OpenStackSwift;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp
{
    public class Startup
    {
        public Startup()
        {

        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddFilter("Microsoft", LogLevel.Error);
            });
            services.AddSingleton<IViewLocalizer, ViewLocalizer>();
            services.AddMvc()
                .AddViewLocalization(
                    Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix,
                    options => 
                    {
                        options.ResourcesPath = "Resources";
                    })
                .AddDataAnnotationsLocalization();                      
            services.AddCors();
            services.AddSession();
            services.Configure<RequestLocalizationOptions>(options => 
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ru")
                };
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
            if (NodeSettings.IsAppConfigured && NodeSettings.Configs.Certificate != null)
            {
                services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions
                {
                    Email = NodeSettings.Configs.Certificate.Email,
                    UseStaging = false,
                    Domains = new[] { NodeSettings.Configs.Certificate.Domain },
                    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
                    CertificateSigningRequest = new Certes.CsrInfo
                    {
                        CountryName = NodeSettings.Configs.Certificate.CountryName,
                        Locality = NodeSettings.Configs.Certificate.Locality,
                        Organization = NodeSettings.Configs.Certificate.Organization,
                        OrganizationUnit = NodeSettings.Configs.Certificate.OrganizationUnit,
                        State = NodeSettings.Configs.Certificate.State
                    }
                });
                string certsPath = Path.Combine(Directory.GetCurrentDirectory(), "Config", "Certs");
                if (!Directory.Exists(certsPath))
                {
                    Directory.CreateDirectory(certsPath);
                }

                services.AddFluffySpoonLetsEncryptFileCertificatePersistence(Path.Combine(certsPath, "certificate_"));
                services.AddFluffySpoonLetsEncryptFileChallengePersistence(Path.Combine(certsPath, "certificate_"));
                services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
            }
            services.AddSingleton<IFileStorage>(provider =>
            {
                if(NodeSettings.Configs.S3FileStorageOptions  != null)
                {
                    return new S3FileStorage(NodeSettings.Configs.S3FileStorageOptions);
                }
                else if (NodeSettings.Configs.OpenStackOptions != null)
                {
                    return new OpenStackFileStorage(NodeSettings.Configs.OpenStackOptions);
                }                
                else
                {
                    return new LocalFileStorage();
                }
            });
            services.AddDbContext<AdminDbContext>(optBuilder => optBuilder.UseNpgsql(NodeSettings.Configs.AdminDbConnection.ToString()));
            services.AddTransient<UserManager<AdminUser>>();
            services.AddTransient<SignInManager<AdminUser>>();
            services.AddDefaultIdentity<AdminUser>()
                .AddRoles<IdentityRole>()
                .AddDefaultUI(UIFramework.Bootstrap4)
                .AddEntityFrameworkStores<AdminDbContext>()
                .AddDefaultTokenProviders(); 
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<IBlockchainDataRestorer, BlockchainDataRestorer>();
            services.AddSingleton<IDbContextFactory<MessengerDbContext>, MessengerDbContextFactory>();
            services.AddSingleton<IConnectionsService, ConnectionsService>();
            services.AddSingleton<INodeNoticeService, NodeNoticeService>();
            services.AddSingleton<IConversationsNoticeService, ConversationsNoticeService>();
            services.AddSingleton<IPushNotificationsService, PushNotificationsService>();
            services.AddSingleton<INoticeService, NoticeService>();
            services.AddSingleton(provider => NodeSettings.Configs.SmsServiceConfiguration);
            services.AddSingleton<ISmsService>(provider => 
            {
                var configuration = provider.GetService<SmsServiceConfiguration>();
                if (configuration == null)
                    return null;
                switch (configuration.ServiceName)
                {
                    case SmsServiceTypes.BSG:
                        {
                            return new BSGSmsService(configuration);
                        }
                    case SmsServiceTypes.SMSIntel:
                        {
                            return new SMSIntelSmsService(configuration);
                        }
                    case SmsServiceTypes.SMSRU:
                        {
                            return new SMSRUSmsService(configuration);
                        }
                    case SmsServiceTypes.GolosAloha:
                        {
                            return new GolosAlohaSmsService(configuration);
                        }
                    default:
                        return null;
                        
                }
            });
            services.AddTransient<ILoadMessagesService, LoadMessagesService>();
            services.AddTransient<ICreateMessagesService, CreateMessagesService>();
            services.AddTransient<IDeleteMessagesService, DeleteMessagesService>();
            services.AddTransient<IUpdateMessagesService, UpdateMessagesService>();
            services.AddTransient<IAttachmentsService, AttachmentsService>();
            services.AddTransient<ICreateChatsService, CreateChatsService>();
            services.AddTransient<ILoadChatsService, LoadChatsService>();
            services.AddTransient<IUpdateChatsService, UpdateChatsService>();
            services.AddTransient<IDeleteChatsService, DeleteChatsService>();
            services.AddTransient<IChangeNodeOperationsService, ChangeNodeOperationsService>();
            services.AddTransient<ICreateChannelsService, CreateChannelsService>();
            services.AddTransient<ILoadChannelsService, LoadChannelsService>();
            services.AddTransient<IUpdateChannelsService, UpdateChannelsService>();
            services.AddTransient<IDeleteChannelsService, DeleteChannelsService>();
            services.AddTransient<ILoadDialogsService, LoadDialogsService>();
            services.AddTransient<IDeleteDialogsService, DeleteDialogsService>();
            services.AddTransient<ICreateUsersService, CreateUsersService>();
            services.AddTransient<ILoadUsersService, LoadUsersService>();
            services.AddTransient<IUpdateUsersService, UpdateUsersService>();
            services.AddTransient<IDeleteUsersService, DeleteUsersService>();
            services.AddTransient<IContactsService, ContactsService>();
            services.AddTransient<IConversationsService, ConversationsService>();
            services.AddTransient<IFavoritesService, FavoritesService>();
            services.AddTransient<IFilesService, FilesService>();
            services.AddTransient<IGroupsService, GroupsService>();
            services.AddTransient<IKeysService, KeysService>();
            services.AddTransient<INodesService, NodesService>();
            services.AddTransient<IPendingMessagesService, PendingMessagesService>();
            services.AddTransient<IPollsService, PollsService>();
            services.AddTransient<IPoolsService, PoolsService>();
            services.AddTransient<IQRCodesService, QRCodesService>();
            services.AddTransient<ITokensService, TokensService>();
            services.AddTransient<INodeRequestSender, NodeRequestSender>();
            services.AddTransient<IPrivacyService, PrivacyService>();
            services.AddTransient<ICrossNodeService, CrossNodeService>();
            services.AddTransient<ISystemMessagesService, SystemMessagesService>();
            BlockSegmentsService.Instance = new BlockSegmentsService(new AppBlockSegmentsService());
            BlockGenerationHelper.Instance = new BlockGenerationHelper(new AppBlockGenerationHelper());
            services.AddSingleton<ICacheRepository<VerificationCodeInfo>, RedisUserVerificationCodesRepository>(
                provider => new RedisUserVerificationCodesRepository(NodeSettings.Configs.CacheServerConnection));
            services.AddSingleton<IVerificationCodesService, VerificationCodesService>();
            services.AddSingleton<IAppServiceProvider, AppServiceProvider>(provider => new AppServiceProvider(provider));
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime)
        {
            AppServiceProvider.Instance = (AppServiceProvider)app.ApplicationServices.GetService<IAppServiceProvider>();
            NodeData.NodeNoticeService = app.ApplicationServices.GetService<INodeNoticeService>();
            NodeData.ConnectionsService = app.ApplicationServices.GetService<IConnectionsService>();
            NodeConnector.ConnectionsService = app.ApplicationServices.GetService<IConnectionsService>();
           
            app.UseRequestLocalization(); 
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseMiddleware<IgnoreRouteMiddleware>();
            app.UseMiddleware<AdminSignOutMiddleware>();
            app.UseStaticFiles();
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            app.UseCors(builder =>
            {
                builder.AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowAnyOrigin();
            });
            app.UseWebSockets();
            if (NodeSettings.IsAppConfigured)
            {
                app.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware();
            }

            if (NodeSettings.IsAppConfigured
                && (NodeSettings.Configs.Certificate.Location != null
                && File.Exists(NodeSettings.Configs.Certificate.Location)
                || LetsEncryptRenewalService.Certificate != null))
            {
                app.UseHttpsRedirection();
            }
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                "Default",
                "{controller}/{action}",
                new
                {
                    controller = "WebSocket",
                    action = "Index"
                });
            });
            if (NodeSettings.IsAppConfigured)
            {
                try
                {
                    AppServiceProvider.Instance.NodesService.CreateOrUpdateNodeInformationAsync(NodeSettings.Configs.Node);
                    LicensorClient.Instance.AuthAsync().Wait(5000);
                    NodeSettings.Configs.License = LicensorClient.Instance.GetLicenseAsync().Result;
                    TasksHelper.StartUpdateLicenseTask();
                    TasksHelper.StartUpdateSessionKeyTask();
                    try
                    {
                        NodeSettings.Configs.Node = LicensorClient.Instance.EditNodeAsync(NodeSettings.Configs.Node).Result;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"An error ocurred while updating node information: {ex.Message}");
                    }
                    File.WriteAllText("Config/appsettings.json", ObjectSerializer.ObjectToJson(NodeSettings.Configs, true));
                    app.ApplicationServices.GetService<IPoolsService>().CheckNodePoolsAsync().Wait();
                    DbBackupHelper.StartDbBackupOperationAsync(NodeSettings.Configs.MessengerDbConnection);
                    InitializeNodeConnections();
                    TasksHelper.StartNodeConnectionsCheckTask();
                    DockerCleanHepler.Clear();
                    BlockchainSynchronizationService synchronizationService = new BlockchainSynchronizationService();
                    synchronizationService.CheckAndSyncBlockchainAsync().Wait();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }

        private class IgnoreRouteMiddleware
        {
            private readonly RequestDelegate requestDelegate;
            public IgnoreRouteMiddleware(RequestDelegate requestDelegate)
            {
                this.requestDelegate = requestDelegate;
            }

            public async Task Invoke(HttpContext context)
            {
                if (!NodeSettings.Configs.NodesUrls?.Any(opt => opt.Contains(context.Request.Host.Host)) ?? false)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
                if (context.Request.Path.HasValue && context.Request.Path.Value.Contains("index.html"))
                {
                    if (context.Request.Host.Port == NodeSettings.DASHBOARD_PORT)
                    {
                        context.Response.StatusCode = 403;
                        return;
                    }
                }
                await requestDelegate.Invoke(context).ConfigureAwait(false);
            }
        }

        private class AdminSignOutMiddleware
        {
            private readonly RequestDelegate requestDelegate;
            public AdminSignOutMiddleware(RequestDelegate requestDelegate)
            {
                this.requestDelegate = requestDelegate;
            }
            public async Task Invoke(HttpContext httpContext, UserManager<AdminUser> userManager, SignInManager<AdminUser> signInManager)
            {
                if (!string.IsNullOrEmpty(httpContext.User.Identity.Name))
                {
                    var userId = signInManager.UserManager.GetUserId(httpContext.User);
                    var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
                    if (user != null && user.Banned)
                    {
                        await signInManager.SignOutAsync().ConfigureAwait(false);
                        httpContext.Response.Redirect("/");
                    }
                }
                await requestDelegate(httpContext).ConfigureAwait(false);
            }
        }

        public void OnShutdown()
        {
            Console.WriteLine("Application exits.");
            BlockGenerationHelper.Instance.StopBlockGenerationAsync().Wait();
        }

        private static async void InitializeNodeConnections()
        {
            try
            {
                List<KeyValuePair<string, NodeVm>> connectionUrls = new List<KeyValuePair<string, NodeVm>>();
                try
                {
                    var licensorNodes = await LicensorClient.Instance.GetNodesAsync(null, null, null).ConfigureAwait(false);
                    if (licensorNodes.Any())
                    {
                        connectionUrls.AddRange(licensorNodes
                            .Where(opt => opt.Id != NodeSettings.Configs.Node.Id)
                            .Select(opt => new KeyValuePair<string, NodeVm>($"{opt.Domains.FirstOrDefault()}:{opt.NodesPort}", opt)));
                    }
                }
                catch (ResponseException ex)
                {
                    Logger.WriteLog(ex);
                }
                if (connectionUrls.Any())
                {
                    NodeConnector nodeConnector = new NodeConnector(connectionUrls.Distinct().ToList());
                    IEnumerable<NodeConnection> nodeList = await nodeConnector.ConnectToNodesAsync().ConfigureAwait(false);
                    nodeConnector.Listen(nodeList);
                    AppServiceProvider.Instance.NodeRequestSender.SendConnectRequestAsync(nodeList.ToList());
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}