using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace PoolHub.IntegrationTests;

public class SessionsControllerContractTests
{
    private static readonly IReadOnlyDictionary<string, ActionContract> ExpectedActions =
        new Dictionary<string, ActionContract>(StringComparer.Ordinal)
        {
            ["GetSessions"] = new(
                "GET",
                "",
                "",
                "Task<ActionResult<ApiResponse<PagedResult<SessionDto>>>>",
                new[]
                {
                    new ParameterContract("request", "SessionQueryRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetActiveSessions"] = new(
                "GET",
                "active",
                "",
                "Task<ActionResult<ApiResponse<List<ActiveSessionResponse>>>>",
                new[]
                {
                    new ParameterContract("floorId", "Nullable<Int64>", "FromQuery"),
                    new ParameterContract("zoneId", "Nullable<Int64>", "FromQuery"),
                    new ParameterContract("tableId", "Nullable<Int64>", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetActiveSessionByTable"] = new(
                "GET",
                "by-table/{tableId:long}",
                "",
                "Task<ActionResult<ApiResponse<SessionDetailDto>>>",
                new[]
                {
                    new ParameterContract("tableId", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetSessionById"] = new(
                "GET",
                "{id:long}",
                "",
                "Task<ActionResult<ApiResponse<SessionDetailDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetSummary"] = new(
                "GET",
                "{id:long}/summary",
                "",
                "Task<ActionResult<ApiResponse<SessionSummaryResponse>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetTimeCharges"] = new(
                "GET",
                "{id:long}/time-charges",
                "",
                "Task<ActionResult<ApiResponse<SessionTimeChargesResponse>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Start"] = new(
                "POST",
                "start",
                "",
                "Task<ActionResult<ApiResponse<SessionDto>>>",
                new[]
                {
                    new ParameterContract("request", "StartSessionRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["CloseWithSummary"] = new(
                "POST",
                "{id:long}/close",
                "Admin,Manager,Staff",
                "Task<ActionResult<ApiResponse<CloseSessionResponse>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "CloseSessionRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["ReleaseTables"] = new(
                "POST",
                "{id:long}/release-tables",
                "Admin,Manager,Staff",
                "Task<ActionResult<ApiResponse<ReleaseSessionTablesResponse>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "ReleaseSessionTablesRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Cancel"] = new(
                "POST",
                "{id:long}/cancel",
                "Admin,Manager",
                "Task<ActionResult<ApiResponse<SessionDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "CancelSessionRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Transfer"] = new(
                "POST",
                "{id:long}/transfer",
                "",
                "Task<ActionResult<ApiResponse<TransferTableResponse>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "TransferTableRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Reopen"] = new(
                "POST",
                "{id:long}/reopen",
                "Admin,Manager,Staff",
                "Task<ActionResult<ApiResponse<SessionDetailDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "ReopenSessionRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                })
        };

    [Fact]
    public void SessionsController_ClassContract_IsStable()
    {
        var controller = GetControllerType();
        var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/sessions", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.NotNull(authorize);
        Assert.Equal("Admin,Manager,Staff", authorize.Roles);
        Assert.True(string.IsNullOrEmpty(authorize.Policy));
        Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void SessionsController_ActionContracts_AreStable()
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

            if (string.IsNullOrEmpty(expected.Roles))
            {
                Assert.Empty(authorize);
            }
            else
            {
                var attribute = Assert.Single(authorize);
                Assert.Equal(expected.Roles, attribute.Roles ?? "");
                Assert.True(string.IsNullOrEmpty(attribute.Policy), $"{actionName} must not add or change authorization policy.");
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
        typeof(Program).Assembly.GetType("PoolHub.API.Controllers.SessionsController")
        ?? throw new InvalidOperationException("SessionsController type was not found.");

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
        string ReturnType,
        ParameterContract[] Parameters);

    private sealed record ParameterContract(string Name, string Type, string Binding);
}
