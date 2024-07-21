using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Authentication;

public interface ILoginRepository : ICrudRepository<Login, LoginId>;
