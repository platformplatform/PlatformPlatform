using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;

namespace Main.Database;

public sealed class MainDbContext(DbContextOptions<MainDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<MainDbContext>(options, executionContext, timeProvider);
