

using System.ComponentModel;
using Shared.Models;

namespace Application.Models.User;

public class GetUserRequest : Pagination, IOrdered
{
    public List<Guid>? Ids { get; set; }
    public string? Search { get; set; }
    public string? Order { get; set; }
}
