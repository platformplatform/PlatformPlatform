using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Database;

public sealed class AccountManagementDbContext(DbContextOptions<AccountManagementDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<AccountManagementDbContext>(options, executionContext);
