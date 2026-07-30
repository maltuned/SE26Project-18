namespace SE26Project_18.Api.Models.Responses;

public sealed record WebSocketTicketResponse(string Ticket, DateTimeOffset ExpiresAt);
