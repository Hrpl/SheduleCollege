using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text;
using Dapper;
using UserService.Infrastructure.Repositories.Interfaces;
using UserService.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Execution;

namespace UserService.Infrastructure.Repositories.Implementations;

public class AsyncRepository : IAsyncRepository
{
    private readonly ILogger<AsyncRepository> _logger;

    private QueryFactory _queryFactory;
    private readonly ConcurrentDictionary<int, IDbTransaction> _transactionPool = new();

    public AsyncRepository(IDbConnectionManager connectionManager, ILogger<AsyncRepository> logger)
    {
        _queryFactory = connectionManager.PostgresQueryFactory;
        _logger = logger;
    }

    public void SetConnection(IDbConnectionManager connectionManager)
    {
        _queryFactory = connectionManager.PostgresQueryFactory;
    }

    public Query GetQueryBuilder()
    {
        return new Query();
    }

    public QueryFactory GetQueryFactory() => _queryFactory;

    public Query GetQueryBuilder(string tableName)
    {
        return _queryFactory.Query(tableName);
    }

    public Query GetQueryBuilderWithConnection()
    {
        return _queryFactory.Query();
    }

    public Query GetIncludeBuilder(string tableName)
    {
        return _queryFactory.Query(tableName);
    }

    public Query GetIncludeBuilder(Query query)
    {
        return _queryFactory.Query().From(query);
    }

