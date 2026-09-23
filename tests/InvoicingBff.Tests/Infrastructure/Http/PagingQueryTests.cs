using InvoicingBff.Infrastructure.Http;
using Shouldly;

namespace InvoicingBff.Tests.Infrastructure.Http;

public class PagingQueryTests
{
    [Fact]
    public void Create_WithOnlyPaging_EmitsPageAndPageSize()
    {
        PagingQuery.Create(3, 20).Value.ShouldBe("?page=3&pageSize=20");
    }

    [Fact]
    public void Create_AppendsNonNullFiltersInOrder()
    {
        var query = PagingQuery.Create(1, 50, ("clientId", "abc"), ("status", "Sent"));

        query.Value.ShouldBe("?page=1&pageSize=50&clientId=abc&status=Sent");
    }

    [Fact]
    public void Create_OmitsNullFilters()
    {
        var query = PagingQuery.Create(1, 50, ("clientId", null), ("status", "Paid"));

        query.Value.ShouldBe("?page=1&pageSize=50&status=Paid");
    }
}
