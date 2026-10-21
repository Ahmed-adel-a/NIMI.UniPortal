namespace NIMI.UniPortal.Modules.Materials.Domain;

/// <summary>Rule R3: which course files may be uploaded, how they are classified, and how big they may be.</summary>
public static class MaterialFiles
{
    public const long MaxBytes = 50 * 1024 * 1024;

    private static readonly Dictionary<string, MaterialFileType> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = MaterialFileType.Pdf,
        [".ppt"] = MaterialFileType.Ppt,
        [".pptx"] = MaterialFileType.Ppt,
        [".doc"] = MaterialFileType.Doc,
        [".docx"] = MaterialFileType.Doc,
        [".xls"] = MaterialFileType.Xls,
        [".xlsx"] = MaterialFileType.Xls,
        [".zip"] = MaterialFileType.Zip,
        [".png"] = MaterialFileType.Image,
        [".jpg"] = MaterialFileType.Image,
        [".jpeg"] = MaterialFileType.Image,
        [".mp4"] = MaterialFileType.Video,
    };

    public static MaterialFileType Classify(string fileName) =>
        Allowed.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out var type) ? type : MaterialFileType.Other;

    public static void EnsureAllowed(string fileName, long sizeBytes)
    {
        if (!Allowed.ContainsKey(Path.GetExtension(fileName ?? string.Empty)))
            throw new ArgumentException("Allowed files: pdf, ppt, pptx, doc, docx, xls, xlsx, zip, png, jpg, jpeg, mp4.", "file");

        if (sizeBytes is < 1 or > MaxBytes)
            throw new ArgumentException("A file must be between 1 byte and 50 MB.", "file");
    }
}
