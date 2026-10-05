using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

namespace Tronzap.Sdk;

/// <summary>
/// The operations of the <see href="https://docs.tronzap.com/">TronZap API</see>: buy TRON energy and bandwidth,
/// activate addresses and run AML checks. <see cref="TronzapClient"/> is the implementation; the interface exists so
/// that code using the SDK can be tested without the API.
/// </summary>
/// <remarks>
/// Every method sends one signed request. A failure is reported as a <see cref="TronzapException"/>; invalid arguments
/// raise <see cref="ArgumentException"/> before anything is sent, and cancellation through the
/// <see cref="CancellationToken"/> raises <see cref="OperationCanceledException"/>.
/// </remarks>
public interface ITronzapClient
{
    /// <summary>Returns the resources on sale and their current prices.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The services and their price tiers.</returns>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<ServiceRates> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the account balance and the address that tops it up.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The balance.</returns>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AccountBalance> GetBalanceAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the on-chain resources (energy, bandwidth) and token balances (TRX, USDT) of an address.</summary>
    /// <param name="address">The TRON address to query.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The address resources and balances.</returns>
    /// <exception cref="ArgumentException"><paramref name="address"/> is missing or blank.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AddressInfo> GetAddressInfoAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates how much energy a token transfer needs and what that energy costs. Without a contract address the
    /// estimate is for a USDT (TRC20) transfer. The API does not reject a request whose sender and recipient are the
    /// same address.
    /// </summary>
    /// <param name="request">The transfer to estimate.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The energy estimate.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<EnergyEstimate> EstimateEnergyAsync(EstimateEnergyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Prices an energy purchase without creating a transaction.</summary>
    /// <param name="request">The purchase to price.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The price.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Calculation> CalculateAsync(CalculateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Buys energy for an address, optionally activating the address in the same transaction.</summary>
    /// <param name="request">The purchase.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created transaction.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Transaction> CreateEnergyTransactionAsync(EnergyTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Buys bandwidth for an address. Bandwidth is rented for one hour.</summary>
    /// <param name="request">The purchase.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created transaction.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Transaction> CreateBandwidthTransactionAsync(BandwidthTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Buys energy and bandwidth for an address in one transaction, optionally activating the address.</summary>
    /// <param name="request">The purchase.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created transaction.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Transaction> CreateResourceBundleTransactionAsync(ResourceBundleTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a TRON address. Activating an address that is already active fails with
    /// <see cref="TronzapApiException"/> and <see cref="TronzapErrorCode.AddressAlreadyActivated"/>.
    /// </summary>
    /// <param name="request">The address to activate.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created transaction.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Transaction> CreateAddressActivationTransactionAsync(AddressActivationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current state of a transaction. An unknown transaction fails with
    /// <see cref="TronzapErrorCode.TransactionNotFound"/>.
    /// </summary>
    /// <param name="request">The transaction ID, external ID, or both.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The transaction.</returns>
    /// <exception cref="ArgumentException">Neither ID is set, or one is blank.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<Transaction> CheckTransactionAsync(CheckTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the address to pay for a direct energy recharge and the rates energy is delivered at.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The direct recharge information.</returns>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<DirectRechargeInfo> GetDirectRechargeInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the available AML screening products and their prices.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The AML services, possibly empty.</returns>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<IReadOnlyList<AmlService>> GetAmlServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an AML screening of an address or a transaction hash. Screening runs asynchronously: poll
    /// <see cref="CheckAmlStatusAsync"/> until the status is <see cref="Models.AmlStatus.Completed"/>.
    /// </summary>
    /// <param name="request">What to screen.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created AML check.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AmlCheck> CreateAmlCheckAsync(AmlCheckRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the current state of an AML check and, once it is complete, its result.</summary>
    /// <param name="id">The AML check ID.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The AML check.</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> is missing or blank.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AmlCheck> CheckAmlStatusAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Returns the first page of past AML checks, ten per page, newest first.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The first page of AML checks.</returns>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AmlHistory> GetAmlHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns one page of past AML checks, newest first.</summary>
    /// <param name="request">The page and an optional status filter.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The page of AML checks.</returns>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="TronzapException">The request failed.</exception>
    Task<AmlHistory> GetAmlHistoryAsync(AmlHistoryRequest request, CancellationToken cancellationToken = default);
}
