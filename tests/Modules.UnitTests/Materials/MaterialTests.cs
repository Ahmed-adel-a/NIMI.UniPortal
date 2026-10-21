// ACCEPTANCE TEST — written by the consultant for phase P07-materials. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Materials.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Materials;

/// <summary>Course files: allowed types and sizes (R3), metadata rules, soft delete (R4) and the download counter.</summary>
public sealed class MaterialTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("slides.pdf", MaterialFileType.Pdf)]
    [InlineData("Lecture 1.PPTX", MaterialFileType.Ppt)]
    [InlineData("notes.ppt", MaterialFileType.Ppt)]
    [InlineData("sheet.docx", MaterialFileType.Doc)]
    [InlineData("grades.xlsx", MaterialFileType.Xls)]
    [InlineData("code.zip", MaterialFileType.Zip)]
    [InlineData("diagram.JPEG", MaterialFileType.Image)]
    [InlineData("board.png", MaterialFileType.Image)]
    [InlineData("recording.mp4", MaterialFileType.Video)]
    [InlineData("readme.txt", MaterialFileType.Other)]
    public void Files_are_classified_by_extension(string fileName, MaterialFileType expected)
    {
        Assert.Equal(expected, MaterialFiles.Classify(fileName));
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("script.js")]
    [InlineData("noextension")]
    [InlineData("readme.txt")]
    public void Only_course_file_types_are_allowed(string fileName)
    {
        Assert.Equal("file", Assert.ThrowsAny<ArgumentException>(() => MaterialFiles.EnsureAllowed(fileName, 1024)).ParamName);
    }

    [Fact]
    public void Files_must_be_between_one_byte_and_50_megabytes()
    {
        MaterialFiles.EnsureAllowed("big.mp4", MaterialFiles.MaxBytes);

        Assert.Equal(50L * 1024 * 1024, MaterialFiles.MaxBytes);
        Assert.Equal("file", Assert.ThrowsAny<ArgumentException>(() => MaterialFiles.EnsureAllowed("big.mp4", MaterialFiles.MaxBytes + 1)).ParamName);
        Assert.Equal("file", Assert.ThrowsAny<ArgumentException>(() => MaterialFiles.EnsureAllowed("empty.pdf", 0)).ParamName);
    }

    [Fact]
    public void A_material_keeps_its_file_and_metadata()
    {
        var material = NewMaterial(weekNumber: 3);

        Assert.Equal(7, material.Id.Version);
        Assert.Equal("Lecture 3 — Graphs", material.Title);
        Assert.Equal(3, material.WeekNumber);
        Assert.Equal("graphs.pdf", material.FileName);
        Assert.Equal(MaterialFileType.Pdf, material.FileType);
        Assert.Equal(2048, material.SizeBytes);
        Assert.Equal(0, material.DownloadCount);
        Assert.False(material.IsDeleted);
    }

    [Fact]
    public void Invalid_metadata_is_rejected_with_its_field_name()
    {
        Assert.Equal("title", Assert.ThrowsAny<ArgumentException>(() => NewMaterial(title: " ")).ParamName);
        Assert.Equal("title", Assert.ThrowsAny<ArgumentException>(() => NewMaterial(title: new string('t', 256))).ParamName);
        Assert.Equal("weekNumber", Assert.ThrowsAny<ArgumentException>(() => NewMaterial(weekNumber: 0)).ParamName);
        Assert.Equal("weekNumber", Assert.ThrowsAny<ArgumentException>(() => NewMaterial(weekNumber: 31)).ParamName);
        Assert.Equal("file", Assert.ThrowsAny<ArgumentException>(() => NewMaterial(fileName: "payload.exe")).ParamName);
    }

    [Fact]
    public void Editing_changes_only_the_metadata()
    {
        var material = NewMaterial();

        material.Edit("Lecture 3 — Graphs (updated)", "Now with exercises", 4, Now.AddDays(1));

        Assert.Equal("Lecture 3 — Graphs (updated)", material.Title);
        Assert.Equal("Now with exercises", material.Description);
        Assert.Equal(4, material.WeekNumber);
        Assert.Equal("graphs.pdf", material.FileName);
        Assert.Equal(Now.AddDays(1), material.UpdatedAt);
    }

    [Fact]
    public void Deleting_is_soft_and_downloads_are_counted()
    {
        var material = NewMaterial();

        material.RecordDownload();
        material.RecordDownload();
        material.Delete(Now);
        material.Delete(Now.AddDays(1));

        Assert.Equal(2, material.DownloadCount);
        Assert.True(material.IsDeleted);
        Assert.Equal("2026/10/graphs-key", material.StorageKey);   // the file is kept
    }

    private static Material NewMaterial(string title = "Lecture 3 — Graphs", int? weekNumber = null, string fileName = "graphs.pdf") =>
        Material.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, Guid.CreateVersion7(), title, null, weekNumber,
            "2026/10/graphs-key", fileName, "application/pdf", 2048, Now);
}
