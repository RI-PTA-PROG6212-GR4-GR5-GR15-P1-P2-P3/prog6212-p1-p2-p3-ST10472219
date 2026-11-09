using System.Text;
using Microsoft.AspNetCore.Http;
using RaceDay.Api;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class MediaTests
{
    private sealed class FakeBlobStorage : IBlobStorage
    {
        public string? Container { get; private set; }

        public Task<string> UploadAsync(IFormFile file, string container, string namePrefix)
        {
            Container = container;
            return Task.FromResult($"https://storage.example/{container}/{namePrefix}.png");
        }

        public Task<BlobReadResult?> DownloadAsync(string container, string blobUri)
        {
            Container = container;
            return Task.FromResult<BlobReadResult?>(
                new BlobReadResult(new MemoryStream(Encoding.UTF8.GetBytes("image")), "image/png"));
        }
    }

    private static FormFile MakeFile(Stream stream, string fileName, string contentType) =>
        new(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    [Fact]
    public async Task BannerUpload_OwnerStoresBlobReference()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var storage = new FakeBlobStorage();
        var controller = new MediaController(db, storage).WithSession(organiser.OrganiserID, Roles.Organiser);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("image"));
        var file = MakeFile(stream, "banner.png", "image/png");

        var result = await controller.Banner(ev.EventID, new ImageUploadRequest { File = file });

        Assert.Equal(200, Status(result));
        Assert.Equal("event-banners", storage.Container);
        Assert.Contains("event-banners", db.Events.Single().BannerImageUrl);
    }

    [Fact]
    public async Task BannerUpload_RejectsUnsupportedContentType()
    {
        using var db = NewDb();
        var organiser = AddOrganiser(db);
        var ev = AddEvent(db, organiser);
        var controller = new MediaController(db, new FakeBlobStorage()).WithSession(organiser.OrganiserID, Roles.Organiser);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));
        var file = MakeFile(stream, "payload.exe", "application/octet-stream");

        Assert.Equal(400, Status(await controller.Banner(ev.EventID, new ImageUploadRequest { File = file })));
    }

    [Fact]
    public async Task BannerUpload_RejectsAnotherOrganisersEvent()
    {
        using var db = NewDb();
        var owner = AddOrganiser(db, "owner@example.com");
        var other = AddOrganiser(db, "other@example.com");
        var ev = AddEvent(db, owner);
        var controller = new MediaController(db, new FakeBlobStorage()).WithSession(other.OrganiserID, Roles.Organiser);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("image"));
        var file = MakeFile(stream, "banner.png", "image/png");

        Assert.Equal(403, Status(await controller.Banner(ev.EventID, new ImageUploadRequest { File = file })));
    }

    [Fact]
    public async Task ProfilePictureUploadAndRead_UsesApiBlobService()
    {
        using var db = NewDb();
        var participant = AddParticipant(db);
        var storage = new FakeBlobStorage();
        var controller = new ProfileController(db).WithSession(participant.ParticipantID, Roles.Participant);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("image"));
        var file = MakeFile(stream, "profile.png", "image/png");

        var uploaded = await controller.UploadPicture(new ImageUploadRequest { File = file }, storage);
        Assert.Equal(200, Status(uploaded));
        Assert.Contains("profiles", db.Participants.Single().ProfilePictureUrl);

        var image = await controller.GetPicture(storage);
        Assert.IsType<Microsoft.AspNetCore.Mvc.FileStreamResult>(image);
        Assert.Equal("profiles", storage.Container);
    }
}
