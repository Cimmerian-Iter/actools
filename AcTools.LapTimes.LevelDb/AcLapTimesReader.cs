﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AcTools.Utils;

namespace AcTools.LapTimes.LevelDb {
    public class AcLapTimesReader : ILapTimesReader {
        private readonly string _sourceDirectory;
        private ChromiumDbWrapper _wrapper;
        private string _tempDirectory;

        private static string GetDatabaseDirectory(string acDocumentsDirectory) {
            return Path.Combine(acDocumentsDirectory, @"launcherdata\IndexedDB\file__0.indexeddb.leveldb");
        }

        public AcLapTimesReader(string acDocumentsDirectory) {
            _sourceDirectory = GetDatabaseDirectory(acDocumentsDirectory);
        }

        private void Prepare() {
            if (_wrapper != null) return;

            _tempDirectory = FileUtils.EnsureUnique(Path.Combine(Path.GetTempPath(), "_laptimesreader"));
            if (Directory.Exists(_sourceDirectory)) {
                FileUtils.CopyRecursive(_sourceDirectory, _tempDirectory);
            }

            try {
                _wrapper = new ChromiumDbWrapper(_tempDirectory);
            } catch (Exception) {
                DisposeTempDirectory();
                throw;
            }
        }

        public DateTime GetLastModified() {
            var directory = new DirectoryInfo(_sourceDirectory);
            return directory.Exists ? directory.GetFiles().Select(f => f.LastWriteTime)
                                               .OrderByDescending(f => f).First() : default;
        }

        public IEnumerable<LapTimeEntry> Import(string sourceName) {
            Prepare();
            return _wrapper.GetData().Select(bits => {
                string carId, trackId, date, time;
                return bits.TryGetValue("car", out carId) && bits.TryGetValue("track", out trackId) &&
                        bits.TryGetValue("date", out date) && bits.TryGetValue("time", out time)
                        ? new LapTimeEntry(
                                sourceName, carId, trackId,
                                new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(double.Parse(date, CultureInfo.InvariantCulture)),
                                TimeSpan.FromMilliseconds(double.Parse(time, CultureInfo.InvariantCulture)))
                        : null;
            }).Where(x => x != null);
        }

        public void Export(IEnumerable<LapTimeEntry> entries) {
            throw new NotSupportedException();
        }

        public void Remove(string carId, string trackAcId) {
            throw new NotSupportedException();
        }

        private void DisposeTempDirectory() {
            if (_tempDirectory == null) return;
            try {
                if (Directory.Exists(_tempDirectory)) {
                    Directory.Delete(_tempDirectory, true);
                }
            } catch (Exception) {
                // ignored
            }
        }

        public void Dispose() {
            _wrapper?.Dispose();
            DisposeTempDirectory();
        }

        public bool CanExport => false;
        public bool CanStay => false;
    }
}
