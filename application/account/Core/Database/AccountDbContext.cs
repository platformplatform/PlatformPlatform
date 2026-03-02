using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;

namespace Account.Database;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<AccountDbContext>(options, executionContext, timeProvider);
