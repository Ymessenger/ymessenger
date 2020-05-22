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
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using NodeApp.Areas.Identity.Data;
using NodeApp.Extensions;
using NodeApp.Helpers;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Blockchain.Entities;
using ObjectsLibrary.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NodeApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Encryptor.SetEncryptIterationsLimit();
            await StartApplicationAsync(args).ConfigureAwait(false);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var nodeUrls = new List<string>();
            if (NodeSettings.Configs == null || NodeSettings.Configs.NodesUrls == null)
            {
                nodeUrls.Add("http://*:80");
            }
            foreach (var url in NodeSettings.Configs.NodesUrls)
            {
                nodeUrls.Add($"https://{url}:{NodeSettings.Configs.Node.ClientsPort}/");
                nodeUrls.Add($"https://{url}:{NodeSettings.Configs.Node.NodesPort}/");
                nodeUrls.Add($"https://{url}:{NodeSettings.DASHBOARD_PORT}");
                nodeUrls.Add($"https://{url}:443/");
                nodeUrls.Add($"http://{url}:80/");
            }
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel()
                .UseUrls(nodeUrls.ToArray())
                .UseWebRoot("wwwroot")
                .ConfigureKestrel(opt =>
                {
                    opt.Limits.MaxRequestBodySize = NodeSettings.MAX_HTTP_REQUEST_SIZE;
                    opt.Limits.MinRequestBodyDataRate =
                    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    opt.Limits.MinResponseDataRate =
                    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    opt.ConfigureHttpsDefaults(options =>
                    {
                        options.ServerCertificateSelector = (context, cert) =>
                        {
                            if (NodeSettings.Configs.Certificate.Location != null
                        && File.Exists(NodeSettings.Configs.Certificate.Location))
                            {
                                return new X509Certificate2(
                            NodeSettings.Configs.Certificate.Location,
                            NodeSettings.Configs.Certificate.Password);
                            }
                            else
                            {
                                return LetsEncryptRenewalService.Certificate;
                            }
                        };
                    });
                });
        }

        private static async Task InitNode()
        {
            bool blockchainDbAvailable = true;
            bool messengerDbAvailable = true;
            try
            {
                Logger.Init("Logs");
                NodeSettings.LoadSettingsFromFile();
                DbConnectionChecker dbConnectionChecker = new DbConnectionChecker();
                blockchainDbAvailable = await dbConnectionChecker.CheckDbConnectionAsync(NodeSettings.Configs.BlockchainDbConnection.ToString());
                if (!blockchainDbAvailable)
                {
                    Console.WriteLine(dbConnectionChecker.Errors?.ToJson());                    
                }
                messengerDbAvailable = await dbConnectionChecker.CheckDbConnectionAsync(NodeSettings.Configs.MessengerDbConnection.ToString());
                if (!messengerDbAvailable)
                {
                    Console.WriteLine(dbConnectionChecker.Errors?.ToJson());
                }
                await ApplyMigrationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            if(!blockchainDbAvailable || !messengerDbAvailable)
            {                
                Console.WriteLine("Cannot connect to one or more databases.");
            }
        }

        private static async Task ApplyMigrationsAsync()
        {
            try
            {
                DbContextOptionsBuilder<MessengerDbContext> optionsBuilder = new DbContextOptionsBuilder<MessengerDbContext>();
                optionsBuilder.UseNpgsql(NodeSettings.Configs.MessengerDbConnection.ToString());
                using (MessengerDbContext messengerContext = new MessengerDbContext(optionsBuilder.Options))
                {
                    await messengerContext.Database.MigrateAsync().ConfigureAwait(false);
                }
                BlockchainDbContext.InitConnectionString(NodeSettings.Configs.BlockchainDbConnection.ToString());
                using (BlockchainDbContext blockchainContext = new BlockchainDbContext())
                {
                    await blockchainContext.Database.MigrateAsync().ConfigureAwait(false);
                }
                DbContextOptionsBuilder<AdminDbContext> adminDbContextOptBuilder = new DbContextOptionsBuilder<AdminDbContext>();
                adminDbContextOptBuilder.UseNpgsql(NodeSettings.Configs.AdminDbConnection.ToString());
                using (AdminDbContext adminDbContext = new AdminDbContext(adminDbContextOptBuilder.Options))
                {
                    adminDbContext.Database.MigrateAsync().Wait(TimeSpan.FromSeconds(1));
                }
                await SqlFunctionsUpdator.UpdateFunctionsAsync("DbBackup").ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task StartApplicationAsync(string[] args)
        {
            try
            {
                await InitNode().ConfigureAwait(false);
                Task appTask = CreateWebHostBuilder(args)
                    .UseStartup<Startup>()
                    .Build()
                    .RunAsync(NodeSettings.AppShutdownTokenSource.Token);
                await appTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application shutdown.");
            }           
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}
