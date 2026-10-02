using BaseTestingLab.Core.Collections;
using BaseTestingLab.Core.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.Collections;

public class CollectionHelperTests
{
    private record Product(int Id, string Name, decimal Price);

    [Fact]
    public void ToPagedResult_ShouldComputePaginationMetadataAccurately()
    {
        var products = Enumerable.Range(1, 15)
            .Select(i => new Product(i, $"Producto {i}", i * 10m))
            .ToList();

        var paged = CollectionHelper.ToPagedResult(products, pageNumber: 2, pageSize: 5);

        paged.Items.Should().HaveCount(5);
        paged.Items.First().Id.Should().Be(6);
        paged.Items.Last().Id.Should().Be(10);
        paged.PageNumber.Should().Be(2);
        paged.PageSize.Should().Be(5);
        paged.TotalItems.Should().Be(15);
        paged.TotalPages.Should().Be(3);
        paged.HasPreviousPage.Should().BeTrue();
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ToPagedResult_WhenInvalidPage_ShouldThrowValidationException()
    {
        var items = new[] { 1, 2, 3 };

        Action act = () => CollectionHelper.ToPagedResult(items, pageNumber: 0, pageSize: 10);

        act.Should().Throw<ValidationException>()
           .WithMessage("*El número de página debe ser mayor o igual a 1*");
    }

    [Fact]
    public void ChunkBySize_ShouldDivideCollectionIntoExactBatches()
    {
        var source = Enumerable.Range(1, 10).ToList();
        var chunks = CollectionHelper.ChunkBySize(source, chunkSize: 3).ToList();

        chunks.Should().HaveCount(4);
        chunks[0].Should().Equal(1, 2, 3);
        chunks[1].Should().Equal(4, 5, 6);
        chunks[2].Should().Equal(7, 8, 9);
        chunks[3].Should().Equal(10);
    }

    [Fact]
    public void SortByProperty_AscendingAndDescending_ShouldOrderCorrectly()
    {
        var products = new List<Product>
        {
            new(1, "Monitor", 300m),
            new(2, "Teclado", 50m),
            new(3, "Laptop", 1200m)
        };

        var asc = CollectionHelper.SortByProperty(products, "Price", ascending: true).ToList();
        var desc = CollectionHelper.SortByProperty(products, "Name", ascending: false).ToList();

        asc.Should().BeInAscendingOrder(p => p.Price);
        asc.First().Name.Should().Be("Teclado");

        desc.Should().BeInDescendingOrder(p => p.Name);
        desc.First().Name.Should().Be("Teclado");
    }

    [Fact]
    public void FindDuplicates_ShouldIdentifyRepeatedElements()
    {
        var emails = new[]
        {
            "maria@test.com",
            "carlos@test.com",
            "maria@test.com",
            "juan@test.com",
            "carlos@test.com"
        };

        var duplicates = CollectionHelper.FindDuplicates(emails, e => e).ToList();

        duplicates.Should().HaveCount(2)
                  .And.Contain("maria@test.com")
                  .And.Contain("carlos@test.com")
                  .And.NotContain("juan@test.com");
    }
}
