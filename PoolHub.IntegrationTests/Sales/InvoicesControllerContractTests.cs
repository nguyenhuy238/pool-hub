using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace PoolHub.IntegrationTests;

public class InvoicesControllerContractTests
{
    private static readonly IReadOnlyDictionary<string, ActionContract> ExpectedActions =
        new Dictionary<string, ActionContract>(StringComparer.Ordinal)
        {
            ["Get"] = new(
                "GET",
                "",
                false,
                false,
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("request", "InvoiceQueryRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetById"] = new(
                "GET",
                "{id:long}",
                false,
                true,
                "Task<ActionResult<ApiResponse<InvoiceDetailDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetPaymentMethods"] = new(
                "GET",
                "payment-methods",
                false,
                false,
                "Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>>",
                new[]
                {
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Generate"] = new(
                "POST",
                "generate/{sessionId:long}",
                false,
                false,
                "Task<ActionResult<ApiResponse<InvoiceDto>>>",
                new[]
                {
                    new ParameterContract("sessionId", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Payment"] = new(
                "POST",
                "payments",
                false,
                false,
                "Task<ActionResult<ApiResponse<CreatePaymentResponse>>>",
                new[]
                {
                    new ParameterContract("request", "CreatePaymentRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["ApplyDiscount"] = new(
                "POST",
                "{id:long}/discounts",
                false,
                false,
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "ApplyDiscountRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["RemoveDiscount"] = new(
                "DELETE",
                "{id:long}/discounts",
                false,
                false,
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Cancel"] = new(
                "POST",
                "{id:long}/cancel",
                false,
                false,
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "CancelInvoiceRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["ExportPdf"] = new(
                "GET",
                "{id:long}/export-pdf",
                false,
                false,
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetQrCode"] = new(
                "GET",
                "{id:long}/qr-code",
                true,
                true,
                "Task<IActionResult>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["UpdateProducts"] = new(
                "PUT",
                "{id:long}/products",
                false,
                false,
                "Task<ActionResult<ApiResponse<InvoiceDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "UpdateInvoiceProductsRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["UpdateCustomer"] = new(
                "PUT",
                "{id:long}/customer",
                false,
                false,
                "Task<ActionResult<ApiResponse<InvoiceDetailDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "UpdateInvoiceCustomerRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                })
        };

    [Fact]
    public void InvoicesController_ClassContract_IsStable()
    {
        var controller = GetControllerType();
        var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/invoices", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.NotNull(authorize);
        Assert.Equal("Admin,Manager,Staff", authorize.Roles);
        Assert.True(string.IsNullOrEmpty(authorize.Policy));
        Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void InvoicesController_ActionContracts_AreStable()
    {
        var controller = GetControllerType();
        var actions = controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        Assert.Equal(ExpectedActions.Keys.Order(StringComparer.Ordinal), actions.Keys.Order(StringComparer.Ordinal));

        foreach (var (actionName, expected) in ExpectedActions)
        {
            var action = actions[actionName];
            var http = action.GetCustomAttributes<HttpMethodAttribute>().Single();

            Assert.Empty(action.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(expected.AllowAnonymous, action.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
            Assert.Equal(expected.HttpMethod, Assert.Single(http.HttpMethods));
            Assert.Equal(expected.RouteTemplate, http.Template ?? "");
            Assert.Equal(expected.ReturnType, FormatType(action.ReturnType));

            AssertResponseCache(action, actionName, expected.HasNoStoreResponseCache);

            var actualParameters = action
                .GetParameters()
                .Select(parameter => new ParameterContract(
                    parameter.Name ?? "",
                    FormatType(parameter.ParameterType),
                    FormatBinding(parameter)))
                .ToArray();

            Assert.Equal(expected.Parameters, actualParameters);
        }
    }

    private static Type GetControllerType() =>
        typeof(Program).Assembly.GetType("PoolHub.API.Controllers.InvoicesController")
        ?? throw new InvalidOperationException("InvoicesController type was not found.");

    private static void AssertResponseCache(MethodInfo action, string actionName, bool expected)
    {
        var attribute = action.GetCustomAttribute<ResponseCacheAttribute>();

        if (!expected)
        {
            Assert.Null(attribute);
            return;
        }

        Assert.NotNull(attribute);
        Assert.True(attribute.NoStore, $"{actionName} must keep no-store response cache metadata.");
        Assert.Equal(ResponseCacheLocation.None, attribute.Location);
    }

    private static string FormatBinding(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
        {
            return "FromQuery";
        }

        if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
        {
            return "FromBody";
        }

        return "";
    }

    private static string FormatType(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var typeName = type.Name[..type.Name.IndexOf('`')];
        return $"{typeName}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }

    private sealed record ActionContract(
        string HttpMethod,
        string RouteTemplate,
        bool AllowAnonymous,
        bool HasNoStoreResponseCache,
        string ReturnType,
        ParameterContract[] Parameters);

    private sealed record ParameterContract(string Name, string Type, string Binding);
}
