using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.StartCheckout;

/// <summary>P5.7 — a student starts an online checkout for one fee and amount.</summary>
public sealed record StartCheckoutCommand(
    Guid StudentId,
    bool IsStudent,
    Guid FeeItemId,
    decimal Amount) : ICommand<CheckoutDto>;
