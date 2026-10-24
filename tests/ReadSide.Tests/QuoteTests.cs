namespace ReadSide.Tests;

public class QuoteTests
{
    [Fact]
    public void Name_is_wrapped_in_square_brackets()
    {
        Assert.Equal("[Order]", SqlTranslator.Quote("Order"));
    }

    [Fact]
    public void Closing_bracket_inside_a_name_is_doubled()
    {
        Assert.Equal("[Odd]]Name]", SqlTranslator.Quote("Odd]Name"));
    }
}
