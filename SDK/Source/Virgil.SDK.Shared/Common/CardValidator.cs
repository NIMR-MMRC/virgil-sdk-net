﻿#region Copyright (C) 2016 Virgil Security Inc.
// Copyright (C) 2016 Virgil Security Inc.
// 
// Lead Maintainer: Virgil Security Inc. <support@virgilsecurity.com>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions 
// are met:
// 
//   (1) Redistributions of source code must retain the above copyright
//   notice, this list of conditions and the following disclaimer.
//   
//   (2) Redistributions in binary form must reproduce the above copyright
//   notice, this list of conditions and the following disclaimer in
//   the documentation and/or other materials provided with the
//   distribution.
//   
//   (3) Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived 
//   from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion

namespace Virgil.SDK.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Virgil.SDK.Cryptography;
    using Virgil.SDK.Client;

    public class CardValidator : ICardValidator
    {
        private readonly Crypto crypto;
        private readonly Dictionary<string, PublicKey> verifiers;   

        private const string ServiceCardId    = "3e29d43373348cfb373b7eae189214dc01d7237765e572db685839b64adca853";
        private const string ServicePublicKey = "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUNvd0JRWURLMlZ3QXlFQVlSNTAx" +
                                                "a1YxdFVuZTJ1T2RrdzRrRXJSUmJKcmMyU3lhejVWMWZ1RytyVnM9Ci0tLS0tRU5E" +
                                                "IFBVQkxJQyBLRVktLS0tLQo=";

        /// <summary>
        /// Initializes a new instance of the <see cref="CardValidator"/> class.
        /// </summary>
        public CardValidator(Crypto crypto)
        {
            this.crypto = crypto;

            var servicePublicKey = crypto.ImportPublicKey(Convert.FromBase64String(ServicePublicKey));
            this.verifiers = new Dictionary<string, PublicKey>
            {
                [ServiceCardId] = servicePublicKey
            };
        }

        /// <summary>
        /// Adds the signature verifier.
        /// </summary>
        public void AddVerifier(string verifierId, byte[] verifierPublicKey)
        {
            if (string.IsNullOrWhiteSpace(verifierId))
                throw new ArgumentException(Localization.ExceptionArgumentIsNullOrWhitespace, nameof(verifierId));

            if (verifierPublicKey == null)
                throw new ArgumentNullException(nameof(verifierPublicKey));
            
            var publicKey = this.crypto.ImportPublicKey(verifierPublicKey);
            this.verifiers.Add(verifierId, publicKey);
        }       

        /// <summary>
        /// Validates a <see cref="Card"/> using pined Public Keys.
        /// </summary>
        public virtual bool Validate(Card card)
        {
            // Support for legacy Cards.
            if (card.Version == "3.0")
            {
                return true;
            }

            var fingerprint = this.crypto.CalculateFingerprint(card.Snapshot);
            var fingerprintHex = fingerprint.ToHEX();

            if (fingerprintHex != card.Id)
            {
                return false;
            }

            // add self signature verifier

            var allVerifiers = this.verifiers.ToDictionary(it => it.Key, it => it.Value);
            allVerifiers.Add(fingerprintHex, this.crypto.ImportPublicKey(card.PublicKey));

            foreach (var verifier in allVerifiers)
            {
                if (!card.Signatures.ContainsKey(verifier.Key))
                {
                    return false;
                }
                
                var isValid = this.crypto.Verify(fingerprint.GetValue(), 
                    card.Signatures[verifier.Key], verifier.Value);

                if (!isValid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}