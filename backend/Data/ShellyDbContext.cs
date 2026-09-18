using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shelly.Backend.Entities;

namespace Shelly.Backend.Data;

public class ShellyDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ShellyDbContext(DbContextOptions<ShellyDbContext> options) : base(options) { }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Identity tables renamed to snake_case to match spec
        b.Entity<User>().ToTable("users");
        b.Entity<IdentityRole<Guid>>().ToTable("roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("user_roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("user_claims");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("user_logins");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("user_tokens");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("role_claims");

        b.Entity<User>(e =>
        {
            e.Property(x => x.DisplayName).HasColumnName("display_name");
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.UserName).HasColumnName("username");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
        });

        b.Entity<Workspace>(e =>
        {
            e.ToTable("workspaces");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Slug).HasColumnName("slug").IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        b.Entity<WorkspaceMember>(e =>
        {
            e.ToTable("workspace_members");
            e.HasKey(x => new { x.WorkspaceId, x.UserId });
            e.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.JoinedAt).HasColumnName("joined_at");
            e.HasOne(x => x.Workspace).WithMany(w => w.Members).HasForeignKey(x => x.WorkspaceId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasCheckConstraint("ck_workspace_role",
                "role IN ('owner','admin','member')");
        });

        b.Entity<Room>(e =>
        {
            e.ToTable("rooms");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ArchivedAt).HasColumnName("archived_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => new { x.WorkspaceId, x.Name }).IsUnique();
            e.HasOne(x => x.Workspace).WithMany(w => w.Rooms).HasForeignKey(x => x.WorkspaceId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        b.Entity<RoomMember>(e =>
        {
            e.ToTable("room_members");
            e.HasKey(x => new { x.RoomId, x.UserId });
            e.Property(x => x.RoomId).HasColumnName("room_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.JoinedAt).HasColumnName("joined_at");
            e.HasOne(x => x.Room).WithMany(r => r.Members).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasCheckConstraint("ck_room_role",
                "role IN ('owner','operator','viewer')");
        });
    }
}