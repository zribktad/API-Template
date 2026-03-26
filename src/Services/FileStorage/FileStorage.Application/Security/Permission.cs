namespace FileStorage.Application.Security;

/// <summary>
/// Permission constants for the FileStorage microservice.
/// </summary>
public static class Permission
{
    public static class Files
    {
        public const string Upload = "Files.Upload";
        public const string Download = "Files.Download";
    }
}
