using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnoShop.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class createProductTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "CategoryName", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Phone", "Ultra modern new phone with A14383 processor, and 25w charger, not included :( !!!", "https://placeholder.co/600x400", "IPhone 37", 130.0 },
                    { 2, "Phone", "Brand new flagship, with touch screen, 1500 watt charging, case, iPhone charger, car and apartment included.", "https://placeholder.co/600x400", "Samsung Galaxy s47+ UltraXXL SuperMega AmoledXL", 110.0 },
                    { 3, "Phone", "New pricing principle. $10 phone, but a little more advertising. 780 megapixel camera, iPhone charger included.", "https://placeholder.co/600x400", "Xiaomi Redmi Note 154X Ultra Plus 9G+ ultra", 10.0 },
                    { 4, "Notebook", "NVIDIA 15990 128gb Cyberpunck 30fps in 2k guaranteed!!!, Intel Core 23 9999K XL, 100Tb M2, daydreaming. Default notebook.", "https://placeholder.co/600x400", "Lenovo B-best 431", 50.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
