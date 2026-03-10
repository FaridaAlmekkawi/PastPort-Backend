using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PastPort.API.Extensions;

/// <summary>
/// Custom filter لـ Swagger لدعم file uploads
/// </summary>
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;

        // البحث عن [FromForm] parameters
        var formFileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null)
            .ToList();

        // إذا ما في file uploads، اتخطى
        if (!formFileParameters.Any())
            return;

        // البحث عن IFormFile parameters
        var fileParams = formFileParameters
            .Where(p => p.ParameterType == typeof(IFormFile) ||
                       (p.ParameterType.IsGenericType &&
                        p.ParameterType.GetGenericArguments().Contains(typeof(IFormFile))))
            .ToList();

        if (!fileParams.Any())
            return;

        // تحديث request body
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    "multipart/form-data",
                    new OpenApiMediaType
                    {
                        Schema = CreateFormDataSchema(context, formFileParameters)
                    }
                }
            }
        };

        // شيل parameters القديمة من الـ operation
        operation.Parameters?.Clear();
    }

    private OpenApiSchema CreateFormDataSchema(OperationFilterContext context, List<ParameterInfo> formFileParameters)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var param in formFileParameters)
        {
            var isFile = param.ParameterType == typeof(IFormFile);
            var isRequired = param.GetCustomAttribute<RequiredAttribute>() != null;

            if (isFile)
            {
                schema.Properties[param.Name!] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = $"Upload file - {param.Name}"
                };

                if (isRequired)
                {
                    schema.Required.Add(param.Name!);
                }
            }
            else
            {
                var paramType = GetOpenApiType(param.ParameterType);

                schema.Properties[param.Name!] = new OpenApiSchema
                {
                    Type = paramType.Type,
                    Format = paramType.Format,
                    Description = $"Field - {param.Name}"
                };

                if (isRequired)
                {
                    schema.Required.Add(param.Name!);
                }
            }
        }

        return schema;
    }

    private (string Type, string? Format) GetOpenApiType(Type type)
    {
        if (type == typeof(string))
            return ("string", null);
        if (type == typeof(int) || type == typeof(int?))
            return ("integer", "int32");
        if (type == typeof(long) || type == typeof(long?))
            return ("integer", "int64");
        if (type == typeof(Guid) || type == typeof(Guid?))
            return ("string", "uuid");
        if (type == typeof(bool) || type == typeof(bool?))
            return ("boolean", null);
        if (type == typeof(decimal) || type == typeof(decimal?))
            return ("number", "double");
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return ("string", "date-time");

        return ("string", null);
    }
}