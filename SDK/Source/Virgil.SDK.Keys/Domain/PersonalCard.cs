﻿namespace Virgil.SDK.Keys.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Crypto;
    using Newtonsoft.Json;
    using TransferObject;

    internal class PersonalCardStorageDto
    {
        public VirgilCardDto virgil_card { get; set; }
        public byte[] private_key { get; set; }
    }

    public class PersonalCard : RecipientCard
    {
        internal PersonalCard(VirgilCardDto cardDto, PrivateKey privateKey) : base(cardDto)
        {
            this.PrivateKey = privateKey;
        }

        public PrivateKey PrivateKey { get; }

        public byte[] Decrypt(byte[] cipherData)
        {
            using (var cipher = new VirgilCipher())
            {
                var contentInfoSize = VirgilCipherBase.DefineContentInfoSize(cipherData);
                if (contentInfoSize == 0)
                {
                    throw new ArgumentException("Content info header is missing or corrupted", nameof(cipherData));
                }

                return cipher.DecryptWithKey(cipherData, this.GetRecepientId(), this.PrivateKey.Data);
            }
        }

        public string Decrypt(string cipherData)
        {
            return (this.Decrypt(Convert.FromBase64String(cipherData))).GetString(Encoding.UTF8);
        }

        public async Task Sign(RecipientCard signedCard)
        {
            var services = ServiceLocator.GetServices();
            var sign = await services.VirgilCardClient.Sign(signedCard.Id, signedCard.Hash, this.Id, this.PrivateKey);
        }

        public async Task Unsign(RecipientCard signedCard)
        {
            var services = ServiceLocator.GetServices();
            await services.VirgilCardClient.Unsign(signedCard.Id, this.Id, this.PrivateKey);
        }

        public string Export()
        {
            var data = new PersonalCardStorageDto
            {
                virgil_card = this.VirgilCardDto,
                private_key = this.PrivateKey.Data
            };

            return JsonConvert.SerializeObject(data);
        }

        public byte[] Export(string password)
        {
            var data = new PersonalCardStorageDto
            {
                virgil_card = this.VirgilCardDto,
                private_key = this.PrivateKey.Data
            };
            var json = JsonConvert.SerializeObject(data);
            using (var cipher = new VirgilCipher())
            {
                cipher.AddPasswordRecipient(password.GetBytes(Encoding.UTF8));
                return cipher.Encrypt(json.GetBytes(Encoding.UTF8), true);
            }
        }

        public static PersonalCard Import(string personalCard)
        {
            var dto = JsonConvert.DeserializeObject<PersonalCardStorageDto>(personalCard);
            return new PersonalCard(dto.virgil_card, new PrivateKey(dto.private_key));
        }

        public static PersonalCard Import(byte[] personalCard, string password)
        {
            using (var cipher = new VirgilCipher())
            {
                var json = cipher.DecryptWithPassword(personalCard, password.GetBytes(Encoding.UTF8));
                var dto = JsonConvert.DeserializeObject<PersonalCardStorageDto>(json.GetString());
                return new PersonalCard(dto.virgil_card, new PrivateKey(dto.private_key));
            }
        }

        public static async Task<PersonalCard> Create(IdentityToken identityToken,
            Dictionary<string, string> customData = null)
        {
            using (var nativeKeyPair = new VirgilKeyPair())
            {
                var privateKey = new PrivateKey(nativeKeyPair);
                var publicKey = new PublicKey(nativeKeyPair);

                var services = ServiceLocator.GetServices();

                var cardDto = await services.VirgilCardClient.Create(
                    publicKey,
                    identityToken.IdentityType,
                    identityToken.Identity,
                    customData,
                    privateKey);

                return new PersonalCard(cardDto, privateKey);
            }
        }

        public static async Task<PersonalCard> Create(string identity, Dictionary<string, string> customData = null)
        {
            using (var nativeKeyPair = new VirgilKeyPair())
            {
                var privateKey = new PrivateKey(nativeKeyPair);
                var publicKey = new PublicKey(nativeKeyPair);

                var services = ServiceLocator.GetServices();

                var cardDto = await services.VirgilCardClient.Create(
                    publicKey,
                    IdentityType.Email,
                    identity,
                    customData,
                    privateKey);

                return new PersonalCard(cardDto, privateKey);
            }
        }

        public static async Task<PersonalCard> AttachTo(PersonalCard personalCard, IdentityToken identityToken)
        {
            var services = ServiceLocator.GetServices();

            var cardDto = await services.VirgilCardClient.CreateAttached(
                personalCard.PublicKey.Id,
                identityToken.IdentityType,
                identityToken.Identity,
                null,
                personalCard.PrivateKey);

            return new PersonalCard(cardDto, personalCard.PrivateKey);
        }

        public async Task UploadPrivateKey()
        {
            var services = ServiceLocator.GetServices();
            await services.PrivateKeysClient.Put(this.PublicKey.Id, this.PrivateKey);
        }

        public static List<PersonalCard> Load(IdentityToken identityToken)
        {
            // search by email
            // get token
            // try get private key ?
            // get all virgil cards
            throw new NotImplementedException();
        }
    }
}