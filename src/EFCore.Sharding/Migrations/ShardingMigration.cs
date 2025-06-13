﻿using EFCore.Sharding.Config;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EFCore.Sharding
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<挂起>")]
    internal class ShardingMigration : MigrationsModelDiffer
    {

#if NET9_0
        public ShardingMigration(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotationProvider,IRelationalAnnotationProvider relationalAnnotationProvider, IRowIdentityMapFactory rowIdentityMapFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotationProvider, relationalAnnotationProvider,rowIdentityMapFactory, commandBatchPreparerDependencies)
        {
        }
#endif
#if NET8_0|| NET7_0
        public ShardingMigration(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotationProvider, IRowIdentityMapFactory rowIdentityMapFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
        {
        }
#endif
#if NET6_0
        public ShardingMigration(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector, IUpdateAdapterFactory updateAdapterFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {
        }
#endif
#if NETSTANDARD2_1
        public ShardingMigration(IRelationalTypeMappingSource typeMappingSource,IMigrationsAnnotationProvider migrationsAnnotations,IChangeDetector changeDetector,IUpdateAdapterFactory updateAdapterFactory,CommandBatchPreparerDependencies commandBatchPreparerDependencies): base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {

        }
#endif
        public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel source, IRelationalModel target)
        {
            EFCoreShardingOptions shardingOption = Cache.RootServiceProvider.GetService<IOptions<EFCoreShardingOptions>>().Value;
            List<MigrationOperation> sourceOperations = base.GetDifferences(source, target).ToList();

            //忽略外键
            if (shardingOption.MigrationsWithoutForeignKey)
            {
                _ = sourceOperations.RemoveAll(x => x is AddForeignKeyOperation or DropForeignKeyOperation);
                foreach (CreateTableOperation operation in sourceOperations.OfType<CreateTableOperation>())
                {
                    operation.ForeignKeys?.Clear();
                }
            }

            return sourceOperations;
        }
    }
}