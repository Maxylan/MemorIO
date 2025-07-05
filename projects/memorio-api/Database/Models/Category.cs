using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemorIO.Database.Models;

/// <summary>
/// The <see cref="Category"/> db-entity.
/// </summary>
[Table("categories", Schema = "magedb")]
[Index("Title", Name = "categories_title_key", IsUnique = true)]
[Index("Title", Name = "idx_categories_title")]
[Index("UpdatedAt", Name = "idx_categories_updated_at")]
public partial class Category : IDatabaseEntity<Category>
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("summary")]
    [StringLength(255)]
    public string? Summary { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("required_privilege")]
    public byte RequiredPrivilege { get; set; }
}

/// <summary>
/// Inverse properties & static methods of the <see cref="Category"/> db-entity.
/// </summary>
public partial class Category
{
    [InverseProperty("Category")]
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("CreatedCategories")]
    public virtual Account? CreatedByNavigation { get; set; }

    [ForeignKey("UpdatedBy")]
    [InverseProperty("UpdatedCategories")]
    public virtual Account? UpdatedByNavigation { get; set; }

    /// <summary>
    /// Convert a <see cref="Category"/> instance to its <see cref="CategoryDTO"/> equivalent.
    /// (Data Transfer Object)
    /// </summary>
    public CategoryDTO DTO() => new CategoryDTO() {
        Id = this.Id,
        Title = this.Title,
        Summary = this.Summary,
        Description = this.Description,
        CreatedBy = this.CreatedBy,
        CreatedAt = this.CreatedAt,
        UpdatedAt = this.UpdatedAt,
        UpdatedBy = this.UpdatedBy,
        RequiredPrivilege = this.RequiredPrivilege,
        // Navigations
        Albums = this.Albums,
        CreatedByNavigation = this.CreatedByNavigation,
        UpdatedByNavigation = this.UpdatedByNavigation
    };

    /// <summary>
    /// Construct / Initialize an <see cref="EntityTypeBuilder{TEntity}"/> of type <see cref="Category"/>
    /// </summary>
    public static Action<EntityTypeBuilder<Category>> Build => (
        entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.RequiredPrivilege).HasDefaultValue((short)0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CreatedCategories)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UpdatedCategories)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_updated_by_user");
        }
    );
}
