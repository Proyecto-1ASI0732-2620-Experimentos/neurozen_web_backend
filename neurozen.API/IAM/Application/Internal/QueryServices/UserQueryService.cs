using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Queries;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;

namespace neurozen.API.IAM.Application.Internal.QueryServices;

public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    public Task<User?> Handle(GetUserByIdQuery query) => userRepository.FindByIdAsync(query.Id);
}
