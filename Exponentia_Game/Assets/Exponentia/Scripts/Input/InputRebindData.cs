/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:30
 * DESCRIPTION: Serializable container for persisted input binding overrides.
 */

using System;

namespace Exponentia.InputSystem
{
    [Serializable]
    public sealed class InputRebindData
    {
        public int schemaVersion = 1;
        public string bindingOverridesJson = string.Empty;
        public string savedAtUtc = string.Empty;
    }
}
