// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

public class LogStructure : MonoBehaviour {
    public virtual string[] GetHeaderColumns()
    {
        return System.Array.Empty<string>();
    }

    public virtual object[] GetData(string inputType, string inputStatus)
    {
        return System.Array.Empty<object>();
    }
}