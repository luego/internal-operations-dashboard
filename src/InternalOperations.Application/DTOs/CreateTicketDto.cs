namespace InternalOperations.Application.DTOs;

public record CreateTicketDto(
    string Title,
    string Description,
    Guid UserId,
    Guid DepartmentId);