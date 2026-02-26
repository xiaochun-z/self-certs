This can help you create a new project based on this template.
you need to install this template first before using it.

## feature
* dev container support.
* docker compose file support
* postgres:18 support
* .NET 10.0

```bash
# install template
dotnet new install .

# use template
# -n 参数后面跟新项目的名称
dotnet new selfcerts-template -n MyGreatApp -o ./MyGreatApp

# uninstall template
dotnet new uninstall .
```