    public async Task<int> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        return await _queryFactory.ExecuteAsync(query, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(Query query, CancellationToken cancellationToken = default)
        => await ExistsAsync(query, null, cancellationToken);

    public async Task<bool> ExistsAsync(Query query, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var finalQuery = CloneToQueryFactory(query);
        return await finalQuery.ExistsAsync(transaction, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public IDbTransaction CreateTransaction(IsolationLevel? isolationLevel = null)
    {
        _queryFactory.Connection.Open();

        IDbTransaction transaction = isolationLevel is null
            ? _queryFactory.Connection.BeginTransaction()
            : _queryFactory.Connection.BeginTransaction(isolationLevel.Value);

        _transactionPool.AddOrUpdate(
            transaction.GetHashCode(),
            _ => transaction,
            (_, _) => transaction);

        return transaction;
    }

    /// <inheritdoc />
    public void CommitTransaction(IDbTransaction transaction)
    {
        try
        {
            transaction.Commit();
            _queryFactory.Connection.Close();
            _transactionPool.Remove(transaction.GetHashCode(), out _);
        }
        catch
        {
            RollbackTransaction(transaction);
            throw;
        }
    }

    /// <inheritdoc />
    public void RollbackTransaction(IDbTransaction transaction)
    {
        transaction.Rollback();
        _queryFactory.Connection.Close();
        _transactionPool.Remove(transaction.GetHashCode(), out _);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(Query query, IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await _queryFactory.ExecuteAsync(query, transaction, cancellationToken: ct);
    }

    public async Task<int> ExecuteAsync(string query, object? value = null, IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await _queryFactory.Connection.ExecuteAsync(query, value, transaction);
    }

    /// <inheritdoc />
    public async Task<TId> InsertAsync<TId>(
        Query query,
        string idColumnName = "ID",
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where TId : struct
    {
        var compiledQuery = _queryFactory.Compiler.Compile(query);

        TId id = default;

        var dbType = id switch
        {
            int => DbType.Int32,
            uint => DbType.UInt32,
            long => DbType.Int64,
            ulong => DbType.UInt64,
            _ => DbType.Object
        };

        var param = new DynamicParameters();
        param.AddDynamicParams(compiledQuery.NamedBindings);
        param.Add("returnId", dbType: dbType, direction: ParameterDirection.Output);

        await _queryFactory.Connection.ExecuteAsync(
            $@"{compiledQuery.Sql} RETURNING {idColumnName}", param, transaction);

        id = param.Get<TId>("returnId");

        return id;
    }

    public async Task<int> InsertManyAsync<T>(
        IEnumerable<T> rowsValue,
        string tableName,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        //http://ahmadsbhutta.blogspot.com/2019/07/how-to-fix-ora-01745-invalid-hostbind.html
        //As per note "ORA-600[qcscbndv1], [65535, ORA-600[Kghssgfr2], ORA-600[17112] Instance Failure ( Doc ID 1311230.1 ) "
        const int totalBindings = 65000;

        var rows = rowsValue.ToList();
        if (!rows.Any())
        {
            return 0;
        }

        var firstRow = rows.First();
        var rowType = firstRow.GetType();
        var props = rowType
            .GetProperties()
            .Where(value =>
                value.GetValue(firstRow) is not null)
            .ToArray();
        var columns = "";

        var propsToInsert = new List<PropertyInfo>();

        foreach (var prop in props)
        {
            var attrs = prop.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                if (attr is ColumnAttribute or SqlKata.ColumnAttribute)
                {
                    columns += attr.GetType().GetProperty("Name")?.GetValue(attr) + ", ";
                    propsToInsert.Add(prop);
                    break;
                }

                if (attr is IgnoreAttribute)
                {
                    break;
                }
            }
        }

        columns = columns.TrimEnd(',', ' ');

        var countOfInsert = 1;
        var totalCountInsert = 0;
        var bindingsCount = rows.Count * propsToInsert.Count;
        if (bindingsCount > totalBindings)
        {
            countOfInsert =
                Convert.ToInt32(Math.Ceiling((double)bindingsCount / totalBindings));
        }

        var rowsSize = Convert.ToInt32(Math.Ceiling((double)rows.Count / countOfInsert));
        var startQuery = new StringBuilder();
        startQuery.Append("INTO ");
        startQuery.Append(tableName);
        startQuery.Append(" (");
        startQuery.Append(columns);
        startQuery.Append(") VALUES (");
        List<Task> insertTasks = new(countOfInsert);
        var comaIndex = propsToInsert.Count - 1;
        for (var i = 0; i < countOfInsert; i++)
        {
            var iteration = i;
            insertTasks.Add(Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var paramsIndex = 0;
                StringBuilder queryBuilder = new();
                queryBuilder.Append("INSERT ALL ");
                var insertRows = rows.Skip(rowsSize * iteration)
                    .Take(rowsSize).ToList();
                var dictionaryParameters = new Dictionary<string, object?>();
                foreach (object item in insertRows)
                {
                    queryBuilder.Append(startQuery);
                    for (var index = 0; index < propsToInsert.Count; index++)
                    {
                        var prop = propsToInsert[index];
                        var indexParameters = new StringBuilder();
                        indexParameters.Append('p');
                        indexParameters.Append(paramsIndex);
                        dictionaryParameters.Add(indexParameters.ToString(),
                            rowType.GetProperty(prop.Name)?.GetValue(item));
                        queryBuilder.Append(':');
                        queryBuilder.Append(indexParameters);
                        if (index < comaIndex)
                        {
                            queryBuilder.Append(',');
                        }

                        paramsIndex++;
                    }

                    queryBuilder.Append(") ");
                }

                var dynamicParameters = new DynamicParameters(dictionaryParameters);
                queryBuilder.Append("SELECT * FROM dual");
                var queryString = queryBuilder.ToString();
                var insertedRows =
                    await _queryFactory.Connection.ExecuteAsync(queryString, dynamicParameters, transaction);
                Interlocked.Add(ref totalCountInsert, insertedRows);
            }, cancellationToken));
        }

        await Task.WhenAll(insertTasks.ToArray());
        return totalCountInsert;
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Query query,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return await _queryFactory.CountAsync<int>(query, null, transaction, null, cancellationToken);
    }

    public async Task<T?> GetAsync<T>(Query query, IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        var finalQuery = CloneToQueryFactory(query);
        return await finalQuery.FirstOrDefaultAsync<T>(transaction, cancellationToken: ct);
    }

    public async Task<IEnumerable<T>> GetListAsync<T>(Query query, CancellationToken cancellationToken = default)
    {
        var finalQuery = CloneToQueryFactory(query);
        return await finalQuery.GetAsync<T>(cancellationToken: cancellationToken);
    }

    public string Compile(Query query) =>
        _queryFactory.Compiler.Compile(query).ToString();

    private Query CloneToQueryFactory(Query sourceQuery)
    {
        if (sourceQuery is XQuery)
        {
            return sourceQuery;
        }

        var resultQuery = _queryFactory.Query()
            .From(sourceQuery);

        resultQuery.Includes = sourceQuery.Includes;
        resultQuery.Method = sourceQuery.Method;

        return resultQuery;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logger.LogDebug("Disposing async repository...");

        foreach (KeyValuePair<int, IDbTransaction> item in _transactionPool)
        {
            item.Value.Rollback();
        }

        if (!_transactionPool.IsEmpty)
        {
            _logger.LogDebug("Rollbacked uncontrolled transactions: {Count}", _transactionPool.Count);
        }

        _transactionPool.Clear();
        _queryFactory.Dispose();
    }

    public Task<T?> GetForIncludeAsync<T>(Query query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<T>> GetListForIncludeAsync<T>(Query query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}