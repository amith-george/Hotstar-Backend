using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotstarApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWatchHistoryCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Contents_ContentId1",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Videos_VideoId",
                table: "WatchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Videos_VideoId1",
                table: "WatchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Watchlists_Contents_ContentId1",
                table: "Watchlists");

            migrationBuilder.DropIndex(
                name: "IX_Watchlists_ContentId1",
                table: "Watchlists");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_VideoId1",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ContentId1",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ContentId1",
                table: "Watchlists");

            migrationBuilder.DropColumn(
                name: "VideoId1",
                table: "WatchHistories");

            migrationBuilder.DropColumn(
                name: "ContentId1",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Videos_VideoId",
                table: "WatchHistories",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Videos_VideoId",
                table: "WatchHistories");

            migrationBuilder.AddColumn<int>(
                name: "ContentId1",
                table: "Watchlists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoId1",
                table: "WatchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentId1",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ContentId1",
                table: "Watchlists",
                column: "ContentId1");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_VideoId1",
                table: "WatchHistories",
                column: "VideoId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ContentId1",
                table: "Reviews",
                column: "ContentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Contents_ContentId1",
                table: "Reviews",
                column: "ContentId1",
                principalTable: "Contents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Videos_VideoId",
                table: "WatchHistories",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Videos_VideoId1",
                table: "WatchHistories",
                column: "VideoId1",
                principalTable: "Videos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Watchlists_Contents_ContentId1",
                table: "Watchlists",
                column: "ContentId1",
                principalTable: "Contents",
                principalColumn: "Id");
        }
    }
}
