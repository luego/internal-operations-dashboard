namespace InternalOperations.Application.DTOs;

public record CreateTicketDto(
    string Title,
    string Description,
    int Number,
    Guid UserId,
    Guid DepartmentId);
