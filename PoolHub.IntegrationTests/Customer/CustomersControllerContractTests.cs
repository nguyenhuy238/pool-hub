using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace PoolHub.IntegrationTests;

public class CustomersControllerContractTests
{
    private static readonly IReadOnlyDictionary<string, ActionContract> ExpectedActions =
        new Dictionary<string, ActionContract>(StringComparer.Ordinal)
        {
            ["GetCustomers"] = new(
                "GET",
                "",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("request", "CustomerQueryRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetCustomer"] = new(
                "GET",
                "{id:long}",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["CreateCustomer"] = new(
                "POST",
                "",
                "Admin,Manager",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("request", "CreateCustomerRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["UpdateCustomer"] = new(
                "PUT",
                "{id:long}",
                "Admin,Manager",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "UpdateCustomerRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["UpdateStatus"] = new(
                "PATCH",
                "{id:long}/status",
                "Admin,Manager",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "UpdateCustomerStatusRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["SoftDelete"] = new(
                "DELETE",
                "{id:long}",
                "Admin",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Bookings"] = new(
                "GET",
                "{id:long}/bookings",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "PaginationRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Sessions"] = new(
                "GET",
                "{id:long}/sessions",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "PaginationRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Invoices"] = new(
                "GET",
                "{id:long}/invoices",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "PaginationRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["ExchangeVoucher"] = new(
                "POST",
                "{id:long}/exchange-voucher/{templateId:long}",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("templateId", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["PointHistory"] = new(
                "GET",
                "{id:long}/point-history",
                "",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "PaginationRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                })
        };

    [Fact]
    public void CustomersController_ClassContract_IsStable()
    {
        var controller = GetControllerType();
        var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/customers", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.NotNull(authorize);
        Assert.Equal("Admin,Manager,Staff", authorize.Roles);
        Assert.Equal("customers.manage", authorize.Policy);
        Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void CustomersController_ActionContracts_AreStable()
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
            var authorize = action.GetCustomAttributes<AuthorizeAttribute>().ToArray();

            Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.Equal(expected.HttpMethod, Assert.Single(http.HttpMethods));
            Assert.Equal(expected.RouteTemplate, http.Template ?? "");
            Assert.Equal(expected.ReturnType, FormatType(action.ReturnType));

            if (string.IsNullOrEmpty(expected.Roles) && string.IsNullOrEmpty(expected.Policy))
            {
                Assert.Empty(authorize);
            }
            else
            {
                var attribute = Assert.Single(authorize);
                Assert.Equal(expected.Roles, attribute.Roles ?? "");
                Assert.Equal(expected.Policy, attribute.Policy ?? "");
            }

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
        typeof(Program).Assembly.GetType("PoolHub.API.Controllers.CustomersController")
        ?? throw new InvalidOperationException("CustomersController type was not found.");

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
        string Roles,
        string Policy,
        string ReturnType,
        ParameterContract[] Parameters);

    private sealed record ParameterContract(string Name, string Type, string Binding);
}
