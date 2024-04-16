using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Infrastructure.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("1_Initial")]
public sealed class DatabaseMigrations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "AccountRegistrations",
            table => new
            {
                Id = table.Column<string>("varchar(33)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                TenantId = table.Column<string>("varchar(30)", nullable: false),
                Email = table.Column<string>("varchar(100)", nullable: false),
                RetryCount = table.Column<int>("int", nullable: false),
                OneTimePasswordHash = table.Column<string>("varchar(84)", nullable: false),
                ValidUntil = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                Completed = table.Column<bool>("bit", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_AccountRegistrations", x => x.Id); }
        );
        
        migrationBuilder.CreateTable(
            "Tenants",
            table => new
            {
                Id = table.Column<string>("varchar(30)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Name = table.Column<string>("nvarchar(30)", nullable: false),
                State = table.Column<string>("varchar(20)", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Tenants", x => x.Id); }
        );
        
        migrationBuilder.CreateTable(
            "Users",
            table => new
            {
                TenantId = table.Column<string>("varchar(30)", nullable: false),
                Id = table.Column<long>("char(30)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Email = table.Column<string>("nvarchar(100)", nullable: false),
                FirstName = table.Column<string>("nvarchar(30)", nullable: true),
                LastName = table.Column<string>("nvarchar(30)", nullable: true),
                UserRole = table.Column<string>("varchar(20)", nullable: false),
                EmailConfirmed = table.Column<bool>("bit", nullable: false),
                Avatar = table.Column<string>("varchar(200)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey("FK_Users_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
            }
        );
        
        migrationBuilder.CreateIndex("IX_Users_TenantId", "Users", "TenantId");
    }
    
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.UseIdentityColumns();
        
        modelBuilder.Entity("PlatformPlatform.AccountManagement.Domain.AccountRegistrations.AccountRegistration", b =>
            {
                b.Property<string>("Id")
                    .HasColumnType("varchar(33)");
                
                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("datetimeoffset");
                
                b.Property<DateTimeOffset?>("ModifiedAt")
                    .IsConcurrencyToken()
                    .HasColumnType("datetimeoffset");
                
                b.Property<string>("TenantId")
                    .IsRequired()
                    .HasColumnType("varchar(30)");
                
                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("varchar(100)");
                
                b.Property<int>("RetryCount")
                    .HasColumnType("int");
                
                b.Property<string>("OneTimePasswordHash")
                    .IsRequired()
                    .HasColumnType("varchar(84)");
                
                b.Property<DateTimeOffset>("ValidUntil")
                    .HasColumnType("datetimeoffset");
                
                b.Property<bool>("Completed")
                    .IsRequired()
                    .HasColumnType("bit");
                
                b.HasKey("Id");
                
                b.ToTable("AccountRegistrations");
            }
        );
        
        modelBuilder.Entity("PlatformPlatform.AccountManagement.Domain.Tenants.Tenant", b =>
            {
                b.Property<string>("Id")
                    .HasColumnType("varchar(30)");
                
                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("datetimeoffset");
                
                b.Property<DateTimeOffset?>("ModifiedAt")
                    .IsConcurrencyToken()
                    .HasColumnType("datetimeoffset");
                
                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("nvarchar(30)");
                
                b.Property<string>("State")
                    .IsRequired()
                    .HasColumnType("varchar(20)");
                
                b.HasKey("Id");
                
                b.ToTable("Tenants");
            }
        );
        
        modelBuilder.Entity("PlatformPlatform.AccountManagement.Domain.Users.User", b =>
            {
                b.Property<string>("TenantId")
                    .IsRequired()
                    .HasColumnType("varchar(30)");
                
                b.Property<long>("Id")
                    .HasColumnType("char(30)");
                
                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("datetimeoffset");
                
                b.Property<DateTimeOffset?>("ModifiedAt")
                    .IsConcurrencyToken()
                    .HasColumnType("datetimeoffset");
                
                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("nvarchar(100)");
                
                b.Property<string>("FirstName")
                    .HasColumnType("nvarchar(30)");
                
                b.Property<string>("LastName")
                    .HasColumnType("nvarchar(30)");
                
                b.Property<string>("UserRole")
                    .IsRequired()
                    .HasColumnType("varchar(20)");
                
                b.Property<bool>("EmailConfirmed")
                    .IsRequired()
                    .HasColumnType("bit");
                
                b.Property<string>("Avatar")
                    .IsRequired()
                    .HasColumnType("varchar(200)");
                
                b.HasKey("Id");
                
                b.HasIndex("TenantId");
                
                b.ToTable("Users");
            }
        );
    }
}
