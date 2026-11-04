using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.Professionals.Infrastructure.Repositories;

public class VerificationAuditRepository(AppDbContext context)
    : BaseRepository<VerificationAuditEntry>(context), IVerificationAuditRepository
{
}
