using SAPToOdoo.Application.DTOs.SapExplorer;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SAPToOdoo.Infrastructure.Sap
{
    public sealed class SapTableExplorerService : ISapTableExplorerService
    {
        private readonly ISapConnectionService _connectionService;
        private readonly ILogger<SapTableExplorerService> _logger;

        public SapTableExplorerService(
            ISapConnectionService connectionService,
            ILogger<SapTableExplorerService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        public Task<SapTableExplorerResponse> GetTableAsync(
            string tableName,
            int limit)
        {
            return StaTaskRunner.RunAsync<SapTableExplorerResponse>(() =>
            {
                ValidateTableName(tableName);

                if (limit < 1)
                {
                    limit = 20;
                }

                if (limit > 100)
                {
                    limit = 100;
                }

                using var handle = _connectionService.Connect();

                dynamic? recordSet = null;

                try
                {
                    recordSet = handle.GetBusinessObjectFromCandidates(
                        SapObjectTypes.BoRecordsetCandidates,
                        $"explore SAP table {tableName}");

                    string safeTableName = tableName.Trim();

                    string query = $@"
                    SELECT TOP {limit} *
                    FROM [{safeTableName}]";

                    recordSet.DoQuery(query);

                    var rows = new List<Dictionary<string, object?>>();

                    int fieldCount = recordSet.Fields.Count;

                    if (!(bool)recordSet.EoF)
                    {
                        recordSet.MoveFirst();

                        while (!(bool)recordSet.EoF)
                        {
                            var row = new Dictionary<string, object?>(
                                StringComparer.OrdinalIgnoreCase);

                            for (int i = 0; i < fieldCount; i++)
                            {
                                dynamic? field = null;

                                try
                                {
                                    field = recordSet.Fields.Item(i);

                                    string fieldName =
                                        field.Name?.ToString() ?? $"Field{i}";

                                    object rawValue = field.Value;

                                    row[fieldName] = ConvertValue(rawValue);
                                }
                                finally
                                {
                                    if (field is not null &&
                                        Marshal.IsComObject(field))
                                    {
                                        Marshal.FinalReleaseComObject(field);
                                    }
                                }
                            }

                            rows.Add(row);

                            recordSet.MoveNext();
                        }
                    }

                    _logger.LogInformation(
                        "SAP table explorer retrieved {Count} rows from {TableName}",
                        rows.Count,
                        safeTableName);

                    return new SapTableExplorerResponse
                    {
                        TableName = safeTableName,
                        Count = rows.Count,
                        Rows = rows
                    };
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to explore SAP table '{tableName}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (recordSet is not null &&
                        Marshal.IsComObject(recordSet))
                    {
                        Marshal.FinalReleaseComObject(recordSet);
                    }
                }
            });
        }

        public Task<SapTableSchemaResponse> GetSchemaAsync(
            string tableName)
        {
            return StaTaskRunner.RunAsync<SapTableSchemaResponse>(() =>
            {
                ValidateTableName(tableName);

                using var handle = _connectionService.Connect();

                dynamic? recordSet = null;

                try
                {
                    recordSet = handle.GetBusinessObjectFromCandidates(
                        SapObjectTypes.BoRecordsetCandidates,
                        $"get SAP table schema {tableName}");

                    string safeTableName = tableName.Trim();

                    string query = $@"
                    SELECT TOP 1 *
                    FROM [{safeTableName}]";

                    recordSet.DoQuery(query);

                    var fields = new List<SapTableFieldResponse>();

                    int fieldCount = recordSet.Fields.Count;

                    for (int i = 0; i < fieldCount; i++)
                    {
                        dynamic? field = null;

                        try
                        {
                            field = recordSet.Fields.Item(i);

                            fields.Add(new SapTableFieldResponse
                            {
                                Name = field.Name?.ToString() ?? string.Empty,
                                Type = field.Type?.ToString() ?? string.Empty
                            });
                        }
                        finally
                        {
                            if (field is not null &&
                                Marshal.IsComObject(field))
                            {
                                Marshal.FinalReleaseComObject(field);
                            }
                        }
                    }

                    _logger.LogInformation(
                        "SAP table schema retrieved for {TableName}. FieldCount={FieldCount}",
                        safeTableName,
                        fields.Count);

                    return new SapTableSchemaResponse
                    {
                        TableName = safeTableName,
                        Fields = fields
                    };
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to retrieve schema for SAP table '{tableName}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (recordSet is not null &&
                        Marshal.IsComObject(recordSet))
                    {
                        Marshal.FinalReleaseComObject(recordSet);
                    }
                }
            });
        }


        public Task<IReadOnlyList<string>> GetTableListAsync()
        {
            return StaTaskRunner.RunAsync<IReadOnlyList<string>>(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? recordSet = null;

                try
                {
                    recordSet = handle.GetBusinessObjectFromCandidates(
                        SapObjectTypes.BoRecordsetCandidates,
                        "get SAP table list");

                    string query = @"
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

                    recordSet.DoQuery(query);

                    var tables = new List<string>();

                    if (!(bool)recordSet.EoF)
                    {
                        recordSet.MoveFirst();

                        while (!(bool)recordSet.EoF)
                        {
                            string schema =
                                recordSet.Fields.Item("TABLE_SCHEMA").Value?.ToString()
                                ?? string.Empty;

                            string tableName =
                                recordSet.Fields.Item("TABLE_NAME").Value?.ToString()
                                ?? string.Empty;

                            tables.Add($"{schema}.{tableName}");

                            recordSet.MoveNext();
                        }
                    }

                    return (IReadOnlyList<string>)tables;
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to retrieve SAP table list: {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (recordSet is not null &&
                        Marshal.IsComObject(recordSet))
                    {
                        Marshal.FinalReleaseComObject(recordSet);
                    }
                }
            });
        }

        private static void ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    "SAP table name is required.");
            }

            string value = tableName.Trim();

            /*
             * SAP table names such as:
             *
             * OITM
             * OCRD
             * OWHS
             * OSTC
             * OSLP
             * OITB
             *
             * are normally composed of letters, numbers and underscore.
             *
             * This validation prevents SQL injection through the table name.
             */
            if (!Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9_]*$"))
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"Invalid SAP table name '{tableName}'.");
            }

            if (value.Length > 128)
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"SAP table name '{tableName}' is too long.");
            }
        }

        private static object? ConvertValue(object? value)
        {
            if (value is null || value == DBNull.Value)
            {
                return null;
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset;
            }

            if (value is decimal ||
                value is double ||
                value is float ||
                value is int ||
                value is long ||
                value is short ||
                value is byte ||
                value is bool ||
                value is string)
            {
                return value;
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
