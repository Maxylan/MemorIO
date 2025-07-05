using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;
using MemorIO.Database.Models;

namespace MemorIO.Models;

public class MutateAccount : AccountDTO
{
    [JsonIgnore, SwaggerIgnore]
    public new string? Password { get; }

    [JsonIgnore, SwaggerIgnore]
    public new DateTime? CreatedAt { get; set; }

    [JsonIgnore, SwaggerIgnore]
    public new DateTime? LastLogin { get; set; }
}
