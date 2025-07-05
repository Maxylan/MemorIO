using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;
using MemorIO.Database.Models;

namespace MemorIO.Models;

public class MutateTag : TagDTO
{
    [JsonIgnore, SwaggerIgnore]
    public new int? Id { get; set; }

    /*
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    */
}
