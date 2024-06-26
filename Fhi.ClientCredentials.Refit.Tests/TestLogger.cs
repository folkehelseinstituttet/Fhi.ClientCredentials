﻿using Microsoft.Extensions.Logging;

namespace Fhi.ClientCredentials.Refit.Tests;

public class TestLogger<T> : ILogger<T>
{
    public List<string> Entries = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(logLevel.ToString() + " " + formatter(state, exception));
    }
}