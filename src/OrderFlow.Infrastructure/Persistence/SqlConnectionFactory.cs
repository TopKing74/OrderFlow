using System.Data.Common;
using Microsoft.Data.SqlClient;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class SqlConnectionFactory(string connectionString)
    : ISqlConnectionFactory
{
    public DbConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(connectionString);

        connection.Open();

        return connection;
    }
}
