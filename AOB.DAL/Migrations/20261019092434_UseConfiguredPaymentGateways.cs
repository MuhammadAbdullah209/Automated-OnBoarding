using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <summary>
    /// Switches projects from the fixed in-code provider and field lists to the payment gateways
    /// configured in the database (PaymentGateways / PaymentGatewayFields).
    ///
    /// The scaffolded version of this migration dropped the old columns first, which would have wiped
    /// existing projects. It has been rewritten to add the new columns, convert the existing rows, and
    /// only then drop the old columns:
    ///  - ProjectPaymentConfigurations.PaymentProvider (enum name) becomes PaymentGatewayId, matched to the
    ///    gateway with the same name (Stripe, ...). Providers that have no configured gateway become NULL.
    ///  - ProjectGatewayFields.Field (enum name) becomes Name, using the same labels the screens showed
    ///    (e.g. TransactionAmount becomes "Transaction Amount"). PaymentGatewayFieldId is NULL for these
    ///    older rows, which keeps them readable (submitted projects still show their fields).
    /// </summary>
    /// <inheritdoc />
    public partial class UseConfiguredPaymentGateways : Migration
    {
        // The labels the screens used for the old fixed field list: enum name -> label.
        private static readonly (string Enum, string Label)[] OldFieldLabels =
        {
            ("MerchantReference", "Merchant Reference"),
            ("TransactionAmount", "Transaction Amount"),
            ("CurrencyCode", "Currency Code"),
            ("CustomerReference", "Customer Reference"),
            ("OrderDescription", "Order Description"),
            ("BillingPostcode", "Billing Postcode"),
            ("CustomerEmail", "Customer Email"),
            ("AdditionalDataField", "Additional Data Field"),
            ("TransactionId", "Transaction ID"),
            ("AuthorisationCode", "Authorisation Code"),
            ("PaymentStatus", "Payment Status"),
            ("CardScheme", "Card Scheme"),
            ("MaskedCardNumber", "Masked Card Number"),
            ("TransactionDateTime", "Transaction Date / Time"),
            ("ResponseMessage", "Response Message")
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectGatewayFields_ProjectId_Field_FieldType_IsInput",
                table: "ProjectGatewayFields");

            // 1) Add the new columns first (the old ones are still there).
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGatewayId",
                table: "ProjectPaymentConfigurations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProjectGatewayFields",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGatewayFieldId",
                table: "ProjectGatewayFields",
                type: "uniqueidentifier",
                nullable: true);

            // 2) Convert the existing rows.
            migrationBuilder.Sql(@"
UPDATE c
SET c.PaymentGatewayId = g.Id
FROM ProjectPaymentConfigurations c
INNER JOIN PaymentGateways g
    ON g.Name = CASE c.PaymentProvider WHEN 'AuthorizeNet' THEN 'Authorize.Net' ELSE c.PaymentProvider END
WHERE c.PaymentProvider IS NOT NULL;");

            var toLabel = string.Join(" ", Array.ConvertAll(OldFieldLabels, f => $"WHEN '{f.Enum}' THEN '{f.Label}'"));
            migrationBuilder.Sql($"UPDATE ProjectGatewayFields SET Name = CASE Field {toLabel} ELSE Field END;");

            // 3) Now the old columns can go.
            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "ProjectPaymentConfigurations");

            migrationBuilder.DropColumn(
                name: "Field",
                table: "ProjectGatewayFields");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPaymentConfigurations_PaymentGatewayId",
                table: "ProjectPaymentConfigurations",
                column: "PaymentGatewayId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGatewayFields_PaymentGatewayFieldId",
                table: "ProjectGatewayFields",
                column: "PaymentGatewayFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGatewayFields_ProjectId_Name_FieldType_IsInput",
                table: "ProjectGatewayFields",
                columns: new[] { "ProjectId", "Name", "FieldType", "IsInput" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectGatewayFields_PaymentGatewayFields_PaymentGatewayFieldId",
                table: "ProjectGatewayFields",
                column: "PaymentGatewayFieldId",
                principalTable: "PaymentGatewayFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPaymentConfigurations_PaymentGateways_PaymentGatewayId",
                table: "ProjectPaymentConfigurations",
                column: "PaymentGatewayId",
                principalTable: "PaymentGateways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <summary>
        /// Best-effort rollback to the fixed lists: only rows that can be expressed with the old enums survive
        /// (field rows whose name is not one of the old labels cannot be represented and are removed).
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectGatewayFields_PaymentGatewayFields_PaymentGatewayFieldId",
                table: "ProjectGatewayFields");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPaymentConfigurations_PaymentGateways_PaymentGatewayId",
                table: "ProjectPaymentConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPaymentConfigurations_PaymentGatewayId",
                table: "ProjectPaymentConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectGatewayFields_PaymentGatewayFieldId",
                table: "ProjectGatewayFields");

            migrationBuilder.DropIndex(
                name: "IX_ProjectGatewayFields_ProjectId_Name_FieldType_IsInput",
                table: "ProjectGatewayFields");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                table: "ProjectPaymentConfigurations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Field",
                table: "ProjectGatewayFields",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE c
SET c.PaymentProvider = CASE
        WHEN g.Name = 'Authorize.Net' THEN 'AuthorizeNet'
        WHEN g.Name IN ('Stripe', 'Adyen', 'Worldpay', 'Barclaycard', 'CyberSource', 'Elavon') THEN g.Name
        ELSE 'Other' END
FROM ProjectPaymentConfigurations c
INNER JOIN PaymentGateways g ON g.Id = c.PaymentGatewayId;");

            var knownLabels = string.Join(", ", Array.ConvertAll(OldFieldLabels, f => $"'{f.Label}'"));
            migrationBuilder.Sql($"DELETE FROM ProjectGatewayFields WHERE Name NOT IN ({knownLabels});");

            var toEnum = string.Join(" ", Array.ConvertAll(OldFieldLabels, f => $"WHEN '{f.Label}' THEN '{f.Enum}'"));
            migrationBuilder.Sql($"UPDATE ProjectGatewayFields SET Field = CASE Name {toEnum} ELSE Name END;");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayId",
                table: "ProjectPaymentConfigurations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProjectGatewayFields");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayFieldId",
                table: "ProjectGatewayFields");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGatewayFields_ProjectId_Field_FieldType_IsInput",
                table: "ProjectGatewayFields",
                columns: new[] { "ProjectId", "Field", "FieldType", "IsInput" },
                unique: true);
        }
    }
}
