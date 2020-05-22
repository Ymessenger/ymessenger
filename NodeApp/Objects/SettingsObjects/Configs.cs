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
using Microsoft.Extensions.Configuration;
using ObjectsLibrary.Converters;
using ObjectsLibrary.OpenStackSwift;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;

namespace NodeApp.Objects.SettingsObjects
{
    public class Configs
    {
        private readonly IConfiguration _configuration;
        private List<string> _nodesUrls;
        private SmtpClientInfo _smtpClient;
        private List<string> _anotherNodesUrls;
        private NodeVm _node;
        private CertificateInfo _certificate;        
        private string _pushServerUrl;
        private OpenStackOptions _openStackOptions;
        private S3FileStorageOptions _s3FileStorageOptions;
        private string _licensorUrl;
        private string _password;
        private short? _maxDbBackups;
        private bool? _allowedRegistration;
        private bool? _confirmUsers;
        private DatabaseConnectionInfo _messengerDbConnection;
        private DatabaseConnectionInfo _blockchainDbConnection;
        private CacheServerConnectionInfo _cacheServerConnection;
        private List<string> _trustedIps;
        private bool? _recoveryMode;        
        private SmsServiceConfiguration _smsServiceConfiguration;

        private readonly string _configurationFilePath;        
        public Configs(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentNullException(nameof(filePath));
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile(filePath);
            IConfiguration configuration = builder.Build();
            _configuration = configuration;
            _configurationFilePath = filePath;
        }

