namespace Defra.Database;

public abstract class DbContextBase<T>(DbContextOptions<T> options) : DbContext(options)
    where T : DbContext
{
    
}