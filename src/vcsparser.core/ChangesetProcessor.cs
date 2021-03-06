﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vcsparser.core.bugdatabase;

namespace vcsparser.core
{
    public class ChangesetProcessor : IChangesetProcessor
    {
        private Dictionary<DateTime, Dictionary<string, DailyCodeChurn>> dict = new Dictionary<DateTime, Dictionary<string, DailyCodeChurn>>();
        public Dictionary<DateTime, Dictionary<string, DailyCodeChurn>> Output {
            get { return dict; }
        }

        private Dictionary<string, string> renameCache = new Dictionary<string, string>();

        private Dictionary<string, List<WorkItem>> workItemCache = new Dictionary<string, List<WorkItem>>();
        public Dictionary<string, List<WorkItem>> WorkItemCache {
            get { return workItemCache; }
        }

        private List<Regex> bugRegexes;

        private ILogger logger;

        public int ChangesetsWithBugs {
            get; private set;
        }

        public ChangesetProcessor(string bugRegexes, ILogger logger)
        {
            this.bugRegexes = new List<Regex>();
            if (bugRegexes != null)
            {
                foreach (var r in bugRegexes.Split(';'))
                    this.bugRegexes.Add(new Regex(r));
            }
            this.logger = logger;
        }

        public void ProcessChangeset(IChangeset changeset)
        {
            if (changeset == null)
                return;

            UpdateRenameCache(changeset);

            if (!dict.ContainsKey(changeset.ChangesetTimestamp.Date))
                dict.Add(changeset.ChangesetTimestamp.Date, new Dictionary<string, DailyCodeChurn>());

            bool containsBugs = CheckAndIncrementIfChangesetContainsBug(changeset);

            foreach (var c in changeset.ChangesetFileChanges)
            {
                ProcessFileChange(changeset, containsBugs, c);

                if (workItemCache.ContainsKey(changeset.ChangesetIdentifier.ToString()))
                    ProcessBugDatabaseFileChange(changeset, c);
            }
        }

        private void ProcessFileChange(IChangeset changeset, bool containsBugs, FileChanges c)
        {
            var fileName = GetFileNameConsideringRenames(c.FileName);

            DailyCodeChurn dailyCodeChurn = FindOrCreateDailyCodeChurnForFileAndDate(changeset, fileName);

            ProcessChanges(c, dailyCodeChurn);
            ProcessAuthor(changeset, dailyCodeChurn);
            if (containsBugs)
            {
                ProcessChangesInFixes(c, dailyCodeChurn);
            }
        }

        private void ProcessBugDatabaseFileChange(IChangeset changeset, FileChanges c)
        {
            var fileName = GetFileNameConsideringRenames(c.FileName);

            DailyCodeChurn dailyCodeChurn = FindOrCreateDailyCodeChurnForFileAndDate(changeset, fileName);
            if (dailyCodeChurn.BugDatabase == null)
                dailyCodeChurn.BugDatabase = new DailyCodeChurnBugDatabase();

            ProcessBugDatabaseChanges(c, dailyCodeChurn);
        }

        private DailyCodeChurn FindOrCreateDailyCodeChurnForFileAndDate(IChangeset changeset, string fileName)
        {
            if (!dict[changeset.ChangesetTimestamp.Date].ContainsKey(fileName))
                dict[changeset.ChangesetTimestamp.Date].Add(fileName, new DailyCodeChurn());

            var dailyCodeChurn = dict[changeset.ChangesetTimestamp.Date][fileName];
            dailyCodeChurn.Timestamp = changeset.ChangesetTimestamp.Date.ToString(DailyCodeChurn.DATE_FORMAT, CultureInfo.InvariantCulture);
            dailyCodeChurn.FileName = fileName;
            return dailyCodeChurn;
        }

        private static void ProcessChanges(FileChanges c, DailyCodeChurn dailyCodeChurn)
        {
            dailyCodeChurn.Added += c.Added;
            dailyCodeChurn.Deleted += c.Deleted;
            dailyCodeChurn.ChangesBefore += c.ChangedBefore;
            dailyCodeChurn.ChangesAfter += c.ChangedAfter;
            dailyCodeChurn.NumberOfChanges += 1;
        }

        private static void ProcessBugDatabaseChanges(FileChanges c, DailyCodeChurn dailyCodeChurn)
        {
            dailyCodeChurn.BugDatabase.NumberOfChangesInFixes++;
            dailyCodeChurn.BugDatabase.AddedInFixes += c.Added;
            dailyCodeChurn.BugDatabase.DeletedInFixes += c.Deleted;
            dailyCodeChurn.BugDatabase.ChangesBeforeInFixes += c.ChangedBefore;
            dailyCodeChurn.BugDatabase.ChangesAfterInFixes += c.ChangedAfter;
        }

        private static void ProcessChangesInFixes(FileChanges c, DailyCodeChurn dailyCodeChurn)
        {
            dailyCodeChurn.NumberOfChangesWithFixes++;
            dailyCodeChurn.AddedWithFixes += c.Added;
            dailyCodeChurn.DeletedWithFixes += c.Deleted;
            dailyCodeChurn.ChangesBeforeWithFixes += c.ChangedBefore;
            dailyCodeChurn.ChangesAfterWithFixes += c.ChangedAfter;
        }

        private static void ProcessAuthor(IChangeset changeset, DailyCodeChurn dailyCodeChurn)
        {
            var author = dailyCodeChurn.Authors.Where(a => a.Author.ToUpper() == changeset.ChangesetAuthor.ToUpper()).FirstOrDefault();
            if (author != null)
                author.NumberOfChanges++;
            else
                dailyCodeChurn.Authors.Add(new DailyCodeChurnAuthor()
                {
                    Author = changeset.ChangesetAuthor,
                    NumberOfChanges = 1
                });
        }

        private bool CheckIfChangesetContainsBug(IChangeset changeset)
        {
            if (String.IsNullOrEmpty(changeset.ChangesetMessage))
                return false;

            foreach (var regex in this.bugRegexes)
                if (regex.IsMatch(changeset.ChangesetMessage))
                    return true;

            return false;
        }

        private bool CheckAndIncrementIfChangesetContainsBug(IChangeset changeset)
        {
            if (CheckIfChangesetContainsBug(changeset))
            {
                ChangesetsWithBugs++;
                return true;
            }
            return false;
        }

        private void UpdateRenameCache(IChangeset changeset)
        {
            foreach (var pair in changeset.ChangesetFileRenames)
            {
                string value = GetDestinationFileFollowingRenames(pair.Value);

                if (!renameCache.ContainsKey(pair.Key))
                    renameCache.Add(pair.Key, value);
                else
                    renameCache[pair.Key] = value;
            }
        }

        private string GetDestinationFileFollowingRenames(string fileName, string finalFileName = null)
        {
            if (finalFileName == fileName)
                return fileName;

            if (renameCache.ContainsKey(fileName))
                return GetDestinationFileFollowingRenames(renameCache[fileName], fileName);
            else
                return fileName;

        }

        private string GetFileNameConsideringRenames(string fileName)
        {
            if (renameCache.ContainsKey(fileName))
                return renameCache[fileName];

            return fileName;
        }
    }
}
