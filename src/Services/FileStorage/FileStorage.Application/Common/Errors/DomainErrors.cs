using ErrorOr;

namespace FileStorage.Application.Common.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for the FileStorage bounded context.
/// </summary>
public static class DomainErrors
{
    public static class Files
    {
        public static Error NotFound(string fileName) =>
            Error.NotFound(
                code: FileStorageErrorCatalog.Files.NotFound,
                description: $"File '{fileName}' not found."
            );

        public static Error InvalidFileType(string extension) =>
            Error.Validation(
                code: FileStorageErrorCatalog.Files.InvalidFileType,
                description: $"File type '{extension}' is not allowed."
            );

        public static Error FileTooLarge(long maxSize) =>
            Error.Validation(
                code: FileStorageErrorCatalog.Files.FileTooLarge,
                description: $"File exceeds maximum size of {maxSize} bytes."
            );
    }
}
