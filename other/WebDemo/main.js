// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { dotnet } from './dotnet.js'

await dotnet
    .withDebugging(1)
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

dotnet.instance.Module['canvas'] = document.getElementById('canvas');

// We're ready to dotnet.run, so let's remove the spinner
const loading_div = document.getElementById('spinner');
loading_div.remove();

await dotnet.run();
