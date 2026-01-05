using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSprint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityTypeAndHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentActivityId",
                table: "ActivityTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ActivityTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_ParentActivityId",
                table: "ActivityTemplates",
                column: "ParentActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_UserId_Type",
                table: "ActivityTemplates",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityTemplates_ActivityTemplates_ParentActivityId",
                table: "ActivityTemplates",
                column: "ParentActivityId",
                principalTable: "ActivityTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityTemplates_ActivityTemplates_ParentActivityId",
                table: "ActivityTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTemplates_ParentActivityId",
                table: "ActivityTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTemplates_UserId_Type",
                table: "ActivityTemplates");

            migrationBuilder.DropColumn(
                name: "ParentActivityId",
                table: "ActivityTemplates");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ActivityTemplates");
        }
    }
}
