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
using Microsoft.AspNetCore.Http;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects.SettingsObjects;
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;
using ObjectsLibrary.OpenStackSwift;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NodeApp.HttpServer.Models
{
    public class ConfigsModel
    {
        public List<string> NodesUrls { get; set; }       
        public int ClientsPort { get; set; }        
        public int NodesPort { get; set; }        
        public SmtpClientInfo SmtpClient { get; set; }        
        public DatabaseConnectionInfo MessengerDbConnection { get; set; }       
        public DatabaseConnectionInfo BlockchainDbConnection { get; set; }        
        public CacheServerConnectionInfo CacheServerConnection { get; set; }        
        public NodeVm Node { get; set; }        
        public List<string> AnotherNodesUrls { get; set; }        
        public CertificateInfo Certificate { get; set; }       
        public BSGServiceConfiguration BSGServiceConfiguration { get; set; } = new BSGServiceConfiguration();
        public SMSRUServiceConfiguration SMSRUServiceConfiguration { get; set; } = new SMSRUServiceConfiguration();
        public SMSIntelServiceConfiguration SMSIntelServiceConfiguration { get; set; } = new SMSIntelServiceConfiguration();
        public VoiceServiceConfiguration GolosAlohaServiceConfiguration { get; set; } = new VoiceServiceConfiguration();        
        public string NotificationServerURL { get; set; }        
        public OpenStackOptions OpenStackOptions { get; set; }        
        public string LicensorUrl { get; set; }        
        public string Password { get; set; }       
        public short MaxDbBackups { get; set; }        
        public bool ConfirmUsers { get; set; }        
        public bool AllowedRegistration { get; set; }        
        [DataType(DataType.Upload)]
        public IFormFile NodeImage { get; set; }
        public NodeKeysDto NodeKeys { get; set; }       
        public bool RecoveryMode { get; set; }        
        public List<string> TrustedIps { get; set; }       
        public BlockchainInfo BlockchainInfo { get; set; }        
        public LicenseVm License { get; set; }     
        public S3FileStorageOptions S3FileStorageOptions { get; set; }
        public Dictionary<string, string> ContriesISO { get; set; }    
        public ErrorModel ErrorModel { get; set; }
    }
}
