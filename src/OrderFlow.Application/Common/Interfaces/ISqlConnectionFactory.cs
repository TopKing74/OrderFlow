using System.Data.Common;

namespace OrderFlow.Application.Common.Interfaces;

public interface ISqlConnectionFactory
{
    DbConnection CreateOpenConnection();
}
