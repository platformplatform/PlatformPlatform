using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260507205500_AddPaymentTransactionTaxBreakdownConstraint")]
public sealed class AddPaymentTransactionTaxBreakdownConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_payment_transactions_tax_breakdown",
            "subscriptions",
            """NOT jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number"))')"""
        );
    }
}
