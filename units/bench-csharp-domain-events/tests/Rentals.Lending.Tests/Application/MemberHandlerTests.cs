using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Members;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Application;

public sealed class MemberHandlerTests
{
    [Fact]
    public async Task Registering_writes_the_member_and_its_integration_event_to_the_outbox()
    {
        await using var lending = await LendingHarness.StartAsync();

        var member = await lending.RegisterMemberAsync();

        Assert.Equal("Ada Lovelace", member.FullName);
        Assert.Equal([nameof(MemberRegisteredIntegrationEvent)], await lending.OutboxTypesAsync());
    }

    [Fact]
    public async Task Registering_again_with_the_same_details_is_accepted_once()
    {
        await using var lending = await LendingHarness.StartAsync();

        await lending.RegisterMemberAsync();
        await lending.RegisterMemberAsync();

        Assert.Single(await lending.OutboxTypesAsync());
    }

    [Fact]
    public async Task Another_person_cannot_register_with_a_taken_email()
    {
        await using var lending = await LendingHarness.StartAsync();
        await lending.RegisterMemberAsync();

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.SendAsync(new RegisterMemberCommand("Ada Byron", "ada@example.org", "")));
    }

    [Fact]
    public async Task Suspending_publishes_the_suspension()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();

        await lending.SendAsync(new SuspendMemberCommand(member.Id, "Unpaid fees"));

        var (published, _) = Assert.Single(lending.Published.Received);
        var suspended = Assert.IsType<MemberSuspendedIntegrationEvent>(published);
        Assert.Equal(member.Id.Value, suspended.MemberId);
    }

    [Fact]
    public async Task Erasing_personal_data_closes_the_member()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();

        await lending.SendAsync(new EraseMemberPersonalDataCommand(member.Id));

        var erased = await lending.InScopeAsync<IMemberRepository, Member?>(members =>
            members.GetAsync(member.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(erased);
        Assert.Equal(MemberStatus.Closed, erased.Status);
        Assert.Equal("(erased)", erased.FullName);
    }

    [Fact]
    public async Task Search_finds_members_by_name_fragment()
    {
        await using var lending = await LendingHarness.StartAsync();
        await lending.RegisterMemberAsync();

        var found = await lending.QueryAsync<SearchMembersQuery, IReadOnlyList<MemberSummary>>(new SearchMembersQuery("Love"));

        Assert.Equal("Ada Lovelace", Assert.Single(found).FullName);
    }

    [Fact]
    public async Task Renewing_extends_the_membership()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();

        await lending.SendAsync(new RenewMembershipCommand(member.Id));

        var renewed = await lending.InScopeAsync<IMemberRepository, Member?>(members =>
            members.GetAsync(member.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(renewed);
        Assert.True(renewed.MembershipExpiresAt > member.MembershipExpiresAt);
    }

    [Fact]
    public async Task Suspending_an_unknown_member_is_reported_as_not_found()
    {
        await using var lending = await LendingHarness.StartAsync();

        var error = await Assert.ThrowsAsync<Rentals.Lending.Application.NotFoundException>(() =>
            lending.SendAsync(new SuspendMemberCommand(MemberId.New(), "Unpaid fees")));
        Assert.Contains("does not exist", error.Message, StringComparison.Ordinal);
    }
}
