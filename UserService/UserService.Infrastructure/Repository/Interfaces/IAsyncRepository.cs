using System.Data;
using SqlKata;
using SqlKata.Execution;

namespace UserService.Infrastructure.Repositories.Interfaces;

public interface IAsyncRepository
{
    QueryFactory GetQueryFactory();
    Query GetQueryBuilder();
    Query GetQueryBuilder(string tableName);
    Query GetIncludeBuilder(string tableName);
    Query GetQueryBuilderWithConnection();
    Query GetIncludeBuilder(Query query);

    IDbTransaction CreateTransaction(IsolationLevel? isolationLevel = null);
    void CommitTransaction(IDbTransaction transaction);
    void RollbackTransaction(IDbTransaction transaction);
    Task<int> ExecuteAsync(Query query, CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(Query query, IDbTransaction? transaction = null, CancellationToken ct = default);

    Task<int> ExecuteAsync(string query, object? value = null, IDbTransaction? transaction = null,
        CancellationToken ct = default);

    Task<TId> InsertAsync<TId>(
        Query query,
        string idColumnName = "ID",
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where TId : struct;

    Task<int> InsertManyAsync<T>(
        IEnumerable<T> rowsValue,
        string tableName,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class;

    Task<int> CountAsync(Query query, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Query query, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Query query, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(Query query, IDbTransaction? transaction = null, CancellationToken ct = default);

    Task<T?> GetForIncludeAsync<T>(Query query, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetListAsync<T>(Query query, CancellationToken ct = default);
    Task<IEnumerable<T>> GetListForIncludeAsync<T>(Query query, CancellationToken ct = default);

    string Compile(Query query);
}