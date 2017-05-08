﻿namespace InfoCarrier.Core.FunctionalTests.InMemory
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Specification.Tests;

    public class NotificationEntitiesInfoCarrierTest
        : NotificationEntitiesTestBase<TestStoreBase, NotificationEntitiesInfoCarrierTest.NotificationEntitiesInfoCarrierFixture>
    {
        public NotificationEntitiesInfoCarrierTest(NotificationEntitiesInfoCarrierFixture fixture)
            : base(fixture)
        {
        }

        public class NotificationEntitiesInfoCarrierFixture : NotificationEntitiesFixtureBase, IDisposable
        {
            private readonly InfoCarrierTestHelper<DbContext> helper;
            private TestStoreBase testStore;

            public NotificationEntitiesInfoCarrierFixture()
            {
                this.helper = InMemoryTestStore<DbContext>.CreateHelper(
                    this.OnModelCreating,
                    opt => new DbContext(opt),
                    _ => this.EnsureCreated());
            }

            public override DbContext CreateContext()
                => this.helper.CreateInfoCarrierContext(this.testStore);

            public override TestStoreBase CreateTestStore()
                => this.testStore = this.helper.CreateTestStore();

            public void Dispose()
            {
                this.testStore?.Dispose();
            }
        }
    }
}