﻿//  Copyright 2021 Google LLC. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NtApiDotNet.Net.Firewall
{
    /// <summary>
    /// Token information for a condition.
    /// </summary>
    public struct FirewallTokenInformation
    {
        /// <summary>
        /// The list of SIDs.
        /// </summary>
        public IReadOnlyList<UserGroup> Sids { get; }
        /// <summary>
        /// The list of restricted SIDs.
        /// </summary>
        public IReadOnlyList<UserGroup> RestrictedSids { get; }

        private static IReadOnlyList<UserGroup> ReadSids(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count == 0)
            {
                return new List<UserGroup>().AsReadOnly();
            }
            SafeHGlobalBuffer buffer = new SafeHGlobalBuffer(ptr, 1, false);
            buffer.Initialize<SidAndAttributes>((uint)count);
            return buffer.ReadArray<SidAndAttributes>(0, count).Select(s => s.ToUserGroup()).ToList().AsReadOnly();
        }

        internal FirewallTokenInformation(FWP_TOKEN_INFORMATION token_info)
        {
            Sids = ReadSids(token_info.sids, token_info.sidCount);
            RestrictedSids = ReadSids(token_info.restrictedSids, token_info.restrictedSidCount);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sids">The list of SIDs.</param>
        /// <param name="restricted_sids">The list of restricted SIDs.</param>
        public FirewallTokenInformation(IEnumerable<UserGroup> sids, IEnumerable<UserGroup> restricted_sids)
        {
            Sids = sids.ToList().AsReadOnly();
            RestrictedSids = restricted_sids.ToList().AsReadOnly();
        }

        internal FWP_TOKEN_INFORMATION ToStruct(DisposableList list)
        {
            FWP_TOKEN_INFORMATION ret = new FWP_TOKEN_INFORMATION();

            if (Sids.Count > 0)
            {
                TokenGroupsBuilder builder = new TokenGroupsBuilder(Sids);
                ret.sidCount = Sids.Count;
                ret.sids = list.AddResource(builder.ToBuffer()).DangerousGetHandle();
            }

            if (RestrictedSids.Count > 0)
            {
                TokenGroupsBuilder builder = new TokenGroupsBuilder(RestrictedSids);
                ret.restrictedSidCount = RestrictedSids.Count;
                ret.restrictedSids = list.AddResource(builder.ToBuffer()).DangerousGetHandle();
            }

            return ret;
        }
    }
}
