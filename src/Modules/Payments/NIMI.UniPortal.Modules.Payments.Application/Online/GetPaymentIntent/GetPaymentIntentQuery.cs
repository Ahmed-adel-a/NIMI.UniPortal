using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.GetPaymentIntent;

/// <summary>P5.7 — the owning student polls their checkout status.</summary>
public sealed record GetPaymentIntentQuery(Guid IntentId, Guid StudentId) : IQuery<PaymentIntentDto>;
