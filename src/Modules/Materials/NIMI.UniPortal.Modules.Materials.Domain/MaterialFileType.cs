namespace NIMI.UniPortal.Modules.Materials.Domain;

/// <summary>What kind of course file a material is — drives the icon and the <c>?type=</c> filter.</summary>
public enum MaterialFileType
{
    Pdf,
    Ppt,
    Doc,
    Xls,
    Zip,
    Image,
    Video,
    Other,
}
