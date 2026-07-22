using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/bookings")]
public partial class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ISessionService _sessionService;
    private readonly ICrudService _crud;

    public BookingsController(IBookingService bookingService, ISessionService sessionService, ICrudService crud)
    {
        _bookingService = bookingService;
        _sessionService = sessionService;
        _crud = crud;
    }
}
