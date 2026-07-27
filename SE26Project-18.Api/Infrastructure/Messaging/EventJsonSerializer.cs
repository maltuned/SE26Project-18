using System.Text.Json;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal static class EventJsonSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
