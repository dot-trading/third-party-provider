using AutoMapper;
using TradingProject.ThirdParty.Domain.Models.Market;
using TradingProject.ThirdParty.Domain.Models.Trading;
using TradingProject.ThirdParty.Domain.Models.Account;

namespace TradingProject.ThirdParty.Application.Mappings;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        // Add mappings here if needed. 
        // Currently, we use records which map 1:1, but AutoMapper can be used for future DTOs.
    }
}
