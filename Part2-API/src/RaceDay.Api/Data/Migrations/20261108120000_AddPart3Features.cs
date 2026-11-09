using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;

#nullable disable

namespace RaceDay.Api.Data.Migrations
{
    /// <summary>Part 3: event banners, published results, profile pictures and age/distance categories.</summary>
    [DbContext(typeof(RaceDayDbContext))]
    [Migration("20261108120000_AddPart3Features")]
    public partial class AddPart3Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl", table: "Event", type: "nvarchar(1000)", maxLength: 1000, nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResultsPublished", table: "Event", type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl", table: "Participant", type: "nvarchar(1000)", maxLength: 1000, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryType", table: "Category", type: "nvarchar(20)", maxLength: 20, nullable: false,
                defaultValue: "Distance");

            migrationBuilder.AddColumn<int>(name: "MinimumAge", table: "Category", type: "int", nullable: true);

            migrationBuilder.AddColumn<int>(name: "MaximumAge", table: "Category", type: "int", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BannerImageUrl", table: "Event");
            migrationBuilder.DropColumn(name: "ResultsPublished", table: "Event");
            migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "Participant");
            migrationBuilder.DropColumn(name: "CategoryType", table: "Category");
            migrationBuilder.DropColumn(name: "MinimumAge", table: "Category");
            migrationBuilder.DropColumn(name: "MaximumAge", table: "Category");
        }
    }
}
