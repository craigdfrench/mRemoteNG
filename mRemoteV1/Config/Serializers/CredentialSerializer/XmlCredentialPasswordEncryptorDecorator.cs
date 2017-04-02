﻿using System;
using System.Collections.Generic;
using System.Security;
using System.Xml.Linq;
using mRemoteNG.Credential;
using mRemoteNG.Security;

namespace mRemoteNG.Config.Serializers.CredentialSerializer
{
    public class XmlCredentialPasswordEncryptorDecorator : ISerializer<IEnumerable<ICredentialRecord>, string>
    {
        private readonly ISerializer<IEnumerable<ICredentialRecord>, string> _baseSerializer;
        private readonly ICryptographyProvider _cryptographyProvider;
        private readonly SecureString _encryptionKey;

        public XmlCredentialPasswordEncryptorDecorator(ISerializer<IEnumerable<ICredentialRecord>, string> baseSerializer, ICryptographyProvider cryptographyProvider, SecureString encryptionKey)
        {
            if (baseSerializer == null)
                throw new ArgumentNullException(nameof(baseSerializer));
            if (cryptographyProvider == null)
                throw new ArgumentNullException(nameof(cryptographyProvider));
            if (encryptionKey == null)
                throw new ArgumentNullException(nameof(encryptionKey));

            _baseSerializer = baseSerializer;
            _cryptographyProvider = cryptographyProvider;
            _encryptionKey = encryptionKey;
        }


        public string Serialize(IEnumerable<ICredentialRecord> credentialRecords)
        {
            if (credentialRecords == null)
                throw new ArgumentNullException(nameof(credentialRecords));

            var baseReturn = _baseSerializer.Serialize(credentialRecords);
            var encryptedReturn = EncryptPasswordAttributes(baseReturn, _encryptionKey);
            return encryptedReturn;
        }

        private string EncryptPasswordAttributes(string xml, SecureString encryptionKey)
        {
            var xdoc = XDocument.Parse(xml);
            xdoc.Root?.SetAttributeValue("Auth", _cryptographyProvider.Encrypt(Guid.NewGuid().ToString(), encryptionKey));
            foreach (var element in xdoc.Descendants())
            {
                var passwordAttribute = element.Attribute("Password");
                if (passwordAttribute == null) continue;
                var encryptedPassword = _cryptographyProvider.Encrypt(passwordAttribute.Value, encryptionKey);
                passwordAttribute.Value = encryptedPassword;
            }
            return xdoc.Declaration + Environment.NewLine + xdoc;
        }
    }
}