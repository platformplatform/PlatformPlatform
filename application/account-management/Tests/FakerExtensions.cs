using Bogus;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Tests;

public static class FakerExtensions
{
    extension(Faker faker)
    {
        public string TenantName()
        {
            return new string(faker.Company.CompanyName().Take(30).ToArray());
        }

        public string PhoneNumber()
        {
            var random = new Random();
            return $"+{random.Next(1, 9)}-{faker.Phone.PhoneNumberFormat()}";
        }

        public string InvalidEmail()
        {
            return faker.Internet.ExampleEmail(faker.Random.AlphaNumeric(100));
        }

        public long RandomId()
        {
            return IdGenerator.NewId();
        }
    }
}
