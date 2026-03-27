using SMEFinanceSuite.Core.Domain.Entities;
using Xunit;

namespace SMEFinanceSuite.Tests.Unit.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void Constructor_ShouldCreateCustomer_WhenNameIsValid()
    {
        Customer customer = new("Cliente Teste", "teste@empresa.com", "41999999999");

        Assert.Equal("Cliente Teste", customer.Name);
        Assert.Equal("teste@empresa.com", customer.Email);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => new Customer("   "));
    }
}
