using Rentals.Billing.Application.Payments;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Loans;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Microsoft.Extensions.DependencyInjection;
using Rentals.SharedKernel;

namespace Rentals.Billing.Tests;

public sealed class HandlerTests
{
    private static readonly Money DailyRate = new(4m, "EUR");

    [Fact]
    public async Task Registration_opens_one_account()
    {
        await using var billing = await BillingHarness.StartAsync();

        await billing.DeliverAsync(new MemberRegisteredIntegrationEvent(BillingHarness.MemberId, "Ada Lovelace", "ada@example.org", BillingHarness.Now));

        var account = await billing.AccountAsync();
        Assert.Equal(1, account.Version);
        Assert.Equal(0m, account.Outstanding);
    }

    [Fact]
    public async Task A_new_loan_holds_a_fifth_of_the_replacement_value()
    {
        await using var billing = await BillingHarness.StartAsync();
        var loanId = Guid.NewGuid();

        await billing.DeliverAsync(new LoanOpenedIntegrationEvent(loanId, BillingHarness.MemberId, Guid.NewGuid(), BillingHarness.Now.AddDays(7), BillingHarness.Now));

        var account = await billing.AccountAsync();
        Assert.Equal(new Money(36m, "EUR"), account.HeldDeposits[new(loanId)]);
        Assert.Equal(1, billing.Catalogue.Calls);
    }

    [Fact]
    public async Task A_redelivered_loan_opened_is_skipped()
    {
        await using var billing = await BillingHarness.StartAsync();
        var opened = new LoanOpenedIntegrationEvent(Guid.NewGuid(), BillingHarness.MemberId, Guid.NewGuid(), BillingHarness.Now.AddDays(7), BillingHarness.Now);
        var messageId = Guid.NewGuid();

        await billing.DeliverAsync(opened, messageId);
        await billing.DeliverAsync(opened, messageId);

        Assert.Equal(1, billing.Catalogue.Calls);
        Assert.Equal(2, (await billing.AccountAsync()).Version);
    }

    [Fact]
    public async Task A_late_return_releases_the_deposit_and_charges_the_fee_once_per_message()
    {
        await using var billing = await BillingHarness.StartAsync();
        var loanId = Guid.NewGuid();
        var due = BillingHarness.Now.AddDays(7);
        await billing.DeliverAsync(new LoanOpenedIntegrationEvent(loanId, BillingHarness.MemberId, Guid.NewGuid(), due, BillingHarness.Now));
        var returned = new LoanReturnedIntegrationEvent(loanId, BillingHarness.MemberId, due, due.AddDays(2).AddHours(1), DailyRate, due.AddDays(2));
        var messageId = Guid.NewGuid();

        await billing.DeliverAsync(returned, messageId);
        await billing.DeliverAsync(returned, messageId);

        var account = await billing.AccountAsync();
        Assert.Empty(account.HeldDeposits);
        Assert.Equal(18m, account.Outstanding);
    }

    [Fact]
    public async Task An_on_time_return_charges_nothing()
    {
        await using var billing = await BillingHarness.StartAsync();
        var due = BillingHarness.Now.AddDays(7);

        await billing.DeliverAsync(new LoanReturnedIntegrationEvent(Guid.NewGuid(), BillingHarness.MemberId, due, due.AddHours(-2), DailyRate, due));

        Assert.Equal(0m, (await billing.AccountAsync()).Outstanding);
    }

    [Fact]
    public async Task A_lost_unit_is_charged_its_replacement_value()
    {
        await using var billing = await BillingHarness.StartAsync();

        await billing.DeliverAsync(new EquipmentDamageReportedIntegrationEvent(
            Guid.NewGuid(), BillingHarness.MemberId, Guid.NewGuid(), Guid.NewGuid(), UnitCondition.Lost, new Money(180m, "EUR"), BillingHarness.Now));

        Assert.Equal(180m, (await billing.AccountAsync()).Outstanding);
    }

    [Fact]
    public async Task A_damaged_unit_is_charged_half_its_replacement_value()
    {
        await using var billing = await BillingHarness.StartAsync();

        await billing.DeliverAsync(new EquipmentDamageReportedIntegrationEvent(
            Guid.NewGuid(), BillingHarness.MemberId, Guid.NewGuid(), Guid.NewGuid(), UnitCondition.Damaged, new Money(180m, "EUR"), BillingHarness.Now));

        Assert.Equal(90m, (await billing.AccountAsync()).Outstanding);
    }

    [Fact]
    public async Task A_suspension_requests_a_final_statement()
    {
        await using var billing = await BillingHarness.StartAsync();

        await billing.DeliverAsync(new MemberSuspendedIntegrationEvent(BillingHarness.MemberId, "Unpaid fees", BillingHarness.Now));

        var bus = billing.Services.GetRequiredService<Rentals.Messaging.InMemoryMessageBus>();
        Assert.Single(bus.Pending("billing-statements"));
    }

    [Fact]
    public async Task An_extension_is_charged_once_per_new_due_date()
    {
        await using var billing = await BillingHarness.StartAsync();
        var command = new ExtendLoanCommand(LoanId.New(), new MemberId(BillingHarness.MemberId), BillingHarness.Now.AddDays(10));

        await billing.SendAsync(command);
        await billing.SendAsync(command);

        Assert.Equal(2m, (await billing.AccountAsync()).Outstanding);
    }

    [Fact]
    public async Task A_payment_is_recorded_once_per_reference()
    {
        await using var billing = await BillingHarness.StartAsync();
        await billing.DeliverAsync(new EquipmentDamageReportedIntegrationEvent(
            Guid.NewGuid(), BillingHarness.MemberId, Guid.NewGuid(), Guid.NewGuid(), UnitCondition.Lost, new Money(180m, "EUR"), BillingHarness.Now));
        var payment = new RecordPaymentCommand(BillingHarness.MemberId, 100m, "EUR", "PAY-7");

        await billing.SendAsync(payment);
        await billing.SendAsync(payment);

        Assert.Equal(80m, (await billing.AccountAsync()).Outstanding);
    }
}
