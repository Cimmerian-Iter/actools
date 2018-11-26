﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.Helpers;
using AcManager.Tools.Managers.Directories;
using AcManager.Tools.Objects;
using AcTools.DataFile;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Tools.Managers {
    public class FontsManager : AcManagerFileSpecific<FontObject> {
        private static FontsManager _instance;

        public static FontsManager Instance => _instance ?? (_instance = new FontsManager());

        [CanBeNull]
        public FontObject GetByAcId(string v) {
            return GetById(v + FontObject.FontExtension);
        }

        public FontsManager() {
            SettingsHolder.Content.PropertyChanged += Content_PropertyChanged;
        }

        private void Content_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SettingsHolder.ContentSettings.FontIconCharacter)) {
                foreach (var fontObject in Loaded) {
                    fontObject.ResetIconBitmap();
                }
            }
        }

        public override string SearchPattern => @"*.txt";

        public override string[] AttachedExtensions => FontObject.BitmapExtensions;

        protected override string CheckIfIdValid(string id) {
            if (!id.EndsWith(FontObject.FontExtension, StringComparison.OrdinalIgnoreCase)) {
                return $"ID should end with “{FontObject.FontExtension}”.";
            }

            return base.CheckIfIdValid(id);
        }

        public override FontObject GetDefault() {
            var v = WrappersList.FirstOrDefault(x => x.Value.Id.Contains(@"arial"));
            return v == null ? base.GetDefault() : EnsureWrapperLoaded(v);
        }

        public override IAcDirectories Directories => AcRootDirectory.Instance.FontsDirectories;

        protected override FontObject CreateAcObject(string id, bool enabled) {
            return new FontObject(this, id, enabled);
        }

        protected override bool ShouldSkipFile(string objectLocation, string filename) {
            return !FontObject.BitmapExtensions.Any(x => filename.EndsWith(x, StringComparison.OrdinalIgnoreCase)) &&
                   !filename.EndsWith(FontObject.FontExtension, StringComparison.OrdinalIgnoreCase);
        }

        public override IEnumerable<string> GetAttachedFiles(string location) {
            return FontObject.BitmapExtensions.Select(ext =>
                    location.ApartFromLast(FontObject.FontExtension, StringComparison.OrdinalIgnoreCase) + ext);
        }

        public DateTime? LastUsingsRescan {
            get => ValuesStorage.Get<DateTime?>("FontsManager.LastUsingsRescan");
            set {
                if (Equals(value, LastUsingsRescan)) return;

                if (value.HasValue) {
                    ValuesStorage.Set("FontsManager.LastUsingsRescan", value.Value);
                } else {
                    ValuesStorage.Remove("FontsManager.LastUsingsRescan");
                }

                OnPropertyChanged();
            }
        }

        public async Task<List<string>> UsingsRescan(IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default) {
            try {
                await EnsureLoadedAsync();
                if (cancellation.IsCancellationRequested) return null;

                await CarsManager.Instance.EnsureLoadedAsync();
                if (cancellation.IsCancellationRequested) return null;

                var i = 0;
                var cars = CarsManager.Instance.Loaded.ToList();

                var list = (await cars.Select(async car => {
                    if (cancellation.IsCancellationRequested) return null;

                    progress?.Report(car.DisplayName, i++, cars.Count);
                    return new {
                        CarId = car.Id,
                        FontIds = (await Task.Run(() => car.AcdData?.GetIniFile(@"digital_instruments.ini"), cancellation))
                                .Values.Select(x => x.GetNonEmpty("FONT")?.ToLowerInvariant()).NonNull().ToList()
                    };
                }).WhenAll(12, cancellation)).Where(x => x != null && x.FontIds.Count > 0).ToListIfItIsNot();

                if (cancellation.IsCancellationRequested) return null;
                foreach (var fontObject in Loaded) {
                    fontObject.UsingsCarsIds = list.Where(x => x.FontIds.Contains(fontObject.AcId)).Select(x => x.CarId).ToArray();
                }

                return list.SelectMany(x => x.FontIds).Distinct().Where(id => GetWrapperById(id + FontObject.FontExtension) == null).ToList();
            } catch (Exception e) when (e.IsCancelled()) {
                return null;
            } catch (Exception e) {
                NonfatalError.Notify(ToolsStrings.Fonts_RescanUsings, e);
                return null;
            } finally {
                LastUsingsRescan = DateTime.Now;
            }
        }

        private CommandBase _usedRescanCommand;

        public ICommand UsingsRescanCommand => _usedRescanCommand ?? (_usedRescanCommand = new AsyncCommand(() => UsingsRescan()));
    }
}