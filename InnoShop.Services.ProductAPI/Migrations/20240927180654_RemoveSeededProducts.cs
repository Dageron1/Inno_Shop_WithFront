using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnoShop.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4);

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Products",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "CategoryName", "CreatedByUserId", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Phone", "", "Ultra modern new phone with A14383 processor, and 25w charger, not included :( !!!", "https://placeholder.co/600x400", "IPhone 37", 130.0 },
                    { 2, "Phone", "", "Brand new flagship, with touch screen, 1500 watt charging, case, iPhone charger, car and apartment included.", "https://placeholder.co/600x400", "Samsung Galaxy s47+ UltraXXL SuperMega AmoledXL", 110.0 },
                    { 3, "Phone", "", "New pricing principle. $10 phone, but a little more advertising. 780 megapixel camera, iPhone charger included.", "https://placeholder.co/600x400", "Xiaomi Redmi Note 154X Ultra Plus 9G+ ultra", 10.0 },
                    { 4, "Notebook", "", "NVIDIA 15990 128gb Cyberpunck 30fps in 2k guaranteed!!!, Intel Core 23 9999K XL, 100Tb M2, daydreaming. Default notebook.", "https://placeholder.co/600x400", "Lenovo B-best 431", 50.0 }
                });
        }
    }
}
