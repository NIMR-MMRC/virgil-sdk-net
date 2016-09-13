namespace Virgil.SDK.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Virgil.SDK.Clients.Models;

    /// <summary>
    /// Provides common methods to interact with Public Keys resource endpoints.
    /// </summary>
    public interface ICardsServiceClient : IVirgilService
    {
        SecureRequest BuildCreateRequest(VirgilCardCreateModel model);
        SecureRequest BuildRevokeRequest(Guid cardId);

        Task<IEnumerable<VirgilCardModel>> SearchInGlobalScopeAsync(IEnumerable<string> identities, string identityType = null);
        Task<IEnumerable<VirgilCardModel>> SearchInAppScopeAsync(IEnumerable<string> identities, string identityType = null, bool isConfirmed = false);
        Task<VirgilCardModel> GetAsync(Guid cardId);

        Task<TResponse> SendSecurityRequest<TResponse>(SecureRequest request);
    }
}   