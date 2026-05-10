using System.Net;
using System.Net.Http.Headers;
using Account.Database;
using SharedKernel.Tests;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class UpdateTenantLogoTests : EndpointBaseTest<AccountDbContext>
{
    public UpdateTenantLogoTests()
    {
        // Set up blob storage URL for tests - tests won't actually upload files
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
    }

    [Fact]
    public async Task UpdateTenantLogo_WhenMemberUser_ShouldReturnForbidden()
    {
        // Arrange
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var formData = new MultipartFormDataContent();
        formData.Add(fileContent, "file", "logo.png");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/tenants/current/update-logo", formData);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to update tenant logo.");
    }

    [Fact]
    public async Task UpdateTenantLogo_WhenSvgContentType_ShouldReturnBadRequest()
    {
        // Arrange
        var fileContent = new ByteArrayContent("<svg"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");

        var formData = new MultipartFormDataContent();
        formData.Add(fileContent, "file", "logo.svg");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/tenants/current/update-logo", formData);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("contentType", "Image must be of type JPEG, PNG, GIF, or WebP.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
