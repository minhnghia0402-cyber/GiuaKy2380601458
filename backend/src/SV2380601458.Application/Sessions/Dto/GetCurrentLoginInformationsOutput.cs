namespace SV2380601458.Sessions.Dto;

public class GetCurrentLoginInformationsOutput
{
    public ApplicationInfoDto Application { get; set; }

    public UserLoginInfoDto User { get; set; }

    public TenantLoginInfoDto Tenant { get; set; }
}
