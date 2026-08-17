using FluentValidation.Results;

namespace Fulfillment.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures) : base(
        "One or more validation failures have occurred")
    {
        Errors = failures
            .GroupBy(f=>f.PropertyName, f=>f.ErrorMessage)
            .ToDictionary(g=>g.Key, g=>g.ToArray());
    }
    
    public IDictionary<string, string[]> Errors { get; }
}