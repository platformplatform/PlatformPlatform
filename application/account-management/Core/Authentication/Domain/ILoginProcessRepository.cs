using PlatformPlatform.SharedKernel.Entities;

namespace PlatformPlatform.AccountManagement.Core.Authentication.Domain;

public interface ILoginRepository : ICrudRepository<Login, LoginId>;
