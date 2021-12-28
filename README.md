# Diga.Core.Json
This project is based on the great work of:

https://github.com/smourier/ZeroDepJson

The only difference is that we have transferred the classes to their own files and changed the namespaces and class names so that there are no conflicts with other serializers. We urgently needed a simple serializer that would work across all frameworks. We have explicitly omitted NewtonSoft.Json(https://www.nuget.org/packages/Newtonsoft.Json/) because some users also want to use other serializers such as MS System.Text.Json(https://www.nuget.org/packages/System.Text.Json/).

