using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.BackOffice.Core.Database;

public sealed class BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options)
    : SharedKernelDbContext<BackOfficeDbContext>(options);
