using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fulfillment.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ErrorMessageOptions _messages;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<ErrorMessageOptions> messages)
    {
        _next = next;
        _logger = logger;
        _messages = messages.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = BuildProblem(exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
        else if (exception is DbUpdateException)
            _logger.LogWarning(exception, "Database conflict on {Path}", context.Request.Path);

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status!.Value;

        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private ProblemDetails BuildProblem(Exception exception) => exception switch
    {
        ValidationException ve => Problem<ValidationProblemDetails>(
            new ValidationProblemDetails(ve.Errors),
            StatusCodes.Status400BadRequest,
            "Validation"),

        NotFoundException nfe => Problem(
            StatusCodes.Status404NotFound, "NotFound", nfe.Message),

        ConflictException ce => Problem(
            StatusCodes.Status409Conflict, "Conflict", ce.Message),

        BusinessRuleViolationException bre => Problem(
            StatusCodes.Status422UnprocessableEntity, "BusinessRuleViolation", bre.Message),
        
        InvalidOrderStateException iose => Problem(
            StatusCodes.Status422UnprocessableEntity, "InvalidOrderState", iose.Message),
        
        DbUpdateConcurrencyException => Problem(
            StatusCodes.Status409Conflict, "ConcurrencyConflict"),

        DbUpdateException due when IsDuplicateKey(due) => Problem(
            StatusCodes.Status409Conflict, "DuplicateRecord"),

        UnauthorizedAccessException => Problem(
            StatusCodes.Status401Unauthorized, "Unauthorized"),

        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected")
    };

    private ProblemDetails Problem(int status, string key, string? detail = null)
    {
        var message = _messages.Get(key);

        return new ProblemDetails
        {
            Status = status,
            Title = message.Title,
            Detail = detail ?? message.Detail
        };
    }

    private T Problem<T>(T problem, int status, string key) where T : ProblemDetails
    {
        var message = _messages.Get(key);
        problem.Status = status;
        problem.Title = message.Title;
        problem.Detail ??= message.Detail;
        return problem;
    }

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is SqlException sql &&
        sql.Number is UniqueIndexViolation or UniqueConstraintViolation;
}