namespace InternalOperations.Application.DTOs;

public record TicketDto(
    Guid Id,
    string Title,
    string Description,
    int Number,
    Guid UserId,
    Guid DepartmentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
