// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Microsoft.Tye.Hosting
{
    public class SharedConfigurationOptions
    {
        public bool ShareConfigurations { get; set; }
        public string Environment { get; set; }

        public static SharedConfigurationOptions FromArgs(string[] args)
        {
            return new SharedConfigurationOptions
            {
                ShareConfigurations = args.Contains("--share-configs"),
            };
        }
    }
}
