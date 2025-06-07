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

namespace Jitter2;

/// <summary>
/// Provides logging functionality.
/// </summary>
public static class Logger
{

    /// <summary>
    /// Gets or sets a listener which receives log messages.
    /// </summary>
    public static Action<LogLevel, string>? Listener { get; set; }

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
    /// Logs an informational message.
    /// </summary>
    /// <param name="format">The message to log.</param>
    public static void Information(scoped ReadOnlySpan<char> format) => Log(LogLevel.Information, format);

    /// <inheritdoc cref="Information"/>
    public static void Information<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Information, format, arg1);

    /// <inheritdoc cref="Information"/>
    public static void Information<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Information, format, arg1, arg2);

    /// <inheritdoc cref="Information"/>
    public static void Information<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Information, format, arg1, arg2, arg3);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="format">The message to log.</param>
    public static void Warning(scoped ReadOnlySpan<char> format) => Log(LogLevel.Warning, format);

    /// <inheritdoc cref="Warning"/>
    public static void Warning<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Warning, format, arg1);

    /// <inheritdoc cref="Warning"/>
    public static void Warning<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Warning, format, arg1, arg2);

    /// <inheritdoc cref="Warning"/>
    public static void Warning<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Warning, format, arg1, arg2, arg3);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="format">The message to log.</param>
    public static void Error(scoped ReadOnlySpan<char> format) => Log(LogLevel.Error, format);

    /// <inheritdoc cref="Error"/>
    public static void Error<T1>(scoped ReadOnlySpan<char> format, T1 arg1) => LogFormat(LogLevel.Error, format, arg1);

    /// <inheritdoc cref="Error"/>
    public static void Error<T1, T2>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2) => LogFormat(LogLevel.Error, format, arg1, arg2);

    /// <inheritdoc cref="Error"/>
    public static void Error<T1, T2, T3>(scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3) => LogFormat(LogLevel.Error, format, arg1, arg2, arg3);

    /// <summary>
    /// Internal logging method that invokes all registered listeners with the given message.
    /// </summary>
    /// <param name="level">The log level of the message.</param>
    /// <param name="format">The message to log.</param>
    private static void Log(LogLevel level, scoped ReadOnlySpan<char> format)
    {
        Listener?.Invoke(level, format.ToString());
    }

    /// <summary>
    /// Formats a log message with one argument and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1)
    {
        Listener?.Invoke(level, string.Format(format.ToString(), arg1));
    }

    /// <summary>
    /// Formats a log message with two arguments and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1, T2>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2)
    {
        Listener?.Invoke(level, string.Format(format.ToString(), arg1, arg2));
    }

    /// <summary>
    /// Formats a log message with three arguments and invokes the listeners.
    /// </summary>
    private static void LogFormat<T1, T2, T3>(LogLevel level, scoped ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3)
    {
        Listener?.Invoke(level, string.Format(format.ToString(), arg1, arg2, arg3));
    }
}
