using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Exceptions;

public sealed class ResponseAlreadyProcessedException : Exception
{
    public ResponseAlreadyProcessedException(ResponseType currentType)
        : base($"Response has already been processed as {currentType}.") { }
}
