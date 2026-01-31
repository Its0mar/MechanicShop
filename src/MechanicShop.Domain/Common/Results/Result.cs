using System.ComponentModel;
using System.Text.Json.Serialization;
using MechanicShop.Domain.Common.Results.Abstractions;

namespace MechanicShop.Domain.Common.Results;

public static class Result
{
    public static Success Success => default;
    public static Created Created => default;
    public static Deleted Deleted => default;
    public static Updated Updated => default;
} 

public sealed class Result<TValue> : IResult<TValue>
{
    private readonly TValue? _value = default;
    private readonly List<Error>? _errors = null;
    public bool IsSuccess {get;}
    public bool IsError => !IsSuccess;
    public TValue Value => IsSuccess ? _value! : default!;
    public List<Error>? Errors => IsError ? _errors : [];
    public Error TopError => _errors?.Count() > 0 ? _errors[0] : default;

    public TNextValue Match<TNextValue>(
    Func<TValue, TNextValue> onValue,
    Func<List<Error>, TNextValue> onError)
    => IsSuccess ? onValue(Value!) : onError(Errors!);

    public static implicit operator Result<TValue>(TValue value)
        => new(value);

    public static implicit operator Result<TValue>(Error error)
        => new(error);

    public static implicit operator Result<TValue>(List<Error> errors)
        => new(errors);

    [JsonConstructor]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("For serializer only.", true)]
    public Result(TValue? value, List<Error>? errors, bool isSuccess)
    {
        if (isSuccess)
        {
            _value = value ?? throw new ArgumentException(nameof(value));
            _errors = [];
            IsSuccess = true;
        }
        else
        {
            if (errors == null || errors.Count() == 0)
            {
                throw new ArgumentException("Provide at least one error.", nameof(errors));
            }

            _errors = errors;
            _value = default;
            IsSuccess = false;
        }
    }

    private Result(Error error)
    {
        _errors = [error];
        IsSuccess = false;
    }

    private Result(List<Error> errors)
    {
        if (errors is null || errors.Count == 0)
        {
            throw new ArgumentException("Cannot create an ErrorOr<TValue> from an empty collection of errors. Provide at least one error.", nameof(errors));
        }

        _errors = errors;
        IsSuccess = false;
    }

    private Result(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));            
        }

        _value = value;
        IsSuccess = true;
    }


}

public readonly record struct Success;
public readonly record struct Created;
public readonly record struct Deleted;
public readonly record struct Updated;