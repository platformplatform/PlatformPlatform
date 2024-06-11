using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AppHost;

public static class SslCertificateManager
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    private static string UserSecretsId =>
        Assembly.GetEntryAssembly()!.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;
    
    public static string CreateSslCertificateIfNotExists(this IDistributedApplicationBuilder builder)
    {
        var config = new ConfigurationBuilder().AddUserSecrets(UserSecretsId).Build();
        
        const string certificatePasswordKey = "certificate-password";
        var certificatePassword = config[certificatePasswordKey]
                                  ?? builder.CreateStablePassword(certificatePasswordKey).Resource.Value;
        
        var certificateLocation = GetLocalhostCertificateLocation();
        if (!IsValidCertificate(certificatePassword, certificateLocation))
        {
            CreateNewSelfSignedDeveloperCertificate(certificatePassword, certificateLocation);
        }
        
        return certificatePassword;
    }
    
    private static string GetLocalhostCertificateLocation()
    {
        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return $"{userFolder}/.aspnet/dev-certs/https/platformplatform.pfx";
    }
    
    private static bool IsValidCertificate(string? password, string certificateLocation)
    {
        if (!File.Exists(certificateLocation))
        {
            return false;
        }
        
        if (IsWindows)
        {
            try
            {
                // Try to load the certificate with the provided password
                _ = new X509Certificate2(certificateLocation, password);
                return true;
            }
            catch (CryptographicException)
            {
                // If a CryptographicException is thrown, the password is invalid
                // Ignore the exception and return false
            }
        }
        else
        {
            var certificateValidation = StartProcess(new ProcessStartInfo
                {
                    FileName = "openssl",
                    Arguments = $"pkcs12 -in {certificateLocation} -passin pass:{password} -nokeys",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            );
            
            if (certificateValidation.Contains("--BEGIN CERTIFICATE--"))
            {
                return true;
            }
        }
        
        Console.WriteLine($"Certificate {certificateLocation} exists, but password {password} was invalid. Creating a new certificate.");
        return false;
    }
    
    private static void CreateNewSelfSignedDeveloperCertificate(string password, string certificateLocation)
    {
        if (File.Exists(certificateLocation))
        {
            File.Delete(certificateLocation);
        }
        
        StartProcess(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"dev-certs https --trust -ep {certificateLocation} -p {password}",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            }
        );
    }
    
    private static string StartProcess(ProcessStartInfo processStartInfo)
    {
        var process = Process.Start(processStartInfo)!;
        
        var output = string.Empty;
        if (processStartInfo.RedirectStandardOutput) output += process.StandardOutput.ReadToEnd();
        if (processStartInfo.RedirectStandardError) output += process.StandardError.ReadToEnd();
        
        process.WaitForExit();
        
        return output;
    }
}
