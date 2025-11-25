using Bogus;
using Bogus.DataSets;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Tests;

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

        public long RandomId()
        {
            return IdGenerator.NewId();
        }
    }

    extension(Internet internet)
    {
        public string UniqueEmail()
        {
            return $"{internet.UserName()}@{internet.Random.AlphaNumeric(16)}.com";
        }

        public string InvalidEmail()
        {
            return internet.ExampleEmail(internet.Random.AlphaNumeric(100));
        }
    }
}
