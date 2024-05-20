using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

namespace PlatformPlatform.BackOffice.Infrastructure;

public sealed class BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options)
    : SharedKernelDbContext<BackOfficeDbContext>(options);
