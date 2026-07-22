using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace PoolHub.IntegrationTests;

public class BookingsControllerContractTests
{
    private static readonly IReadOnlyDictionary<string, ActionContract> ExpectedActions =
        new Dictionary<string, ActionContract>(StringComparer.Ordinal)
        {
            ["Get"] = new(
                "GET",
                "",
                "Authorize",
                "",
                "Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>>",
                new[]
                {
                    new ParameterContract("request", "BookingQueryRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetCalendar"] = new(
                "GET",
                "calendar",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<PagedResult<BookingCalendarItem>>>>",
                new[]
                {
                    new ParameterContract("request", "BookingCalendarRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetAvailability"] = new(
                "GET",
                "availability",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<List<AvailableTableDto>>>>",
                new[]
                {
                    new ParameterContract("request", "BookingAvailabilityRequest", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetPublicCalendar"] = new(
                "GET",
                "public/calendar",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<IEnumerable<PublicBookingSlotDto>>>>",
                new[]
                {
                    new ParameterContract("tableId", "Int64", "FromQuery"),
                    new ParameterContract("date", "DateTime", "FromQuery"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetById"] = new(
                "GET",
                "{id:long}",
                "Authorize",
                "",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["GetPublicBookingById"] = new(
                "GET",
                "public/{id:long}",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Create"] = new(
                "POST",
                "",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("request", "CreateBookingRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["CreatePublic"] = new(
                "POST",
                "public",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("request", "CreateBookingRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Update"] = new(
                "PUT",
                "{id:long}",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "UpdateBookingRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Confirm"] = new(
                "PUT",
                "{id:long}/confirm",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Approve"] = new(
                "POST",
                "{id:long}/approve",
                "Authorize",
                "Admin,Manager",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["SubmitDepositTransfer"] = new(
                "POST",
                "{id:long}/deposit/submit-transfer",
                "AllowAnonymous",
                "",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["ConfirmDeposit"] = new(
                "POST",
                "{id:long}/deposit/confirm",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "ConfirmDepositRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["RejectDepositTransfer"] = new(
                "POST",
                "{id:long}/deposit/reject",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "RejectDepositTransferRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["MockPayDeposit"] = new(
                "POST",
                "{id:long}/deposit/mock-pay",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Cancel"] = new(
                "PUT",
                "{id:long}/cancel",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "CancelBookingRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["NoShow"] = new(
                "PUT",
                "{id:long}/no-show",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "NoShowBookingRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Complete"] = new(
                "PUT",
                "{id:long}/complete",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<BookingDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["StartSession"] = new(
                "POST",
                "{id:long}/start-session",
                "Authorize",
                "Admin,Manager,Staff,Cashier",
                "Task<ActionResult<ApiResponse<SessionDto>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("request", "StartSessionRequest", "FromBody"),
                    new ParameterContract("ct", "CancellationToken", "")
                }),
            ["Delete"] = new(
                "DELETE",
                "{id:long}",
                "Authorize",
                "Admin,Manager",
                "Task<ActionResult<ApiResponse<Object>>>",
                new[]
                {
                    new ParameterContract("id", "Int64", ""),
                    new ParameterContract("ct", "CancellationToken", "")
                })
        };

    [Fact]
    public void BookingsController_ClassRoute_IsStable()
    {
        var controller = GetControllerType();

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/bookings", controller.GetCustomAttribute<RouteAttribute>()?.Template);
    }

    [Fact]
    public void BookingsController_ActionContracts_AreStable()
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
            var allowAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>() is not null;

            Assert.Equal(expected.HttpMethod, Assert.Single(http.HttpMethods));
            Assert.Equal(expected.RouteTemplate, http.Template ?? "");
            Assert.Equal(expected.ReturnType, FormatType(action.ReturnType));

            if (expected.Authorization == "AllowAnonymous")
            {
                Assert.True(allowAnonymous, $"{actionName} must allow anonymous access.");
                Assert.Empty(authorize);
            }
            else
            {
                Assert.False(allowAnonymous, $"{actionName} must not allow anonymous access.");
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
        typeof(Program).Assembly.GetType("PoolHub.API.Controllers.BookingsController")
        ?? throw new InvalidOperationException("BookingsController type was not found.");

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
        string Authorization,
        string Roles,
        string ReturnType,
        ParameterContract[] Parameters);

    private sealed record ParameterContract(string Name, string Type, string Binding);
}
