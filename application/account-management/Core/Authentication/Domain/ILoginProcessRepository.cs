using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Core.Authentication.Domain;

public interface ILoginRepository : ICrudRepository<Login, LoginId>;
