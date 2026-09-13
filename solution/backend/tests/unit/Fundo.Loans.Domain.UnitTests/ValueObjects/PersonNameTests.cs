using Fundo.Loans.Domain;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.ValueObjects;

public class PersonNameTests
{
    [Fact]
    public void Create_TrimsWhitespace()
    {
        var name = PersonName.Create("  Jane  ", "  Doe  ");

        Assert.Equal("Jane", name.First);
        Assert.Equal("Doe", name.Last);
    }

    [Fact]
    public void Create_RejectsEmptyFirstOrLast()
    {
        Assert.Throws<DomainException>(() => PersonName.Create("", "Doe"));
        Assert.Throws<DomainException>(() => PersonName.Create("Jane", "   "));
    }

    [Fact]
    public void Create_RejectsNamesLongerThan100Characters()
    {
        var tooLong = new string('a', 101);

        Assert.Throws<DomainException>(() => PersonName.Create(tooLong, "Doe"));
    }

    [Fact]
    public void ToString_JoinsFirstAndLast()
    {
        var name = PersonName.Create("Jane", "Doe");

        Assert.Equal("Jane Doe", name.ToString());
    }
}
