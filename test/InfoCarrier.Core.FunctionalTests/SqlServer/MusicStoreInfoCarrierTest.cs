﻿// Copyright (c) Alexander Zabluda. All rights reserved.
// Licensed under the MIT license. See license.txt file in the project root for license information.

namespace InfoCarrier.Core.FunctionalTests.SqlServer
{
    using InfoCarrier.Core.FunctionalTests.TestUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.TestUtilities;

    public class MusicStoreInfoCarrierTest : MusicStoreTestBase<MusicStoreInfoCarrierTest.TestFixture>
    {
        public MusicStoreInfoCarrierTest(TestFixture fixture)
            : base(fixture)
        {
        }

        public class TestFixture : MusicStoreFixtureBase
        {
            private ITestStoreFactory testStoreFactory;

            protected override ITestStoreFactory TestStoreFactory =>
                InfoCarrierTestStoreFactory.EnsureInitialized(
                    ref this.testStoreFactory,
                    InfoCarrierTestStoreFactory.SqlServer,
                    this.ContextType,
                    this.OnModelCreating);
        }
    }
}
