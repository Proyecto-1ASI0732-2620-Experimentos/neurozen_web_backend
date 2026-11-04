using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Domain.Repositories;

public interface IVerificationAuditRepository : IBaseRepository<VerificationAuditEntry>
{
}
