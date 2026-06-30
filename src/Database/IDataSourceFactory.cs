namespace Defra.Database;

public interface IDataSourceFactory<out TSource>
{
    TSource CreateDataSource(string connectionIdentifier);
}