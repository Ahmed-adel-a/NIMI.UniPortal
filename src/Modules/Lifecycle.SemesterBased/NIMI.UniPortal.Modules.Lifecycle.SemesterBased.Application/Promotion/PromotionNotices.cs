using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Notifications.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>Tells the student their year-end decision, in English and Arabic.</summary>
internal static class PromotionNotices
{
    public static NotificationRequested For(YearPromotion decision)
    {
        var (title, titleAr, message, messageAr) = decision.Result switch
        {
            PromotionResult.Promoted => (
                "Promoted", "تم نقلك للفرقة التالية",
                $"Congratulations: you move up to year {decision.ToLevel}.",
                $"مبروك، تم نقلك إلى الفرقة {decision.ToLevel}."),
            PromotionResult.PromotedWithCarried => (
                "Promoted with carried subjects", "تم نقلك بمواد تخلف",
                $"You move up to year {decision.ToLevel} carrying {decision.FailedSubjects} failed subject(s).",
                $"تم نقلك إلى الفرقة {decision.ToLevel} مع {decision.FailedSubjects} مواد تخلف."),
            PromotionResult.Repeat => (
                "Repeating the year", "إعادة الفرقة",
                $"You stay in year {decision.ToLevel} and take your failed subjects again.",
                $"ستبقى في الفرقة {decision.ToLevel} وتعيد المواد التي لم تنجح فيها."),
            PromotionResult.External => (
                "Exams from outside", "امتحان من الخارج",
                $"You sit your failed subjects of year {decision.ToLevel} from outside.",
                $"ستؤدي امتحانات المواد التي لم تنجح فيها في الفرقة {decision.ToLevel} من الخارج."),
            PromotionResult.Graduated => (
                "Programme completed", "إتمام متطلبات التخرج",
                "Congratulations: you have passed every subject of your programme.",
                "مبروك، لقد نجحت في جميع مواد البرنامج."),
            _ => (
                "Year-end decision on hold", "قرار النقل معلّق",
                "Your year-end decision is on hold until your outstanding fees are paid.",
                "قرار نقلك معلّق حتى سداد المصروفات المستحقة."),
        };

        return new NotificationRequested(
            Guid.CreateVersion7(), decision.DecidedAt, [decision.StudentId], "system", title, titleAr, message, messageAr, "/standing");
    }
}
