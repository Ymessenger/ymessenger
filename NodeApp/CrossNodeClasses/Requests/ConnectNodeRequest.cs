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
using NodeApp.CrossNodeClasses.Enums;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.ViewModels;
using System;

namespace NodeApp.CrossNodeClasses.Requests
{    
    [Serializable]
    public class ConnectNodeRequest : NodeRequest
    {       
        public byte[] EncryptedKey { get; set; }
        public byte[] Data { get; set; }      
        public NodeKeysDto Keys { get; set; }

        public ConnectNodeRequest(byte[] encryptedKey, byte[] data, NodeKeysDto keys)
        {
            EncryptedKey = encryptedKey;
            Data = data;
            Keys = keys;
            RequestType = NodeRequestType.Connect;
        }       
        public ConnectData GetConnectData(byte[] signPublicKey, byte[] masterPassword,  byte[] encPrivateKey = null)
        {
            ConnectData connectData;            
            byte[] symmetricKey = Encryptor.AsymmetricDecryptKey(EncryptedKey, encPrivateKey, signPublicKey, masterPassword).Data;
            byte[] decryptedData = Encryptor.SymmetricDataDecrypt(Data, signPublicKey, symmetricKey, masterPassword).DecryptedData;
            connectData = ObjectSerializer.ByteArrayToObject<ConnectData>(decryptedData);
            connectData.SymmetricKey = symmetricKey;
            return connectData;
        }

    }
    [Serializable]
    public class ConnectData
    {
        public byte[] SymmetricKey { get; set; }
        public NodeVm Node { get; set; }
        public byte[] LicensorSign { get; set; }
        public LicenseVm License { get; set; }
    }
}
