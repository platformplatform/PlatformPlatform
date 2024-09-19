using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.BackOffice.Database;

public sealed class BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<BackOfficeDbContext>(options, executionContext);
