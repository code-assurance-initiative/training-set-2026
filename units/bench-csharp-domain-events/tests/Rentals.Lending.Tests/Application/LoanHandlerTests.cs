using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Catalogue;
using Rentals.Lending.Application.Loans;
using Rentals.Lending.Application.Members;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Maintenance;
using Rentals.Lending.Infrastructure.Messaging;
using Rentals.Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Application;

public sealed class LoanHandlerTests
{
    [Fact]
    public async Task Checkout_opens_a_loan_and_writes_loan_opened_to_the_outbox()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();

        await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7));

        var open = await lending.InScopeAsync<ILoanRepository, IReadOnlyList<Loan>>(loans =>
            loans.ListOpenForMemberAsync(member.Id, TestContext.Current.CancellationToken));
        Assert.Equal(LendingHarness.Now.AddDays(7), Assert.Single(open).DueAt);
        Assert.Contains(nameof(LoanOpenedIntegrationEvent), await lending.OutboxTypesAsync());
    }

    [Fact]
    public async Task A_unit_on_loan_cannot_be_checked_out_again()
    {
        await using var lending = await LendingHarness.StartAsync();
        var ada = await lending.RegisterMemberAsync();
        var grace = await lending.RegisterMemberAsync("grace@example.org");
        var drill = await lending.RegisterDrillAsync();
        var unit = drill.Units.Single().Id;
        await lending.SendAsync(new CheckoutEquipmentCommand(ada.Id, unit, 7));

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.SendAsync(new CheckoutEquipmentCommand(grace.Id, unit, 7)));
    }

    [Fact]
    public async Task A_suspended_member_cannot_borrow()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        await lending.SendAsync(new SuspendMemberCommand(member.Id, "Unpaid fees"));

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7)));
    }

    [Fact]
    public async Task The_loan_limit_applies()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync("DR-001", "DR-002", "DR-003");
        foreach (var unit in drill.Units.Take(2))
        {
            await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, unit.Id, 7));
        }

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Last().Id, 7)));
    }

    [Fact]
    public async Task Returning_damaged_publishes_return_and_damage()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7));
        var loan = (await lending.InScopeAsync<ILoanRepository, IReadOnlyList<Loan>>(loans =>
            loans.ListOpenAsync(TestContext.Current.CancellationToken))).Single();

        await lending.SendAsync(new ReturnEquipmentCommand(loan.Id, UnitCondition.Damaged));
        await lending.SendAsync(new ReturnEquipmentCommand(loan.Id, UnitCondition.Damaged));

        var types = await lending.OutboxTypesAsync();
        Assert.Single(types, nameof(LoanReturnedIntegrationEvent));
        Assert.Single(types, nameof(EquipmentDamageReportedIntegrationEvent));
    }

    [Fact]
    public async Task Extending_moves_the_due_date()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7));
        var loan = (await lending.InScopeAsync<ILoanRepository, IReadOnlyList<Loan>>(loans =>
            loans.ListOpenAsync(TestContext.Current.CancellationToken))).Single();

        await lending.SendAsync(new ExtendLoanCommand(loan.Id, member.Id, loan.DueAt.AddDays(3)));

        var extended = await lending.InScopeAsync<ILoanRepository, Loan?>(loans =>
            loans.GetAsync(loan.Id, TestContext.Current.CancellationToken));
        Assert.Equal(loan.DueAt.AddDays(3), extended?.DueAt);
    }

    [Fact]
    public async Task Overdue_loans_are_those_past_their_due_date()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7));

        lending.Clock.Advance(TimeSpan.FromDays(8));
        var overdue = await lending.QueryAsync<OverdueLoansQuery, IReadOnlyList<OverdueLoan>>(new OverdueLoansQuery());

        Assert.Equal(member.Id.Value, Assert.Single(overdue).MemberId);
    }

    [Fact]
    public async Task Damage_reported_marks_the_unit_and_is_safe_to_repeat()
    {
        await using var lending = await LendingHarness.StartAsync();
        var drill = await lending.RegisterDrillAsync();
        var unit = drill.Units.Single();
        var reported = new EquipmentDamageReportedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), drill.Id.Value, unit.Id.Value, UnitCondition.Damaged, drill.ReplacementValue,
            LendingHarness.Now);
        var context = new Rentals.Messaging.MessageContext(Guid.NewGuid(), nameof(EquipmentDamageReportedIntegrationEvent), LendingHarness.Now);

        for (var delivery = 0; delivery < 2; delivery++)
        {
            await lending.InScopeAsync<Rentals.Messaging.IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>>(handler =>
                handler.HandleAsync(reported, context, TestContext.Current.CancellationToken));
        }

        var stored = await lending.InScopeAsync<IEquipmentRepository, Equipment?>(equipment =>
            equipment.GetAsync(drill.Id, TestContext.Current.CancellationToken));
        Assert.Equal(UnitCondition.Damaged, stored?.Units.Single().Condition);
    }

    [Fact]
    public async Task Maintenance_is_recorded_once_per_record_id()
    {
        await using var lending = await LendingHarness.StartAsync();
        var drill = await lending.RegisterDrillAsync();
        var command = new RecordMaintenanceCommand(MaintenanceRecordId.New(), drill.Units.Single().Id, "Replace chuck", 12m);

        await lending.SendAsync(command);
        await lending.SendAsync(command);

        var records = await lending.InScopeAsync<IMaintenanceRecordRepository, IReadOnlyList<MaintenanceRecord>>(r =>
            r.ListForUnitAsync(command.UnitId, TestContext.Current.CancellationToken));
        Assert.Single(records);
    }

    [Fact]
    public async Task The_dispatcher_publishes_and_empties_the_outbox()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        await lending.SendAsync(new CheckoutEquipmentCommand(member.Id, drill.Units.Single().Id, 7));

        var published = await lending.Services.GetRequiredService<OutboxDispatcher>()
            .DispatchPendingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, published);
        Assert.Empty(await lending.OutboxTypesAsync());
        var (opened, context) = Assert.Single(lending.Published.Received);
        Assert.IsType<LoanOpenedIntegrationEvent>(opened);
        Assert.NotEqual(Guid.Empty, context.MessageId);
    }

    [Fact]
    public async Task The_database_refuses_a_second_open_loan_for_a_unit()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        var unit = drill.Units.Single().Id;

        async Task OpenAsync() => await lending.InScopeAsync<LendingDbContext>(async db =>
        {
            var borrower = await db.Members.SingleAsync(m => m.Id == member.Id, TestContext.Current.CancellationToken);
            db.Loans.Add(Loan.Open(borrower, drill.Id, unit, LoanPeriod.Starting(LendingHarness.Now, 7)));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await OpenAsync();
        await Assert.ThrowsAsync<DbUpdateException>(OpenAsync);
    }
}
