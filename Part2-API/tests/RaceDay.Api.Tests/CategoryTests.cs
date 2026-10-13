using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class CategoryTests
{
    private static CategoryRequest NewCategory(int max = 100) => new()
    {
        CategoryName = "21km Half Marathon", DistanceKm = 21.1m, EntryFee = 200m, MaxParticipants = max
    };

    [Fact]
    public async Task Create_OnOwnEvent_Returns201()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Create(ev.EventID, NewCategory());

        Assert.Equal(201, Status(result));
        Assert.Equal("21km Half Marathon", Body<CategoryDto>(result).CategoryName);
        Assert.Equal(ev.EventID, db.Categories.Single().EventID);
    }

    [Fact]
    public async Task Create_OnAnotherOrganisersEvent_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var ev = AddEvent(db, owner);
        var controller = new CategoriesController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        var result = await controller.Create(ev.EventID, NewCategory());

        Assert.Equal(403, Status(result));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Create_UnknownEvent_Returns404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(404, Status(await controller.Create(999, NewCategory())));
    }

    [Fact]
    public async Task GetForEvent_ListsCategories_AndUnknownEventIs404()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        AddCategory(db, ev, "10km", 10m);
        AddCategory(db, ev, "5km", 5m);
        var controller = new CategoriesController(db).WithSession();

        var list = Body<List<CategoryDto>>(await controller.GetForEvent(ev.EventID));

        Assert.Equal(new[] { "5km", "10km" }, list.Select(c => c.CategoryName).ToArray());
        Assert.Equal(404, Status(await controller.GetForEvent(999)));
    }

    [Fact]
    public async Task GetById_ReturnsCategory_OrNotFound()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        var controller = new CategoriesController(db).WithSession();

        Assert.Equal(200, Status(await controller.GetById(category.CategoryID)));
        Assert.Equal(404, Status(await controller.GetById(999)));
    }

    [Fact]
    public async Task Update_OwnCategory_Returns200()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.Update(category.CategoryID, NewCategory(250));

        Assert.Equal(200, Status(result));
        Assert.Equal(250, db.Categories.Single().MaxParticipants);
    }

    [Fact]
    public async Task Update_AnotherOrganisersCategory_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var category = AddCategory(db, AddEvent(db, owner));
        var controller = new CategoriesController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        Assert.Equal(403, Status(await controller.Update(category.CategoryID, NewCategory())));
    }

    [Fact]
    public async Task Update_MaxParticipantsBelowCurrentEnrolments_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, AddParticipant(db, "a@test.com"), category, "BIB0001");
        AddEnrolment(db, AddParticipant(db, "b@test.com"), category, "BIB0002");
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(409, Status(await controller.Update(category.CategoryID, NewCategory(1))));
    }

    [Fact]
    public async Task Delete_OwnEmptyCategory_Returns204()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(204, Status(await controller.Delete(category.CategoryID)));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Delete_CategoryWithEnrolments_Returns409()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, AddParticipant(db), category);
        var controller = new CategoriesController(db).WithSession(organiser.OrganiserID, Roles.Organiser);

        Assert.Equal(409, Status(await controller.Delete(category.CategoryID)));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Delete_AnotherOrganisersCategory_Returns403()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@test.com");
        var intruder = AddOrganiser(db, "intruder@test.com");
        var category = AddCategory(db, AddEvent(db, owner));
        var controller = new CategoriesController(db).WithSession(intruder.OrganiserID, Roles.Organiser);

        Assert.Equal(403, Status(await controller.Delete(category.CategoryID)));
    }
}
