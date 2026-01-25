using System.Net;
using System.Net.Http.Headers;
using System.Text;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Tenants;

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
        var svgContent = "<svg width='58' height='57' viewBox='0 0 58 57' fill='none' xmlns='http://www.w3.org/2000/svg'><rect width='58' height='57' fill='#FF6620'/></svg>";
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(svgContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");

        var formData = new MultipartFormDataContent();
        formData.Add(fileContent, "file", "logo.svg");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/tenants/current/update-logo", formData);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to update tenant logo.");
    }
}
