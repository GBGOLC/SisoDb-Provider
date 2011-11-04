﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using EnsureThat;
using ErikEJ.SqlCe;
using NCore;
using PineCone.Structures;
using PineCone.Structures.Schemas;
using SisoDb.Dac;
using SisoDb.Querying.Sql;
using SisoDb.Structures;

namespace SisoDb.SqlCe4.Dac
{
    public class SqlCe4DbClient : DbClientBase
    {
        public SqlCe4DbClient(ISisoConnectionInfo connectionInfo, bool transactional)
            : base(connectionInfo, transactional, () => new SqlCeConnection(connectionInfo.ConnectionString.PlainString))
        {
        }

        public override IDbBulkCopy GetBulkCopy(bool keepIdentities)
        {
            var options = keepIdentities ? SqlCeBulkCopyOptions.KeepIdentity : SqlCeBulkCopyOptions.None;

            return new SqlCe4DbBulkCopy((SqlCeConnection)Connection, options, (SqlCeTransaction)Transaction);
        }

        public override void Drop(IStructureSchema structureSchema)
        {
            var indexesTableExists = TableExists(structureSchema.GetIndexesTableName());
            var uniquesTableExists = TableExists(structureSchema.GetUniquesTableName());
            var structureTableExists = TableExists(structureSchema.GetStructureTableName());

            var sqlDropTableFormat = SqlStatements.GetSql("DropTable");
            
            using (var cmd = CreateCommand(CommandType.Text, string.Empty, new DacParameter("entityHash", structureSchema.Hash)))
            {
                if (indexesTableExists)
                {
                    cmd.CommandText = sqlDropTableFormat.Inject(structureSchema.GetIndexesTableName());
                    cmd.ExecuteNonQuery();
                }

                if (uniquesTableExists)
                {
                    cmd.CommandText = sqlDropTableFormat.Inject(structureSchema.GetUniquesTableName());
                    cmd.ExecuteNonQuery();
                }

                if (structureTableExists)
                {
                    cmd.CommandText = sqlDropTableFormat.Inject(structureSchema.GetStructureTableName());
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = SqlStatements.GetSql("DeleteStructureFromSisoDbIdentitiesTable");
                cmd.ExecuteNonQuery();
            }
        }

        public override void RebuildIndexes(IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("RebuildIndexes").Inject(
                structureSchema.GetStructureTableName(),
                structureSchema.GetIndexesTableName(),
                structureSchema.GetUniquesTableName());

            using (var cmd = CreateCommand(CommandType.Text, sql))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public override void DeleteById(ValueType structureId, IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("DeleteById").Inject(
                structureSchema.GetStructureTableName());

            using (var cmd = CreateCommand(CommandType.Text, sql, new DacParameter("id", structureId)))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public override void DeleteByIds(IEnumerable<ValueType> ids, StructureIdTypes idType, IStructureSchema structureSchema)
        {
            throw new SisoDbNotSupportedByProviderException(StorageProviders.SqlCe4, "DeleteByIds.");
            //Ensure.That(structureSchema, "structureSchema").IsNotNull();

            //var sql = SqlStatements.GetSql("DeleteByIds").Inject(
            //    structureSchema.GetStructureTableName());

            //using (var cmd = CreateCommand(CommandType.Text, sql))
            //{
            //    cmd.Parameters.Add(Sql2008IdsTableParam.CreateIdsTableParam(idType, ids));
            //    cmd.ExecuteNonQuery();
            //}
        }

        public override void DeleteByQuery(SqlQuery query, Type idType, IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("DeleteByQuery").Inject(
                structureSchema.GetStructureTableName(),
                structureSchema.GetIndexesTableName(),
                query.Sql);

            using (var cmd = CreateCommand(CommandType.Text, sql, query.Parameters.ToArray()))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public override void DeleteWhereIdIsBetween(ValueType structureIdFrom, ValueType structureIdTo, IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("DeleteWhereIdIsBetween").Inject(
                structureSchema.GetStructureTableName());

            using (var cmd = CreateCommand(CommandType.Text, sql, new DacParameter("idFrom", structureIdFrom), new DacParameter("idTo", structureIdTo)))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public override bool TableExists(string name)
        {
            Ensure.That(name, "name").IsNotNullOrWhiteSpace();

            var sql = SqlStatements.GetSql("TableExists");
            var value = ExecuteScalar<int>(CommandType.Text, sql, new DacParameter("tableName", name));

            return value > 0;
        }

        public override int RowCount(IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("RowCount").Inject(structureSchema.GetStructureTableName());

            return ExecuteScalar<int>(CommandType.Text, sql);
        }

        public override int RowCountByQuery(IStructureSchema structureSchema, SqlQuery query)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("RowCountByQuery").Inject(
                structureSchema.GetIndexesTableName(),
                query.Sql);

            return ExecuteScalar<int>(CommandType.Text, sql, query.Parameters.ToArray());
        }

        public override long CheckOutAndGetNextIdentity(string entityHash, int numOfIds)
        {
            Ensure.That(entityHash, "entityHash").IsNotNullOrWhiteSpace();

            var sql = SqlStatements.GetSql("Sys_Identities_CheckOutAndGetNextIdentity");

            return ExecuteScalar<long>(CommandType.Text, sql,
                new DacParameter("entityHash", entityHash),
                new DacParameter("numOfIds", numOfIds));
        }

        public override IEnumerable<string> GetJson(IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("GetAllById").Inject(structureSchema.GetStructureTableName());

            using (var cmd = CreateCommand(CommandType.Text, sql))
            {
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                    reader.Close();
                }
            }
        }

        public override string GetJsonById(ValueType structureId, IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("GetById").Inject(structureSchema.GetStructureTableName());

            return ExecuteScalar<string>(CommandType.Text, sql, new DacParameter("id", structureId));
        }

        public override IEnumerable<string> GetJsonByIds(IEnumerable<ValueType> ids, StructureIdTypes idType, IStructureSchema structureSchema)
        {
            throw new SisoDbNotSupportedByProviderException(StorageProviders.SqlCe4, "GetJsonByIds.");
            //Ensure.That(structureSchema, "structureSchema").IsNotNull();

            //var sql = SqlStatements.GetSql("GetByIds").Inject(structureSchema.GetStructureTableName());

            //using (var cmd = CreateCommand(CommandType.Text, sql))
            //{
            //    cmd.Parameters.Add(Sql2008IdsTableParam.CreateIdsTableParam(idType, ids));

            //    using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
            //    {
            //        while (reader.Read())
            //        {
            //            yield return reader.GetString(0);
            //        }
            //        reader.Close();
            //    }
            //}
        }

        public override IEnumerable<string> GetJsonWhereIdIsBetween(ValueType structureIdFrom, ValueType structureIdTo, IStructureSchema structureSchema)
        {
            Ensure.That(structureSchema, "structureSchema").IsNotNull();

            var sql = SqlStatements.GetSql("GetJsonWhereIdIsBetween").Inject(structureSchema.GetStructureTableName());

            using (var cmd = CreateCommand(CommandType.Text, sql, new DacParameter("idFrom", structureIdFrom), new DacParameter("idTo", structureIdTo)))
            {
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                    reader.Close();
                }
            }
        }
    }
}