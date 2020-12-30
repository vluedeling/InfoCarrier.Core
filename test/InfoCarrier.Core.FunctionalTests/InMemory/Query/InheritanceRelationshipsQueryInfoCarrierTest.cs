﻿// Copyright (c) Alexander Zabluda. All rights reserved.
// Licensed under the MIT license. See license.txt file in the project root for license information.

namespace InfoCarrier.Core.FunctionalTests.InMemory.Query
{
    using InfoCarrier.Core.FunctionalTests.TestUtilities;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.TestUtilities;

    public class InheritanceRelationshipsQueryInfoCarrierTest : InheritanceRelationshipsQueryTestBase<InheritanceRelationshipsQueryInfoCarrierTest.TestFixture>
    {
        public InheritanceRelationshipsQueryInfoCarrierTest(TestFixture fixture)
            : base(fixture)
        {
        }

        public class TestFixture : InheritanceRelationshipsQueryFixtureBase
        {
            private ITestStoreFactory testStoreFactory;

            protected override ITestStoreFactory TestStoreFactory =>
                InfoCarrierTestStoreFactory.EnsureInitialized(
                    ref this.testStoreFactory,
                    InfoCarrierTestStoreFactory.InMemory,
                    this.ContextType,
                    this.OnModelCreating);
        }
    }
}
