using Bogus;

namespace PlatformPlatform.AccountManagement.Tests;

public static class FakerExtensions
{
    public static string TenantName(this Faker faker)
    {
        return new string(faker.Company.CompanyName().Take(30).ToArray());
    }

    public static string PhoneNumber(this Faker faker)
    {
        var random = new Random();
        return $"+{random.Next(1, 9)}-{faker.Phone.PhoneNumberFormat()}";
    }

    public static string Subdomain(this Faker faker)
    {
        return faker.Random.AlphaNumeric(10);
    }

    public static string InvalidEmail(this Faker faker)
    {
        return faker.Internet.ExampleEmail(faker.Random.AlphaNumeric(100));
    }
}