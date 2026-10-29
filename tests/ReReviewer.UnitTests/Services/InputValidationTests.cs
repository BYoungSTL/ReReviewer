using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;

namespace ReReviewer.UnitTests.Services;

public class InputValidationTests
{
    [Fact]
    public void RequiredText_TrimsValue() =>
        InputValidation.RequiredText("  RPG  ", "Genre title").ShouldBe("RPG");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredText_Empty_ThrowsWithFieldName(string? value) =>
        Should.Throw<ValidationException>(() => InputValidation.RequiredText(value, "Genre title"))
            .Message.ShouldBe("Genre title must not be empty.");

    [Theory]
    [InlineData("  text  ", "text")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void OptionalText_TrimsOrReturnsNull(string? value, string? expected) =>
        InputValidation.OptionalText(value).ShouldBe(expected);

    [Fact]
    public void RequiredIds_ReturnsIdsInOrder()
    {
        GenreViewModel[] genres = [new() { Id = 3 }, new() { Id = 1 }];

        InputValidation.RequiredIds(genres, g => g.Id, "Genre").ShouldBe([3, 1]);
    }

    [Fact]
    public void RequiredIds_ItemWithoutId_Throws()
    {
        GenreViewModel[] genres = [new() { Id = 3 }, new() { Title = "RPG" }];

        Should.Throw<ValidationException>(() => InputValidation.RequiredIds(genres, g => g.Id, "Genre"))
            .Message.ShouldBe("Genre id is required.");
    }
}