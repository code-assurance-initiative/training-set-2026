using HarbourLane.Bookings.Web.Components.Shared;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class ConfirmDialogTests : BunitContext
{
    private bool? _answer;

    [Fact]
    public void Nothing_is_rendered_while_closed()
    {
        var cut = RenderDialog(open: false);

        Assert.Empty(cut.FindAll("[role=dialog]"));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void The_buttons_answer_the_question(int button, bool expected)
    {
        var cut = RenderDialog(open: true);

        cut.FindAll(".dialog-actions button")[button].Click();

        Assert.Equal(expected, _answer);
    }

    [Fact]
    public void The_dialog_is_named_by_its_title()
    {
        var cut = RenderDialog(open: true);

        var labelledBy = cut.Find("[role=dialog]").GetAttribute("aria-labelledby");
        Assert.Equal("Cancel this booking?", cut.Find($"#{labelledBy}").TextContent);
    }

    private IRenderedComponent<ConfirmDialog> RenderDialog(bool open) =>
        Render<ConfirmDialog>(p => p
            .Add(c => c.Open, open)
            .Add(c => c.Title, "Cancel this booking?")
            .Add(c => c.Message, "The room will be released.")
            .Add(c => c.ConfirmText, "Cancel booking")
            .Add(c => c.Closed, answer => _answer = answer));
}
