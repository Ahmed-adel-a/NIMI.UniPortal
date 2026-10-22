using NIMI.UniPortal.Abstractions.Files;

namespace NIMI.UniPortal.Hosting.Storage;

/// <summary>
/// <see cref="IFileStorage"/> on the local disk under <c>Storage:RootPath</c> (default <c>App_Data/files</c> next to the
/// host). Keys are <c>yyyy/MM/&lt;guid-v7&gt;</c> — never the user's file name (rule R3). Files are written to a
/// temporary name first and moved into place, so a half-written upload is never visible under its key.
/// Object storage replaces this in the deployment phase (DEP).
/// </summary>
internal sealed class LocalFileStorage(string rootPath, TimeProvider clock) : IFileStorage
{
    private const string TempFolder = ".incoming";

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var now = clock.GetUtcNow();
        var key = $"{now:yyyy}/{now:MM}/{Guid.CreateVersion7():N}";
        var target = PathFor(key);
        var temp = Path.Combine(rootPath, TempFolder, Guid.CreateVersion7().ToString("N"));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Directory.CreateDirectory(Path.GetDirectoryName(temp)!);

        long size;
        await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
            size = file.Length;
        }

        File.Move(temp, target);
        return new StoredFile(key, size, contentType);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
            throw new FileNotFoundException("Stored file not found.", key);

        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Keys are ours (yyyy/MM/guid); anything that would escape the root is refused.</summary>
    private string PathFor(string key)
    {
        var full = Path.GetFullPath(Path.Combine(rootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
        return full.StartsWith(root, StringComparison.Ordinal)
            ? full
            : throw new InvalidOperationException("Invalid storage key.");
    }
}
