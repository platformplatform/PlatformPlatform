using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Database;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<AccountDbContext>(options, executionContext, timeProvider);
