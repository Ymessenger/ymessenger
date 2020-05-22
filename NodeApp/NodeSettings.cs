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
using NodeApp.ExceptionClasses;
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace NodeApp
{
    public static class NodeSettings
    {
        public const int WEBSOCKET_BUFFER_SIZE = 1024 * 16;
        public const int MAX_REQUEST_SIZE = 1024 * 1024;
        public const int MAX_HTTP_REQUEST_SIZE = 1024 * 1024 * 100;
        public const int MAX_CONNECIONS = 5000;
        public const int LETSENCRYPT_PORT = 80;
        public const int DASHBOARD_PORT = 5555;
        public static readonly string LOCAL_FILE_STORAGE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "LocalFileStorage");
        public static CancellationTokenSource AppShutdownTokenSource { get; } = new CancellationTokenSource();
        private static Configs settings;
        public static readonly BlockVm GENESIS_BLOCK;

        private static readonly NodeVm DEFAULT_NODE_INFO = new NodeVm
        {
            Id = 1000,
            About = "Company information.",
            Name = "COMPANY NAME",
            StartDay = DateTime.UtcNow.Date,
            Routing = true,
            Storage = true,
            Visible = true,
            Tag = "COMPANYTAG",
            Country = "USA",
            Domains = new List<string> { "nodedomain.com" },
            Photo = "https://nodedomain.com:5000/api/Files/NODEIMAGESTRINGID"
        };
       

        private static readonly CertificateInfo DEFAULT_CERTINFO = new CertificateInfo
        {
            CountryName = "USA",
            Domain = "nodedomain.com",
            Email = "mailbox@nodedomain.com",
            Locality = "EN",
            Location = "certificate.pfx",
            Password = "password",
            Organization = "Company name",
            OrganizationUnit = "Company department",
            State = "US"
        };

        public static bool IsAppConfigured
        {
            get
            {
                try
                {
                    ThrowIfSettingsNotValid(Configs);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }


        public static Configs Configs
        {
            get => settings;
            set => settings = value;
        }
        public static void LoadSettingsFromFile(string filePath = "Config/appsettings.json")
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Configs = new Configs(filePath);
                    ThrowIfSettingsNotValid(Configs);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public static void ThrowIfSettingsNotValid(Configs settings)
        {
            if (settings.Node == null)
            {
                throw new InvalidConfigException($"Invalid node information. The information should look something like this:{ObjectSerializer.ObjectToJson(DEFAULT_NODE_INFO)}");
            }
            else
            {
                if (settings.Node.Id <= 0)
                {
                    throw new InvalidConfigException($"Node id should be lagger than 1: {settings.Node.Id}");
                }

                if (string.IsNullOrWhiteSpace(settings.Node.Name) || settings.Node.Name.Length > 50)
                {
                    throw new InvalidConfigException($"Invalid node name: {settings.Node.Name}");
                }
            }
            if (settings.NodesUrls == null || !settings.NodesUrls.Any())
            {
                settings.NodesUrls = new List<string> { "*" };
            }
            if (settings.Certificate == null)
            {
                throw new InvalidConfigException($"Invalid certificate information. The information should look something like this:{ObjectSerializer.ObjectToJson(DEFAULT_CERTINFO)}");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(settings.Certificate.Location) && !File.Exists(settings.Certificate.Location))
                {
                    throw new InvalidConfigException($@"Certificate file ""{settings.Certificate.Location}"" not found.");
                }
            }
        }
        public static void ShutdownApplication(int cancelAfter)
        {
            AppShutdownTokenSource.CancelAfter(cancelAfter);
        }
    }
}
