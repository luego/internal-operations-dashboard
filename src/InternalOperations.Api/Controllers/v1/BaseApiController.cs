using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

[ApiController]
// Define la ruta global compartida: api/v1/tickets, api/v1/users, etc.
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    // Aquí puedes inyectar servicios compartidos en el futuro (ILogger, IMapper, etc.) si lo deseas.
}
