using MemorIO.Database.Models;

namespace MemorIO.Models;

/// <summary>
/// Collection of all albums (<see cref="MemorIO.Models.DisplayAlbum"/>) inside the the given
/// <paramref name="category"/> (<see cref="MemorIO.Database.Models.AlbumDTO"/>).
/// </summary>
public record class DisplayCategory
{
    public DisplayCategory(CategoryDTO category)
    {
        ArgumentNullException.ThrowIfNull(category, nameof(category));
        ArgumentNullException.ThrowIfNull(category.Albums, nameof(category.Albums));

        this._albums = category.Albums
            .Select(a => new DisplayAlbum(a));
    }

    public DisplayCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category, nameof(category));
        ArgumentNullException.ThrowIfNull(category.Albums, nameof(category.Albums));

        this._albums = category.Albums
            .Select(a => new DisplayAlbum(a));
    }

    protected IEnumerable<DisplayAlbum> _albums;
    public IEnumerable<DisplayAlbum> Albums {
        get => _albums;
    }

    /// <summary>
    /// Returns the number of elements (albums) in this sequence.
    /// </summary>
    public int Count => this._albums.Count();
}
