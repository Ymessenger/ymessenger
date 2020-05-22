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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeApp.HttpServer.Models;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.LicensorClasses;
using NodeApp.Objects.SettingsObjects;
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Enums;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.OpenStackSwift;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Authorize]
    public class ConfigurationController : Controller
    {
        private readonly IFileStorage _fileStorage;
        private readonly IConnectionsService _connectionsService;
        private readonly IFilesService _filesService;
        private readonly INodesService _nodesService;
        private readonly IKeysService _keysService;
        private readonly IAppServiceProvider _appServiceProvider;
        public ConfigurationController(IFileStorage fileStorage, IConnectionsService connectionsService, IFilesService filesService, INodesService nodesService, IKeysService keysService, IAppServiceProvider appServiceProvider)
        {
            _fileStorage = fileStorage;
            _connectionsService = connectionsService;
            _filesService = filesService;
            _keysService = keysService;
            _nodesService = nodesService;
            _appServiceProvider = appServiceProvider;
        }
        [HttpPost]
        public IActionResult Index(ConfigsModel configsModel)
        {
            if (configsModel != null)
            {
                configsModel.ContriesISO = GetCountriesISO();
                return View("Index", configsModel);
            }
            return BadRequest();
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ConfigsModel
            {
                AllowedRegistration = NodeSettings.Configs.AllowedRegistration,
                AnotherNodesUrls = NodeSettings.Configs.AnotherNodesUrls,
                BlockchainDbConnection = NodeSettings.Configs.BlockchainDbConnection ?? new DatabaseConnectionInfo(),
                CacheServerConnection = NodeSettings.Configs.CacheServerConnection ?? new CacheServerConnectionInfo(),
                Certificate = NodeSettings.Configs.Certificate,
                ConfirmUsers = NodeSettings.Configs.ConfirmUsers,
                LicensorUrl = NodeSettings.Configs.LicensorUrl,
                MaxDbBackups = NodeSettings.Configs.MaxDbBackups,
                MessengerDbConnection = NodeSettings.Configs.MessengerDbConnection ?? new DatabaseConnectionInfo(),
                Node = NodeSettings.Configs.Node,
                NodesUrls = NodeSettings.Configs.NodesUrls,
                NotificationServerURL = NodeSettings.Configs.NotificationServerURL,
                OpenStackOptions = NodeSettings.Configs.OpenStackOptions ?? new OpenStackOptions(),                
                SmtpClient = NodeSettings.Configs.SmtpClient ?? new SmtpClientInfo(),
                NodeKeys = NodeData.Instance.NodeKeys,
                RecoveryMode = NodeSettings.Configs.RecoveryMode,
                TrustedIps = NodeSettings.Configs.TrustedIps,
                BlockchainInfo = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false),
                ClientsPort = NodeSettings.Configs.Node.ClientsPort,
                NodesPort = NodeSettings.Configs.Node.NodesPort,
                License = NodeSettings.Configs.License,
                S3FileStorageOptions = NodeSettings.Configs.S3FileStorageOptions,
                ContriesISO = GetCountriesISO(),
                SMSRUServiceConfiguration = NodeSettings.Configs.SmsServiceConfiguration is SMSRUServiceConfiguration 
                    ? (SMSRUServiceConfiguration) NodeSettings.Configs.SmsServiceConfiguration : new SMSRUServiceConfiguration(),
                SMSIntelServiceConfiguration = NodeSettings.Configs.SmsServiceConfiguration is SMSIntelServiceConfiguration
                    ? (SMSIntelServiceConfiguration) NodeSettings.Configs.SmsServiceConfiguration : new SMSIntelServiceConfiguration(),
                BSGServiceConfiguration = NodeSettings.Configs.SmsServiceConfiguration is BSGServiceConfiguration
                    ? (BSGServiceConfiguration) NodeSettings.Configs.SmsServiceConfiguration : new BSGServiceConfiguration(),
                GolosAlohaServiceConfiguration = NodeSettings.Configs.SmsServiceConfiguration is VoiceServiceConfiguration
                    ? (VoiceServiceConfiguration) NodeSettings.Configs.SmsServiceConfiguration : new VoiceServiceConfiguration()
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Update([FromForm] ConfigsModel configsModel)
        {
            ErrorModel errorModel = new ErrorModel();
            bool encryptionChangedFlag = false;
            try
            {                
                if(!configsModel.SmtpClient.IsValid() && configsModel.Node.RegistrationMethod == RegistrationMethod.EmailRequired)
                {
                    errorModel.Errors.Add("RegistrationMethod", "Selected registration method requires specifying the correct SmtpClient settings.");
                }
                int settingsCount = 0;
                SmsServiceConfiguration smsConfiguration = NodeSettings.Configs.SmsServiceConfiguration;
                if (configsModel.BSGServiceConfiguration.IsValid() && NodeSettings.Configs.SmsServiceConfiguration.GetType() != typeof(BSGServiceConfiguration))
                {                    
                    settingsCount++;
                    smsConfiguration = configsModel.BSGServiceConfiguration;
                }
                if (configsModel.SMSIntelServiceConfiguration.IsValid() && NodeSettings.Configs.SmsServiceConfiguration.GetType() != typeof(SMSIntelServiceConfiguration))
                {                    
                    settingsCount++;
                    smsConfiguration = configsModel.SMSIntelServiceConfiguration;
                }
                if (configsModel.SMSRUServiceConfiguration.IsValid() && NodeSettings.Configs.SmsServiceConfiguration.GetType() != typeof(SMSRUServiceConfiguration))
                {                   
                    settingsCount++;
                    smsConfiguration = configsModel.SMSRUServiceConfiguration;
                }      
                if(configsModel.GolosAlohaServiceConfiguration.IsValid() && NodeSettings.Configs.SmsServiceConfiguration.GetType() != typeof(VoiceServiceConfiguration))
                {
                    settingsCount++;
                    smsConfiguration = configsModel.GolosAlohaServiceConfiguration;
                }
                if(settingsCount > 1)
                {
                    errorModel.Errors.Add("SmsServiceConfiguration", "Found more than one SMS service configuration");
                }                
                if(settingsCount == 0 && configsModel.Node.RegistrationMethod == RegistrationMethod.PhoneRequired && !smsConfiguration.IsValid())
                {
                    errorModel.Errors.Add("RegistrationMethod", "Selected registration method requires specifying the correct SmsService settings.");
                }
                if (errorModel.Errors.Any())
                {
                    configsModel.ErrorModel = errorModel;
                    return Index(configsModel);
                }
                if(settingsCount == 0)
                {
                    if (configsModel.SMSIntelServiceConfiguration.IsValid())
                    {
                        smsConfiguration = configsModel.SMSIntelServiceConfiguration;
                    }
                    else if (configsModel.SMSRUServiceConfiguration.IsValid())
                    {
                        smsConfiguration = configsModel.SMSRUServiceConfiguration;
                    }
                    else if (configsModel.BSGServiceConfiguration.IsValid())
                    {
                        smsConfiguration = configsModel.BSGServiceConfiguration;
                    }
                    else if (configsModel.GolosAlohaServiceConfiguration.IsValid())
                    {
                        smsConfiguration = configsModel.GolosAlohaServiceConfiguration;
                    }
                }                    
                NodeSettings.Configs.AllowedRegistration = configsModel.AllowedRegistration;
                NodeSettings.Configs.BlockchainDbConnection = configsModel.BlockchainDbConnection;
                NodeSettings.Configs.CacheServerConnection = configsModel.CacheServerConnection;
                NodeSettings.Configs.ConfirmUsers = configsModel.ConfirmUsers;
                NodeSettings.Configs.LicensorUrl = configsModel.LicensorUrl;
                NodeSettings.Configs.MaxDbBackups = configsModel.MaxDbBackups;
                NodeSettings.Configs.MessengerDbConnection = configsModel.MessengerDbConnection;
                NodeSettings.Configs.OpenStackOptions = configsModel.OpenStackOptions;
                NodeSettings.Configs.S3FileStorageOptions = configsModel.S3FileStorageOptions;               
                NodeSettings.Configs.SmtpClient = configsModel.SmtpClient;
                NodeSettings.Configs.RecoveryMode = configsModel.RecoveryMode;
                NodeSettings.Configs.Node.ClientsPort = configsModel.ClientsPort;
                NodeSettings.Configs.Node.NodesPort = configsModel.NodesPort;
                NodeSettings.Configs.Node.SupportEmail = configsModel.Node.SupportEmail;
                NodeSettings.Configs.Node.AdminEmail = configsModel.Node.AdminEmail;
                NodeSettings.Configs.Node.PermanentlyDeleting = configsModel.Node.PermanentlyDeleting;
                NodeSettings.Configs.Node.RegistrationMethod = configsModel.Node.RegistrationMethod;
                NodeSettings.Configs.Node.Country = configsModel.Node.Country;
                NodeSettings.Configs.SmsServiceConfiguration = smsConfiguration;
                if (configsModel.Node.EncryptionType != NodeSettings.Configs.Node.EncryptionType)
                {
                    NodeSettings.Configs.Node.EncryptionType = configsModel.Node.EncryptionType;
                    encryptionChangedFlag = true;
                }
                if (configsModel.RecoveryMode)
                {
                    _connectionsService.CloseAllClientConnections();
                }

                if (configsModel.Node != null)
                {
                    NodeSettings.Configs.Node.About = configsModel.Node.About;
                    NodeSettings.Configs.Node.Name = configsModel.Node.Name;
                    NodeSettings.Configs.Node.Storage = configsModel.Node.Storage;
                    NodeSettings.Configs.Node.Visible = configsModel.Node.Visible;
                    NodeSettings.Configs.Node.Routing = configsModel.Node.Routing;
                    NodeSettings.Configs.Node.UserRegistrationAllowed = configsModel.Node.UserRegistrationAllowed;
                    if (configsModel.NodeImage != null && configsModel.NodeImage.Length > 0)
                    {
                        string path = _fileStorage.StorageName == "Local"
                            ? Path.Combine(
                                NodeSettings.LOCAL_FILE_STORAGE_PATH,
                                $"[{RandomExtensions.NextString(10)}]{configsModel.NodeImage.FileName}")
                            : null;
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            var fileHash = sha256.ComputeHash(configsModel.NodeImage.OpenReadStream());
                            var fileInfo = await _filesService.SaveFileAsync(
                                null,
                                configsModel.NodeImage.FileName,
                                path,
                                configsModel.NodeImage.Length,
                                fileHash,
                                _fileStorage.StorageName).ConfigureAwait(false);
                            await _fileStorage.UploadAsync(configsModel.NodeImage.OpenReadStream(), path ?? fileInfo.FileId).ConfigureAwait(false);
                            NodeSettings.Configs.Node.Photo = fileInfo.FileId;
                        }
                    }
                    if (LicensorClient.Instance.IsAuthentificated)
                    {
                        var editedNode = await LicensorClient.Instance.EditNodeAsync(NodeSettings.Configs.Node).ConfigureAwait(false);
                        _nodesService.CreateOrUpdateNodeInformationAsync(editedNode);
                    }
                }
                await NodeSettings.Configs.UpdateConfigurationFileAsync().ConfigureAwait(false);
                if (encryptionChangedFlag)
                {
                    _connectionsService.RestartNodeConnections();
                }
                NodeSettings.AppShutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
                return Index(configsModel);
            }
            catch (Exception ex)
            {
                errorModel.Errors.Add("Internal server error", ex.Message);
                configsModel.ErrorModel = errorModel;
                return Index(configsModel);
            }
        }
        [HttpGet]
        public IActionResult CreateKeys()
        {
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> CreateKeys([FromForm] CreateNewKeysModel model)
        {

            if (model.ExpirationTime < DateTime.UtcNow)
            {
                return PartialView(nameof(CreateKeys));
            }

            uint lifeTime = (uint)(model.ExpirationTime - DateTime.UtcNow).TotalSeconds;
            var nodeKeys = await _keysService.CreateNewNodeKeysAsync(NodeSettings.Configs.Node.Id, model.KeyType, lifeTime).ConfigureAwait(false);
            await _appServiceProvider.NodeNoticeService.SendNewNodeKeysNodeNoticeAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime).ConfigureAwait(false);
            await LicensorClient.Instance.AddNewKeyAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime, nodeKeys.GenerationTime, false).ConfigureAwait(false);
            NodeData.Instance.SetNodeKeys(nodeKeys);
            return Redirect($"{nameof(Index)}#keys-info");
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordModel model)
        {
            if (!TryValidateModel(model))
            {
                return BadRequest();
            }

            var reencryptedKeys = await _keysService.ReencryptNodeKeysAsync(NodeSettings.Configs.Node.Id, NodeSettings.Configs.Password, model.NewPassword).ConfigureAwait(false);
            NodeData.Instance.SetNodeKeys(reencryptedKeys.FirstOrDefault(key => key.KeyId == NodeData.Instance.NodeKeys.KeyId));
            NodeSettings.Configs.Password = model.NewPassword;
            await NodeSettings.Configs.UpdateConfigurationFileAsync().ConfigureAwait(false);
            return Redirect(nameof(Index));
        }
        private Dictionary<string, string> GetCountriesISO()
        {
            var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
               .Where(culture => culture.LCID != 0x7F)
               .Select(cult => new RegionInfo(cult.Name));
            var countriesISO = new Dictionary<string, string>();
            foreach (var region in regions.OrderBy(v => v.EnglishName))
            {
                if (!countriesISO.ContainsKey(region.ThreeLetterISORegionName))
                {
                    countriesISO.Add(region.ThreeLetterISORegionName, region.EnglishName);
                }
            }
            return countriesISO;
        }
    }
}