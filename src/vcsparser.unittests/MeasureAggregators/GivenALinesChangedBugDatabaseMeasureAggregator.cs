﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vcsparser.core;
using vcsparser.core.MeasureAggregators;
using Xunit;

namespace vcsparser.unittests.MeasureAggregators
{
    public class GivenALinesChangedBugDatabaseMeasureAggregator
    {
        private LinesChangedInFixesBugDatabaseMeasureAggregator measureAggregator;

        public GivenALinesChangedBugDatabaseMeasureAggregator()
        {
            this.measureAggregator = new LinesChangedInFixesBugDatabaseMeasureAggregator();
        }

        [Fact]
        public void WhenCallingHasValueAndNoBugDatabaseValueShouldReturnFalse()
        {
            var dailyCodeChurn = new DailyCodeChurn()
            {
                Timestamp = "2018/09/17 00:00:00",
                FileName = "file1"
            };

            Assert.False(this.measureAggregator.HasValue(dailyCodeChurn));
        }

        [Fact]
        public void WhenCallingHasValueAndNoValueShouldReturnFalse()
        {
            var dailyCodeChurn = new DailyCodeChurn()
            {
                Timestamp = "2018/09/17 00:00:00",
                FileName = "file1",
                BugDatabase = new DailyCodeChurnBugDatabase
                {
                    AddedInFixes = 0,
                    DeletedInFixes = 0
                }
            };

            Assert.False(this.measureAggregator.HasValue(dailyCodeChurn));
        }

        [Fact]
        public void WhenCallingAHasValueAndHasValueShouldReturnTrue()
        {
            var dailyCodeChurn = new DailyCodeChurn()
            {
                Timestamp = "2018/09/17 00:00:00",
                FileName = "file1",
                BugDatabase = new DailyCodeChurnBugDatabase
                {
                    AddedInFixes = 6,
                    DeletedInFixes = 1
                }
            };

            Assert.True(this.measureAggregator.HasValue(dailyCodeChurn));
        }

        [Fact]
        public void WhenGettingValueForNewMeasureShouldReturnNumberOfChanges()
        {
            var dailyCodeChurn = new DailyCodeChurn()
            {
                Timestamp = "2018/09/17 00:00:00",
                FileName = "file1",
                BugDatabase = new DailyCodeChurnBugDatabase
                {
                    AddedInFixes = 6,
                    DeletedInFixes = 1
                }
            };

            Assert.Equal(7, this.measureAggregator.GetValueForNewMeasure(dailyCodeChurn));
        }

        [Fact]
        public void WhenGettingValueForExistingMeasureShouldSumWithExistingValue()
        {
            var dailyCodeChurn = new DailyCodeChurn()
            {
                Timestamp = "2018/09/17 00:00:00",
                FileName = "file1",
                BugDatabase = new DailyCodeChurnBugDatabase
                {
                    AddedInFixes = 6,
                    DeletedInFixes = 1
                }
            };

            var measure = new Measure<int>();
            measure.Value = 3;
            Assert.Equal(10, this.measureAggregator.GetValueForExistingMeasure(dailyCodeChurn, measure));
        }

    }
}
