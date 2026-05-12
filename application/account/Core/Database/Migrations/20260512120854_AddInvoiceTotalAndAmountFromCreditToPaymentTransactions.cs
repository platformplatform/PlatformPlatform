using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

// Adds InvoiceTotal and AmountFromCredit to every element in the subscriptions.payment_transactions
// jsonb array, and tightens the existing amounts-non-negative CHECK to also require these two new
// fields to be non-negative numbers. Existing rows are backfilled with a best-effort guess that
// preserves the AmountExcludingTax + TaxAmount invariant: InvoiceTotal = AmountExcludingTax +
// TaxAmount, AmountFromCredit = 0. Going forward, the StripeClient producer reads the real values
// off the Stripe invoice and the back-office LTV math sums InvoiceTotal so credit-absorbed invoices
// (e.g. proration on a mid-period upgrade after a credit note) are no longer silently undercounted.
[DbContext(typeof(AccountDbContext))]
[Migration("20260512120854_AddInvoiceTotalAndAmountFromCreditToPaymentTransactions")]
public sealed class AddInvoiceTotalAndAmountFromCreditToPaymentTransactions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE subscriptions
            SET payment_transactions = (
                SELECT jsonb_agg(
                    element || jsonb_build_object(
                        'InvoiceTotal', COALESCE((element->>'AmountExcludingTax')::numeric, 0) + COALESCE((element->>'TaxAmount')::numeric, 0),
                        'AmountFromCredit', 0
                    )
                )
                FROM jsonb_array_elements(payment_transactions) AS element
            )
            WHERE jsonb_array_length(payment_transactions) > 0;
            """
        );

        migrationBuilder.Sql("ALTER TABLE subscriptions DROP CONSTRAINT IF EXISTS chk_subscriptions_payment_transactions_amounts_non_negative;");
        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_payment_transactions_amounts_non_negative",
            "subscriptions",
            """NOT jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number") || !(@.InvoiceTotal.type() == "number") || !(@.AmountFromCredit.type() == "number") || @.AmountExcludingTax < 0 || @.TaxAmount < 0 || @.InvoiceTotal < 0 || @.AmountFromCredit < 0)')"""
        );
    }
}
