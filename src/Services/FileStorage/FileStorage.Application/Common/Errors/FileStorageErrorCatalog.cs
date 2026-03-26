namespace FileStorage.Application.Common.Errors;

/// <summary>
/// Central catalog of structured error codes for the FileStorage microservice.
/// </summary>
public static class FileStorageErrorCatalog
{
    public static class Files
    {
        public const string NotFound = "FILE-0404";
        public const string InvalidFileType = "FILE-0400-TYPE";
        public const string FileTooLarge = "FILE-0400-SIZE";
    }
}
