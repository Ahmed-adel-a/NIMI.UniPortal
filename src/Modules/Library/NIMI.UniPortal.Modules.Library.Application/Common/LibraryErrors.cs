using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>Turns a domain state rule (InvalidOperationException) into a 409 with the domain's own message.</summary>
internal static class LibraryErrors
{
    public static void AsConflict(Action change)
    {
        try
        {
            change();
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}
