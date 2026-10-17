namespace SV2380601458.EntityFrameworkCore.Seed.Host;

public class InitialHostDbBuilder
{
    private readonly SV2380601458DbContext _context;

    public InitialHostDbBuilder(SV2380601458DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        new DefaultEditionCreator(_context).Create();
        new DefaultLanguagesCreator(_context).Create();
        new HostRoleAndUserCreator(_context).Create();
        new DefaultSettingsCreator(_context).Create();

        _context.SaveChanges();
    }
}
