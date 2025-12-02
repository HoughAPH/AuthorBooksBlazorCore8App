# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade SharedClassLibrary\SharedClassLibrary.csproj
4. Upgrade BlazorWebAssemblyApp\BlazorWebAssemblyApp.csproj
5. Upgrade IdentityManagerServerApi\IdentityManagerServerApi.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                        | Current Version | New Version | Description                                   |
|:----------------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer        | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Components.Authorization        | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter | 9.0.7 | 10.0.0 | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Components.WebAssembly          | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer| 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore    | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore                        | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.SqlServer              | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.Tools                  | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |
| System.Text.Json                                     | 9.0.7           | 10.0.0      | Recommended for .NET 10.0                      |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### SharedClassLibrary\SharedClassLibrary.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - No direct NuGet packages listed for this project in analysis.

Other changes:
  - None

#### BlazorWebAssemblyApp\BlazorWebAssemblyApp.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Components.Authorization should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.WebAssembly should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - System.Text.Json should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)

Other changes:
  - None

#### IdentityManagerServerApi\IdentityManagerServerApi.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.7` to `10.0.0` (*recommended for .NET 10.0*)

Other changes:
  - None
