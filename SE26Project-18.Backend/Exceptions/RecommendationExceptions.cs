namespace SE26Project_18.Backend.Exceptions;

internal sealed class ServiceUnavailableException(string message) : Exception(message);

internal sealed class NotFoundException(string message) : Exception(message);
