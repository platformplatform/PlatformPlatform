using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Main.Database;

public sealed class MainDbContext(DbContextOptions<MainDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<MainDbContext>(options, executionContext, timeProvider);
