using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;

namespace BackOffice.Database;

public sealed class BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<BackOfficeDbContext>(options, executionContext, timeProvider);
