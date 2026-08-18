using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    public async Task HandleAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            throw exception;
            
        var problem = exception switch
        {
            ValidationException ve => (ProblemDetails)new ValidationProblemDetails(ve.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed."
            },

            NotFoundException nfe => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = nfe.Message,
            },

            ConflictException ce => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = ce.Message,
            },

            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict.",
                Detail = "This stock record was recorded by another user. Reload and try again.",
            },

            DbUpdateException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = "The operation conflicts with another existing data.",
            },

            BusinessRuleViolationException bre => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business Rule violated.",
                Detail = bre.Message,
            },

            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized.",
                Detail = "Authentication is required.",
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please contact support if the problem persists.",
            }
        };
        
        if(problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsJsonAsync(problem);
    }
}