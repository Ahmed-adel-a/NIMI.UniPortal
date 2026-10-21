using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.HandleCallback;

/// <summary>
/// P5.7 — a provider callback (anonymous at the edge, signature-verified by the adapter). It is a command because it
/// may create the student fee, the payment row and the receipt number.
/// </summary>
public sealed record HandlePaymentCallbackCommand(
    string Provider,
    HttpRequestData Request) : ICommand<PaymentIntentDto>;