        public Configs()
        {
        }
        public Configs(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public List<string> NodesUrls 
        { 
            get 
            {
                if (_nodesUrls == null)
                    _nodesUrls = _configuration.GetSection(nameof(NodesUrls)).Get<string[]>()?.ToList();
                return _nodesUrls;               
            }
            set
            {
                _nodesUrls = value;
            }
        }       
        
        public SmtpClientInfo SmtpClient 
        { 
            get 
            {
                if (_smtpClient == null)
                    _smtpClient = _configuration.GetSection(nameof(SmtpClient)).Get<SmtpClientInfo>();
                return _smtpClient;                
            } 
            set 
            {
                _smtpClient = value;
            }
        }        
        public DatabaseConnectionInfo MessengerDbConnection
        {
            get
            {
                if (_messengerDbConnection == null)
                    _messengerDbConnection = _configuration.GetSection(nameof(MessengerDbConnection)).Get<DatabaseConnectionInfo>();
                return _messengerDbConnection;               
            }
            set
            {
                _messengerDbConnection = value;
            }
        }       
        public DatabaseConnectionInfo BlockchainDbConnection
        {
            get
            {
                if (_blockchainDbConnection == null)
                    _blockchainDbConnection = _configuration.GetSection(nameof(BlockchainDbConnection)).Get<DatabaseConnectionInfo>();
                return _blockchainDbConnection;                
            }
            set
            {
                _blockchainDbConnection = value;
            }
        }        
        public CacheServerConnectionInfo CacheServerConnection
        {
            get
            {
                if (_cacheServerConnection == null)
                    _cacheServerConnection = _configuration.GetSection(nameof(CacheServerConnection)).Get<CacheServerConnectionInfo>();
                return _cacheServerConnection;                
            }
            set
            {
                _cacheServerConnection = value;
            }
        }       
        public NodeVm Node 
        { 
            get 
            {
                if (_node == null)
                    _node = _configuration.GetSection(nameof(Node)).Get<NodeVm>();
                return _node;
            }
            set
            {
                _node = value;
            }
        }        
        public List<string> AnotherNodesUrls 
        { 
            get 
            {
                if (_anotherNodesUrls == null)
                    _anotherNodesUrls = _configuration.GetSection(nameof(AnotherNodesUrls)).Get<string[]>()?.ToList();
                return _anotherNodesUrls;                
            } 
            set 
            {
                _anotherNodesUrls = value;
            } 
        }        
        public CertificateInfo Certificate 
        { 
            get 
            {
                if (_certificate == null)
                    _certificate = _configuration.GetSection(nameof(Certificate)).Get<CertificateInfo>();
                return _certificate;                
            } 
            set 
            {
                _certificate = value;
            }
        }       
               
        public string NotificationServerURL 
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_pushServerUrl))
                    _pushServerUrl = _configuration.GetValue<string>(nameof(NotificationServerURL));
                return _pushServerUrl;
            }
            set
            {
                _pushServerUrl = value;
            } 
        }        
        public OpenStackOptions OpenStackOptions 
        {
            get 
            {
                if (_openStackOptions == null)
                    _openStackOptions = _configuration.GetSection(nameof(OpenStackOptions)).Get<OpenStackOptions>();
                return _openStackOptions;                
            }
            set 
            {
                _openStackOptions = value;
            }
        }        
        public string LicensorUrl 
        { 
            get
            {
                if (string.IsNullOrWhiteSpace(_licensorUrl))
                    _licensorUrl = _configuration.GetValue<string>(nameof(LicensorUrl));
                return _licensorUrl;                
            }
            set
            {
                _licensorUrl = value;
            } 
        }        
        public string Password 
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_password))
                    _password = _configuration.GetValue<string>(nameof(Password));
                return _password;                
            }
            set
            {
                _password = value;
            }
        }        
        public short MaxDbBackups 
        {
            get
            {
                if (_maxDbBackups == null)
                    _maxDbBackups = _configuration.GetValue<short>(nameof(MaxDbBackups), 5);
                return _maxDbBackups.Value;                
            }
            set
            {
                _maxDbBackups = value;
            }
        }        
        public bool ConfirmUsers 
        {
            get
            {
                if (_confirmUsers == null)
                    _confirmUsers = _configuration.GetValue(nameof(ConfirmUsers), true);
                return _confirmUsers.Value;                
            }
            set
            {
                _confirmUsers = value;
            }
        }
        public DatabaseConnectionInfo AdminDbConnection
        {
            get
            {
                return _configuration.GetSection(nameof(AdminDbConnection)).Get<DatabaseConnectionInfo>();
            }
        }
        
        public bool AllowedRegistration
        {
            get
            {
                if (_allowedRegistration == null)
                    _allowedRegistration = _configuration.GetValue(nameof(AllowedRegistration), false);
                return _allowedRegistration.Value;                
            }
            set
            {
                _allowedRegistration = value;
            }
        }
        public bool RecoveryMode
        {
            get
            {
                if (_recoveryMode == null)
                    _recoveryMode = _configuration.GetValue(nameof(RecoveryMode), false);
                return _recoveryMode.Value;
            }
            set
            {
                _recoveryMode = value;
            }
        }
        public List<string> TrustedIps
        {
            get
            {
                if (_trustedIps == null)
                    _trustedIps = _configuration.GetSection(nameof(TrustedIps)).Get<string[]>()?.ToList();
                return _trustedIps;
            }
            set
            {
                _trustedIps = value;
            }
        }       
        public S3FileStorageOptions S3FileStorageOptions
        {
            get
            {
                if (_s3FileStorageOptions == null)
                    _s3FileStorageOptions = _configuration.GetSection(nameof(S3FileStorageOptions)).Get<S3FileStorageOptions>();
                return _s3FileStorageOptions;
            }
            set
            {
                _s3FileStorageOptions = value;
            }
        } 
        public SmsServiceConfiguration SmsServiceConfiguration
        {
            get
            {
                if (_smsServiceConfiguration == null)
                {
                    var section = _configuration.GetSection(nameof(SmsServiceConfiguration));
                    switch (section["ServiceName"])
                    {
                        case SmsServiceTypes.BSG:
                            {
                                _smsServiceConfiguration = section.Get<BSGServiceConfiguration>();
                            }
                            break;
                        case SmsServiceTypes.SMSIntel:
                            {
                                _smsServiceConfiguration = section.Get <SMSIntelServiceConfiguration>();
                            }
                            break;
                        case SmsServiceTypes.SMSRU:
                            {
                                _smsServiceConfiguration = section.Get<SMSRUServiceConfiguration>();
                            }
                            break;
                        case SmsServiceTypes.GolosAloha:
                            {
                                _smsServiceConfiguration = section.Get<VoiceServiceConfiguration>();
                            }
                            break;
                        default:
                            _smsServiceConfiguration = new SmsServiceConfiguration();
                            break;
                    }
                }
                return _smsServiceConfiguration;
            }
            set
            {
                _smsServiceConfiguration = value;
            }
        }
        public LicensorSign LicensorSign { get; set; }
        public LicenseVm License { get; set; }
        public async Task UpdateConfigurationFileAsync()
        {
            string json = ObjectSerializer.ObjectToJson(this, true);
            await File.WriteAllTextAsync(_configurationFilePath, json).ConfigureAwait(false);
        }
    }
}