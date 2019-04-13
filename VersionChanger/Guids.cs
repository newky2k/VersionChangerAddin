// Guids.cs
// MUST match guids.h
using System;

namespace DSoft.VersionChanger
{
    static class GuidList
    {
        public const string guidVersionChangerPkgString = "e899704b-fe71-4936-8ab3-a392385423ef";
        public const string guidVersionChangerCmdSetString = "bcada3cf-bf41-4378-a7e6-7f84110d4714";

        public static readonly Guid guidVersionChangerCmdSet = new Guid(guidVersionChangerCmdSetString);
    };
}