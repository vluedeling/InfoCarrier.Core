﻿namespace InfoCarrier.Core.FunctionalTests
{
    using System;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
    using Microsoft.Extensions.DependencyInjection;

    public class SqlServerTestStore<TDbContext> : TestStoreImplBase<TDbContext>
        where TDbContext : DbContext
    {
        private readonly Func<TestStoreBase> fromShared;
        private readonly DbConnection connection;
        private DbTransaction transaction;

        private SqlServerTestStore(
            Func<DbContextOptions, TDbContext> createContext,
            Action<IServiceCollection> configureStoreService,
            Action<TDbContext> initializeDatabase,
            string databaseName)
            : base(
                  createContext,
                  c =>
                  {
                      if (initializeDatabase != null)
                      {
                          c.Database.EnsureDeleted();
                          c.Database.EnsureCreated();
                          initializeDatabase(c);
                      }
                  })
        {
            this.connection = new SqlConnection(CreateConnectionString(databaseName, true));

            var serviceCollection = new ServiceCollection().AddEntityFrameworkSqlServer();
            configureStoreService(serviceCollection);

            this.DbContextOptions = new DbContextOptionsBuilder()
                .UseSqlServer(this.connection)
                .UseInternalServiceProvider(serviceCollection.BuildServiceProvider())
                .Options;

            this.fromShared = () => new SqlServerTestStore<TDbContext>(createContext, configureStoreService, null, databaseName);
        }

        protected override DbContextOptions DbContextOptions { get; }

        public override TDbContext CreateContext()
        {
            var context = base.CreateContext();
            context.Database.UseTransaction(this.transaction);
            return context;
        }

        public override TestStoreBase FromShared()
        {
            this.EnsureInitialized();
            return this.fromShared();
        }

        public override void BeginTransaction()
        {
            this.EnsureInitialized();
            this.connection.Open();
            this.transaction = this.connection.BeginTransaction();
        }

        public override async Task BeginTransactionAsync()
        {
            this.EnsureInitialized();
            await this.connection.OpenAsync();
            this.transaction = this.connection.BeginTransaction();
        }

        public override void CommitTransaction()
        {
            this.transaction.Commit();
            this.transaction = null;
            this.connection.Close();
        }

        public override void RollbackTransaction()
        {
            this.transaction.Rollback();
            this.transaction = null;
            this.connection.Close();
        }

        private static string CreateConnectionString(string name, bool multipleActiveResultSets)
        {
            var builder = new SqlConnectionStringBuilder(@"Data Source=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Connect Timeout=30")
            {
                MultipleActiveResultSets = multipleActiveResultSets,
                InitialCatalog = name
            };

            return builder.ToString();
        }

        public static InfoCarrierTestHelper<TDbContext> CreateHelper(
            Action<ModelBuilder> onModelCreating,
            Func<DbContextOptions, TDbContext> createContext,
            Action<TDbContext> initializeDatabase,
            bool useSharedStore,
            string databaseName)
        {
            return CreateTestHelper(
                onModelCreating,
                () => new SqlServerTestStore<TDbContext>(
                    createContext,
                    MakeStoreServiceConfigurator<SqlServerModelSource>(onModelCreating, p => new TestSqlServerModelSource(p)),
                    initializeDatabase,
                    databaseName),
                useSharedStore);
        }

        private class TestSqlServerModelSource : SqlServerModelSource
        {
            private readonly TestModelSourceParams testModelSourceParams;

            public TestSqlServerModelSource(TestModelSourceParams p)
                : base(p.SetFinder, p.CoreConventionSetBuilder, p.ModelCustomizer, p.ModelCacheKeyFactory)
            {
                this.testModelSourceParams = p;
            }

            public override IModel GetModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator)
                => this.testModelSourceParams.GetModel(context, conventionSetBuilder, validator);
        }
    }
}