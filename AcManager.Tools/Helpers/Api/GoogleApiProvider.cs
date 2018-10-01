﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using AcManager.Internal;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;
using TimeZoneConverter;

namespace AcManager.Tools.Helpers.Api {
    public class GoogleApiProvider {
        private const string RequestTimeZoneUri = "https://maps.googleapis.com/maps/api/timezone/xml?location={0},{1}&timestamp={2}&key={3}";

        [ItemNotNull]
        public static async Task<TimeZoneInfo> DetermineTimeZoneAsync(GeoTagsEntry geoTags) {
            var requestUri = string.Format(RequestTimeZoneUri, geoTags.LatitudeValue, geoTags.LongitudeValue,
                                           DateTime.Now.ToUnixTimestamp(), InternalUtils.GetGoogleMapsApiCode());

            using (var order = KillerOrder.Create(new WebClient(), 5000)) {
                var data = await order.Victim.DownloadStringTaskAsync(requestUri);
                var doc = XDocument.Parse(data);
                var zoneId = doc.Descendants(@"time_zone_id").FirstOrDefault()?.Value;
                if (zoneId == null) throw new Exception("Invalid response");

                try {
                    return TZConvert.GetTimeZoneInfo(zoneId);
                } catch (TimeZoneNotFoundException) {
                    var rawOffset = doc.Descendants(@"raw_offset").FirstOrDefault()?.Value;
                    var dstOffset = doc.Descendants(@"dst_offset").FirstOrDefault()?.Value ?? @"0";
                    var zoneName = doc.Descendants(@"time_zone_name").FirstOrDefault()?.Value;
                    if (rawOffset == null || zoneName == null) throw new Exception("Invalid response");
                    return TimeZoneInfo.CreateCustomTimeZone(zoneId, TimeSpan.FromSeconds(FlexibleParser.ParseDouble(rawOffset)), zoneName, zoneName);
                }
            }
        }
    }
}
