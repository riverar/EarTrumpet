﻿using EarTrumpet.DataModel;
using EarTrumpet.Interop;
using EarTrumpet.Interop.Helpers;
using EarTrumpet.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Drawing;

namespace EarTrumpet.UI.Helpers
{
    public class TaskbarIconSource : IIconSource
    {
        enum IconKind
        {
            EarTrumpet = 0,
            EarTrumpet_LightTheme = 1,
            Muted = 120,
            SpeakerZeroBars = 121,
            SpeakerOneBar = 122,
            SpeakerTwoBars = 123,
            SpeakerThreeBars = 124,
            NoDevice = 125,
        }

        public event Action<IIconSource> Changed;

        public Icon Current { get; private set; }

        private readonly DeviceCollectionViewModel _collection;
        private readonly AppSettings _settings;
        private bool _isMouseOver;
        private string _hash;
        private IconKind _kind;

        public TaskbarIconSource(DeviceCollectionViewModel collection, AppSettings settings)
        {
            _collection = collection;
            _settings = settings;

            _settings.UseLegacyIconChanged += (_, __) => CheckForUpdate();
            collection.TrayPropertyChanged += OnTrayPropertyChanged;

            OnTrayPropertyChanged();
        }

        public void OnMouseOverChanged(bool isMouseOver)
        {
            _isMouseOver = isMouseOver;
            CheckForUpdate();
        }

        public void CheckForUpdate()
        {
            var nextHash = GetHash();
            if (nextHash != _hash)
            {
                Trace.WriteLine($"TaskbarIconSource Changed: {nextHash}");
                _hash = nextHash;
                using (var old = Current)
                {
                    Current = SelectAndLoadIcon(_kind);
                    Changed?.Invoke(this);
                }
            }
        }

        private void OnTrayPropertyChanged()
        {
            _kind = IconKindFromDeviceCollection(_collection);
            CheckForUpdate();
        }

        private Icon SelectAndLoadIcon(IconKind kind)
        {
            if (_settings.UseLegacyIcon)
            {
                kind = IconKind.EarTrumpet;
            }

            try
            {
                if (System.Windows.SystemParameters.HighContrast)
                {
                    using (var icon = LoadIcon(kind))
                    {
                        return ColorIconForHighContrast(icon, kind, _isMouseOver);
                    }
                }
                else if (SystemSettings.IsSystemLightTheme)
                {
                    if (kind == IconKind.EarTrumpet)
                    {
                        return LoadIcon(IconKind.EarTrumpet_LightTheme);
                    }
                    else
                    {
                        using (var icon = LoadIcon(kind))
                        {
                            return ColorIconForLightTheme(icon, kind);
                        }
                    }
                }
                else
                {
                    return LoadIcon(kind);
                }
            }
            // Legacy fallback if SndVolSSD.dll icons are unavailable.
            catch (Exception ex) when (kind != IconKind.EarTrumpet)
            {
                Trace.WriteLine($"TaskbarIconSource LoadIcon: {ex}");
                return SelectAndLoadIcon(IconKind.EarTrumpet);
            }
        }

        private static Icon LoadIcon(IconKind kind)
        {
            switch (kind)
            {
                case IconKind.EarTrumpet:
                    return IconHelper.LoadIconForTaskbar($"{App.AssetBaseUri}Tray.ico");
                case IconKind.EarTrumpet_LightTheme:
                    return IconHelper.LoadIconForTaskbar($"{App.AssetBaseUri}Application.ico");
                case IconKind.Muted:
                    return IconHelper.LoadIconForTaskbar(SndVolSSO.GetPath(SndVolSSO.IconId.Muted));
                case IconKind.NoDevice:
                    return IconHelper.LoadIconForTaskbar(SndVolSSO.GetPath(SndVolSSO.IconId.NoDevice));
                case IconKind.SpeakerOneBar:
                    return IconHelper.LoadIconForTaskbar(SndVolSSO.GetPath(SndVolSSO.IconId.SpeakerOneBar));
                case IconKind.SpeakerTwoBars:
                    return IconHelper.LoadIconForTaskbar(SndVolSSO.GetPath(SndVolSSO.IconId.SpeakerTwoBars));
                case IconKind.SpeakerThreeBars:
                    return IconHelper.LoadIconForTaskbar(SndVolSSO.GetPath(SndVolSSO.IconId.SpeakerThreeBars));
                default: throw new NotImplementedException();
            }
        }

        private string GetHash() =>
            $"kind={_kind} " +
            $"{(System.Windows.SystemParameters.HighContrast ? $"hc=true mouse={_isMouseOver} " : "")}" +
            $"dpi={WindowsTaskbar.Dpi} " +
            $"isSysLight={SystemSettings.IsSystemLightTheme} " +
            $"isLegacy={_settings.UseLegacyIcon}";

        // Only fill part of the icon, so we can preserve the red X.
        private static double GetIconFillPercent(IconKind kind) => kind == IconKind.NoDevice ? 0.4 : 1;

        private static Icon ColorIconForLightTheme(Icon darkIcon, IconKind kind)
        {
            return IconHelper.ColorIcon(darkIcon, GetIconFillPercent(kind), System.Windows.Media.Colors.Black);
        }

        private static Icon ColorIconForHighContrast(Icon darkIcon, IconKind kind, bool isMouseOver)
        {
            return IconHelper.ColorIcon(darkIcon, GetIconFillPercent(kind),
                isMouseOver ? System.Windows.SystemColors.HighlightTextColor : System.Windows.SystemColors.WindowTextColor);
        }

        private static IconKind IconKindFromDeviceCollection(DeviceCollectionViewModel collectionViewModel)
        {
            if (collectionViewModel.Default != null)
            {
                switch (collectionViewModel.Default.IconKind)
                {
                    case DeviceViewModel.DeviceIconKind.Mute:
                        return IconKind.Muted;
                    case DeviceViewModel.DeviceIconKind.Bar1:
                        return IconKind.SpeakerOneBar;
                    case DeviceViewModel.DeviceIconKind.Bar2:
                        return IconKind.SpeakerTwoBars;
                    case DeviceViewModel.DeviceIconKind.Bar3:
                        return IconKind.SpeakerThreeBars;
                    default: throw new NotImplementedException();
                }
            }
            return IconKind.NoDevice;
        }
    }
}
