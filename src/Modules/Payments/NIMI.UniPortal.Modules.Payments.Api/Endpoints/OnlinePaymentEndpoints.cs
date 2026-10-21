using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Online;
using NIMI.UniPortal.Modules.Payments.Application.Online.GetPaymentIntent;
using NIMI.UniPortal.Modules.Payments.Application.Online.HandleCallback;
using NIMI.UniPortal.Modules.Payments.Application.Online.StartCheckout;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>
/// P5.7 — online checkout, the provider callback and the mock-only completion endpoint. The callback is anonymous
/// at the edge because the adapter verifies the provider signature (rules R9–R11).
/// </summary>
internal static class OnlinePaymentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints, bool mock)
    {
        var student = endpoints
            .MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        student.MapPost("/checkout", Checkout).WithName("StartPaymentCheckout");
        student.MapGet("/intents/{id:guid}", GetIntent).WithName("GetPaymentIntent");

        endpoints
            .MapPost("/api/payments/callbacks/{provider}", Callback)
            .WithTags("Payments")
            .AllowAnonymous()
            .WithName("PaymentCallback");

        if (mock)
        {
            endpoints
                .MapPost("/api/payments/mock/{id:guid}/complete", CompleteMock)
                .WithTags("Payments")
                .RequireAuthorization()
                .WithName("CompleteMockPayment");
        }
    }

    private static async Task<Ok<CheckoutDto>> Checkout(
        CheckoutPaymentRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new StartCheckoutCommand(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                request.FeeItemId,
                request.Amount),
            cancellationToken));

    private static async Task<Ok<PaymentIntentDto>> GetIntent(
        Guid id,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetPaymentIntentQuery(id, caller.GetUserId()),
            cancellationToken));

    private static async Task<Ok<PaymentIntentDto>> CompleteMock(
        Guid id,
        CompleteMockPaymentRequest request,
        ClaimsPrincipal caller,
        IMockPaymentCompleter completer,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await completer.CompleteAsync(
            id, caller.GetUserId(), request.Outcome, cancellationToken));

    private static async Task<Ok<PaymentIntentDto>> Callback(
        HttpRequest request,
        string provider,
        ISender sender,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        return TypedResults.Ok(await sender.Send(
            new HandlePaymentCallbackCommand(provider, new HttpRequestData(headers, body)),
            cancellationToken));
    }
}
