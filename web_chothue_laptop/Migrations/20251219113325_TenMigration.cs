using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_chothue_laptop.Migrations
{
    /// <inheritdoc />
    public partial class TenMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BRAND",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BRAND_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BRAND__3214EC27C3AE4F7C", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ROLE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROLE_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ROLE__3214EC27367E6C9F", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "STATUS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STATUS_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__STATUS__3214EC279CA1D331", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "USER",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROLE_ID = table.Column<long>(type: "bigint", nullable: true),
                    STATUS_ID = table.Column<long>(type: "bigint", nullable: true),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PASSWORD_HASH = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    OTP_CODE = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    OTP_EXPIRY = table.Column<DateTime>(type: "datetime", nullable: true),
                    AVATAR_URL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__USER__3214EC2709715138", x => x.ID);
                    table.ForeignKey(
                        name: "FK_USER_ROLE",
                        column: x => x.ROLE_ID,
                        principalTable: "ROLE",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_USER_STATUS",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CUSTOMER",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CUSTOMER_ID = table.Column<long>(type: "bigint", nullable: true),
                    FIRST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LAST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PHONE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    BLACK_LIST = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CUSTOMER__3214EC27DA0E6190", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CUSTOMER_USER",
                        column: x => x.CUSTOMER_ID,
                        principalTable: "USER",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MANAGER",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MANAGER_ID = table.Column<long>(type: "bigint", nullable: true),
                    FIRST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LAST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PHONE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MANAGER__3214EC27B2D74430", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MANAGER_USER",
                        column: x => x.MANAGER_ID,
                        principalTable: "USER",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "STAFF",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STAFF_ID = table.Column<long>(type: "bigint", nullable: true),
                    FIRST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LAST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PHONE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__STAFF__3214EC274C563AA1", x => x.ID);
                    table.ForeignKey(
                        name: "FK_STAFF_USER",
                        column: x => x.STAFF_ID,
                        principalTable: "USER",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "STUDENT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STUDENT_ID = table.Column<long>(type: "bigint", nullable: true),
                    FIRST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LAST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PHONE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__STUDENT__3214EC27658F3E18", x => x.ID);
                    table.ForeignKey(
                        name: "FK_STUDENT_USER",
                        column: x => x.STUDENT_ID,
                        principalTable: "USER",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TECHNICAL",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TECHNICAL_ID = table.Column<long>(type: "bigint", nullable: true),
                    FIRST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LAST_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PHONE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    IS_WORKING = table.Column<long>(type: "bigint", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TECHNICA__3214EC27F9334865", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TECHINICAL_USER",
                        column: x => x.TECHNICAL_ID,
                        principalTable: "USER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TECHNICAL_STATUS",
                        column: x => x.IS_WORKING,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "LAPTOP",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BRAND_ID = table.Column<long>(type: "bigint", nullable: true),
                    PRICE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    STATUS_ID = table.Column<long>(type: "bigint", nullable: true),
                    MANAGER_ID = table.Column<long>(type: "bigint", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UPDATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    STUDENT_ID = table.Column<long>(type: "bigint", nullable: true),
                    IMAGE_URL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    END_TIME = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LAPTOP__3214EC27AA362BA4", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LAPTOP_BRAND",
                        column: x => x.BRAND_ID,
                        principalTable: "BRAND",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_LAPTOP_MANAGER",
                        column: x => x.MANAGER_ID,
                        principalTable: "MANAGER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_LAPTOP_STATUS",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_LAPTOP_STUDENT",
                        column: x => x.STUDENT_ID,
                        principalTable: "STUDENT",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "BOOKING",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CUSTOMER_ID = table.Column<long>(type: "bigint", nullable: false),
                    LAPTOP_ID = table.Column<long>(type: "bigint", nullable: false),
                    STAFF_ID = table.Column<long>(type: "bigint", nullable: true),
                    START_TIME = table.Column<DateTime>(type: "datetime", nullable: false),
                    END_TIME = table.Column<DateTime>(type: "datetime", nullable: false),
                    TOTAL_PRICE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    STATUS_ID = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UPDATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    ID_NO_URL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    STUDENT_URL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BOOKING__3214EC275D19D412", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BOOKING_CUSTOMER",
                        column: x => x.CUSTOMER_ID,
                        principalTable: "CUSTOMER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_BOOKING_LAPTOP",
                        column: x => x.LAPTOP_ID,
                        principalTable: "LAPTOP",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_BOOKING_STAFF",
                        column: x => x.STAFF_ID,
                        principalTable: "STAFF",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_BOOKING_STATUS",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "LAPTOP_DETAIL",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LAPTOP_ID = table.Column<long>(type: "bigint", nullable: false),
                    GPU = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RAM_SIZE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RAM_TYPE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    STORAGE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SCREEN_SIZE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CPU = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LAPTOP_D__3214EC27977D8B22", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LAPTOP_DETAIL_LAPTOP",
                        column: x => x.LAPTOP_ID,
                        principalTable: "LAPTOP",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BOOKING_RECEIPT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BOOKING_ID = table.Column<long>(type: "bigint", nullable: false),
                    CUSTOMER_ID = table.Column<long>(type: "bigint", nullable: false),
                    STAFF_ID = table.Column<long>(type: "bigint", nullable: false),
                    START_TIME = table.Column<DateTime>(type: "datetime", nullable: false),
                    END_TIME = table.Column<DateTime>(type: "datetime", nullable: false),
                    LATE_MINUTES = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    TOTAL_PRICE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LATE_FEE = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BOOKING___3214EC2712A0AB23", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RECEIPT_BOOKING",
                        column: x => x.BOOKING_ID,
                        principalTable: "BOOKING",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_RECEIPT_CUSTOMER",
                        column: x => x.CUSTOMER_ID,
                        principalTable: "CUSTOMER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_RECEIPT_STAFF",
                        column: x => x.STAFF_ID,
                        principalTable: "STAFF",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "STUDENT_RENT_NOTIFICATION",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STUDENT_ID = table.Column<long>(type: "bigint", nullable: false),
                    MANAGER_ID = table.Column<long>(type: "bigint", nullable: false),
                    BOOKING_ID = table.Column<long>(type: "bigint", nullable: false),
                    TITLE = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MESSAGE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__STUDENT___3214EC27AA9F8216", x => x.ID);
                    table.ForeignKey(
                        name: "FK_STUDENT_NOTIFICATION_BOOKING",
                        column: x => x.BOOKING_ID,
                        principalTable: "BOOKING",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_STUDENT_NOTIFICATION_MANAGER",
                        column: x => x.MANAGER_ID,
                        principalTable: "MANAGER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_STUDENT_NOTIFICATION_STUDENT",
                        column: x => x.STUDENT_ID,
                        principalTable: "STUDENT",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TECHNICAL_TICKET",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LAPTOP_ID = table.Column<long>(type: "bigint", nullable: false),
                    BOOKING_ID = table.Column<long>(type: "bigint", nullable: true),
                    STAFF_ID = table.Column<long>(type: "bigint", nullable: false),
                    TECHNICAL_ID = table.Column<long>(type: "bigint", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TECHNICAL_RESPONSE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    STATUS_ID = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UPDATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TECHNICA__3214EC27887CB471", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TT_BOOKING",
                        column: x => x.BOOKING_ID,
                        principalTable: "BOOKING",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TT_LAPTOP",
                        column: x => x.LAPTOP_ID,
                        principalTable: "LAPTOP",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TT_STAFF",
                        column: x => x.STAFF_ID,
                        principalTable: "STAFF",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TT_STATUS",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TT_TECH",
                        column: x => x.TECHNICAL_ID,
                        principalTable: "TECHNICAL",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TICKET_LIST",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CUSTOMER_ID = table.Column<long>(type: "bigint", nullable: false),
                    STAFF_ID = table.Column<long>(type: "bigint", nullable: false),
                    LAPTOP_ID = table.Column<long>(type: "bigint", nullable: false),
                    TECHNICAL_TICKET_ID = table.Column<long>(type: "bigint", nullable: false),
                    FIXED_COST = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUSTOMER_RESPONSE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    STATUS_ID = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UPDATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    ERROR_IMAGE_URL = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TICKET_L__3214EC275F5E9A5A", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TL_CUSTOMER",
                        column: x => x.CUSTOMER_ID,
                        principalTable: "CUSTOMER",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TL_LAPTOP",
                        column: x => x.LAPTOP_ID,
                        principalTable: "LAPTOP",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TL_STAFF",
                        column: x => x.STAFF_ID,
                        principalTable: "STAFF",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TL_STATUS",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TL_TECH_TKT",
                        column: x => x.TECHNICAL_TICKET_ID,
                        principalTable: "TECHNICAL_TICKET",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_CUSTOMER_ID",
                table: "BOOKING",
                column: "CUSTOMER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_LAPTOP_ID",
                table: "BOOKING",
                column: "LAPTOP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_STAFF_ID",
                table: "BOOKING",
                column: "STAFF_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_STATUS_ID",
                table: "BOOKING",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_RECEIPT_BOOKING_ID",
                table: "BOOKING_RECEIPT",
                column: "BOOKING_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_RECEIPT_CUSTOMER_ID",
                table: "BOOKING_RECEIPT",
                column: "CUSTOMER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BOOKING_RECEIPT_STAFF_ID",
                table: "BOOKING_RECEIPT",
                column: "STAFF_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__BRAND__FBF4813685EAEB89",
                table: "BRAND",
                column: "BRAND_NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CUSTOMER_CUSTOMER_ID",
                table: "CUSTOMER",
                column: "CUSTOMER_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__CUSTOMER__161CF72465772F8F",
                table: "CUSTOMER",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LAPTOP_BRAND_ID",
                table: "LAPTOP",
                column: "BRAND_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LAPTOP_MANAGER_ID",
                table: "LAPTOP",
                column: "MANAGER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LAPTOP_STATUS_ID",
                table: "LAPTOP",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LAPTOP_STUDENT_ID",
                table: "LAPTOP",
                column: "STUDENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LAPTOP_DETAIL_LAPTOP_ID",
                table: "LAPTOP_DETAIL",
                column: "LAPTOP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MANAGER_MANAGER_ID",
                table: "MANAGER",
                column: "MANAGER_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__MANAGER__161CF724952E4047",
                table: "MANAGER",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ROLE__2B9B877EDFE61C0F",
                table: "ROLE",
                column: "ROLE_NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_STAFF_ID",
                table: "STAFF",
                column: "STAFF_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__STAFF__161CF7246254ACB9",
                table: "STAFF",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__STATUS__064B2D2D22983721",
                table: "STATUS",
                column: "STATUS_NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_STUDENT_ID",
                table: "STUDENT",
                column: "STUDENT_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__STUDENT__161CF724B0DFA8D3",
                table: "STUDENT",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_RENT_NOTIFICATION_BOOKING_ID",
                table: "STUDENT_RENT_NOTIFICATION",
                column: "BOOKING_ID");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_RENT_NOTIFICATION_MANAGER_ID",
                table: "STUDENT_RENT_NOTIFICATION",
                column: "MANAGER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_RENT_NOTIFICATION_STUDENT_ID",
                table: "STUDENT_RENT_NOTIFICATION",
                column: "STUDENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_IS_WORKING",
                table: "TECHNICAL",
                column: "IS_WORKING");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TECHNICAL_ID",
                table: "TECHNICAL",
                column: "TECHNICAL_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__TECHNICA__161CF724B6923E1A",
                table: "TECHNICAL",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TICKET_BOOKING_ID",
                table: "TECHNICAL_TICKET",
                column: "BOOKING_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TICKET_LAPTOP_ID",
                table: "TECHNICAL_TICKET",
                column: "LAPTOP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TICKET_STAFF_ID",
                table: "TECHNICAL_TICKET",
                column: "STAFF_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TICKET_STATUS_ID",
                table: "TECHNICAL_TICKET",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TECHNICAL_TICKET_TECHNICAL_ID",
                table: "TECHNICAL_TICKET",
                column: "TECHNICAL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TICKET_LIST_CUSTOMER_ID",
                table: "TICKET_LIST",
                column: "CUSTOMER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TICKET_LIST_LAPTOP_ID",
                table: "TICKET_LIST",
                column: "LAPTOP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TICKET_LIST_STAFF_ID",
                table: "TICKET_LIST",
                column: "STAFF_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TICKET_LIST_STATUS_ID",
                table: "TICKET_LIST",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TICKET_LIST_TECHNICAL_TICKET_ID",
                table: "TICKET_LIST",
                column: "TECHNICAL_TICKET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ROLE_ID",
                table: "USER",
                column: "ROLE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_STATUS_ID",
                table: "USER",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__USER__161CF724C30A992C",
                table: "USER",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BOOKING_RECEIPT");

            migrationBuilder.DropTable(
                name: "LAPTOP_DETAIL");

            migrationBuilder.DropTable(
                name: "STUDENT_RENT_NOTIFICATION");

            migrationBuilder.DropTable(
                name: "TICKET_LIST");

            migrationBuilder.DropTable(
                name: "TECHNICAL_TICKET");

            migrationBuilder.DropTable(
                name: "BOOKING");

            migrationBuilder.DropTable(
                name: "TECHNICAL");

            migrationBuilder.DropTable(
                name: "CUSTOMER");

            migrationBuilder.DropTable(
                name: "LAPTOP");

            migrationBuilder.DropTable(
                name: "STAFF");

            migrationBuilder.DropTable(
                name: "BRAND");

            migrationBuilder.DropTable(
                name: "MANAGER");

            migrationBuilder.DropTable(
                name: "STUDENT");

            migrationBuilder.DropTable(
                name: "USER");

            migrationBuilder.DropTable(
                name: "ROLE");

            migrationBuilder.DropTable(
                name: "STATUS");
        }
    }
}
