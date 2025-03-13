/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

namespace Jitter2;

/// <summary>
/// Provides logging functionality with support for different log levels and listeners.
/// </summary>
public static class Logger
{
    private static readonly List<Action<LogLevel, string>> Listeners = new();

    /// <summary>
    /// Defines the severity levels for logging messages.
    /// </summary>
    public enum LogLevel
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// Registers a listener to receive log messages.
    /// </summary>
    /// <param name="listener">The action to invoke when a log message is generated.</param>
    public static void RegisterListener(Action<LogLevel, string> listener)
    {
        Listeners.Add(listener);
    }

    /// <summary>
    /// Unregisters a previously registered listener.
    /// </summary>
    /// <param name="listener">The action to remove from the listeners list.</param>
    public static void UnregisterListener(Action<LogLevel, string> listener)
    {
        Listeners.Remove(listener);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Information(scoped ReadOnlySpan<char> message) => Log(LogLevel.Information, message);

    public static void Information<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Information, format, arg1);

    public static void Information<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Information, format, arg1, arg2);

    public static void Information<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Information, format, arg1, arg2, arg3);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Warning(scoped ReadOnlySpan<char> message) => Log(LogLevel.Warning, message);

    public static void Warning<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Warning, format, arg1);

    public static void Warning<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Warning, format, arg1, arg2);

    public static void Warning<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Warning, format, arg1, arg2, arg3);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Error(scoped ReadOnlySpan<char> message) => Log(LogLevel.Error, message);

    public static void Error<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Error, format, arg1);

    public static void Error<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Error, format, arg1, arg2);

    public static void Error<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Error, format, arg1, arg2, arg3);

    /// <summary>
    /// Internal logging method that invokes all registered listeners with the given message.
    /// </summary>
    /// <param name="level">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    private static void Log(LogLevel level, scoped ReadOnlySpan<char> message)
    {
        if (Listeners.Count == 0) return;

        var messageString = message.ToString();
        foreach (var listener in Listeners)
        {
            listener(level, messageString);
        }
    }

    /// <summary>
    /// Formats a log message with one argument and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1)
    {
        if (Listeners.Count == 0) return;

        var message = string.Format(format.ToString(), arg1);
        foreach (var listener in Listeners)
        {
            listener(level, message);
        }
    }

    /// <summary>
    /// Formats a log message with two arguments and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1, T2>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2)
    {
        if (Listeners.Count == 0) return;

        var message = string.Format(format.ToString(), arg1, arg2);
        foreach (var listener in Listeners)
        {
            listener(level, message);
        }
    }

    /// <summary>
    /// Formats a log message with three arguments and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1, T2, T3>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3)
    {
        if (Listeners.Count == 0) return;

        var message = string.Format(format.ToString(), arg1, arg2, arg3);
        foreach (var listener in Listeners)
        {
            listener(level, message);
        }
    }
}
