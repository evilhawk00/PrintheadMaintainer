/*
*
* Copyright (C) 2026  YAN-LIN, CHEN
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
*
*/
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.Models;
using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace PrintheadMaintainerUI.NamedPipeClient
{
    /// <summary>
    /// Sends requests to the service. The commands and their answers are described in the
    /// service's Ipc/RequestHandler.h. Every request uses its own connection. Communication
    /// problems never throw: when the service cannot be reached or does not answer in time,
    /// the result says so.
    /// </summary>
    public sealed class ServiceClient
    {
        private const string PipeName = "Printhead Maintainer";

        // Enough to read, write and switch the pipe to message mode, and nothing more. The
        // service only learns who is connecting (identification level); it cannot act as the user.
        private const PipeAccessRights PipeAccess = PipeAccessRights.ReadData | PipeAccessRights.WriteData |
            PipeAccessRights.ReadAttributes | PipeAccessRights.WriteAttributes;

        private const int MaxResponseBytes = 1024 * 1024;
        private const int MaxImageSourceNameLength = 255;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ImageRequestTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ConnectRetryDelay = TimeSpan.FromMilliseconds(100);

        private const string ResultOk = "OK";

        /// <summary>Returns null if the service cannot be reached or gives an unexpected answer.</summary>
        public Task<ServiceStatus> GetStatusAsync()
        {
            return Task.Run(async () =>
            {
                ServiceMessage response = await SendAsync(new ServiceMessage("GetStatus"), RequestTimeout).ConfigureAwait(false);
                return response != null && response.Name == ResultOk ? ParseStatus(response) : null;
            });
        }

        public Task<ApplySettingsResult> ApplySettingsAsync(SettingsUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            return Task.Run(async () =>
            {
                ServiceMessage request = CreateApplySettingsRequest(update);
                if (request == null)
                {
                    return ApplySettingsResult.InvalidValue;
                }

                TimeSpan timeout = request.Data.Length > 0 ? ImageRequestTimeout : RequestTimeout;
                ServiceMessage response = await SendAsync(request, timeout).ConfigureAwait(false);
                if (response == null)
                {
                    return ApplySettingsResult.NotConnected;
                }
                switch (response.Name)
                {
                    case ResultOk:
                        return ApplySettingsResult.Applied;
                    case "InvalidValue":
                        return ApplySettingsResult.InvalidValue;
                    case "PrinterUnavailable":
                        return ApplySettingsResult.PrinterUnavailable;
                    case "StorageError":
                        return ApplySettingsResult.StorageError;
                    default:
                        return ApplySettingsResult.Failed;
                }
            });
        }

        public Task<PrintNowResult> PrintNowAsync()
        {
            return Task.Run(async () =>
            {
                ServiceMessage response = await SendAsync(new ServiceMessage("PrintNow"), RequestTimeout).ConfigureAwait(false);
                if (response == null)
                {
                    return PrintNowResult.NotConnected;
                }
                switch (response.Name)
                {
                    case ResultOk:
                        return PrintNowResult.Accepted;
                    case "Busy":
                        return PrintNowResult.Busy;
                    case "NotConfigured":
                        return PrintNowResult.NotConfigured;
                    default:
                        return PrintNowResult.Failed;
                }
            });
        }

        /// <summary>Returns null if a value cannot be sent (it contains control characters).</summary>
        private static ServiceMessage CreateApplySettingsRequest(SettingsUpdate update)
        {
            var request = new ServiceMessage("ApplySettings");
            if (update.Enabled.HasValue)
            {
                request.Add("Enabled", update.Enabled.Value ? "1" : "0");
            }
            if (update.IntervalDays.HasValue)
            {
                request.Add("IntervalDays", update.IntervalDays.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (update.PrinterName != null)
            {
                if (ServiceMessage.ContainsControlCharacters(update.PrinterName))
                {
                    return null;
                }
                request.Add("PrinterName", update.PrinterName);
            }
            if (update.PaperSource.HasValue)
            {
                request.Add("PaperSource", update.PaperSource.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (update.Image != null)
            {
                PrintImage image = update.Image.CustomImage;
                if (image == null)
                {
                    request.Add("Image", "Default");
                }
                else
                {
                    request.Add("Image", "Custom");
                    request.Add("ImageWidth", image.Width.ToString(CultureInfo.InvariantCulture));
                    request.Add("ImageHeight", image.Height.ToString(CultureInfo.InvariantCulture));
                    request.Add("ImageSourceName", ToImageSourceName(image.SourceName));
                    request.Data = image.Pixels;
                }
            }
            return request;
        }

        /// <summary>
        /// The name is only shown to the user, so characters that cannot be sent are replaced and a
        /// name longer than the service accepts (Settings/ServiceSettings.h) is shortened.
        /// </summary>
        private static string ToImageSourceName(string fileName)
        {
            string name = ServiceMessage.ReplaceControlCharacters(fileName);
            if (name.Length <= MaxImageSourceNameLength)
            {
                return name;
            }
            int length = MaxImageSourceNameLength;
            if (char.IsHighSurrogate(name[length - 1]))
            {
                length--; // do not cut a character in half
            }
            return name.Substring(0, length);
        }

        private static ServiceStatus ParseStatus(ServiceMessage response)
        {
            try
            {
                return new ServiceStatus
                {
                    PrintState = ParsePrintState(Required(response, "PrintState")),
                    ManualPrintPending = ParseFlag(Required(response, "ManualPrintPending")),
                    Enabled = ParseFlag(Required(response, "Enabled")),
                    IntervalDays = ParseInt(Required(response, "IntervalDays")),
                    PrinterName = Required(response, "PrinterName"),
                    PaperSource = ParseInt(Required(response, "PaperSource")),
                    CustomImage = ParseFlag(Required(response, "CustomImage")),
                    ImageSourceName = Required(response, "ImageSourceName"),
                    ImagePath = Required(response, "ImagePath"),
                    ImageAvailable = ParseFlag(Required(response, "ImageAvailable")),
                    LastPrintUtc = ParseTime(Required(response, "LastPrint")),
                    LastScheduledFailure = ParseFailure(response, "LastScheduledFailure"),
                    LastManualFailure = ParseFailure(response, "LastManualFailure"),
                    NextScheduledPrintUtc = ParseTime(Required(response, "NextScheduledPrint")),
                };
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string Required(ServiceMessage response, string key)
        {
            return response.Get(key) ?? throw new FormatException("The status has no " + key + " field.");
        }

        private static bool ParseFlag(string text)
        {
            switch (text)
            {
                case "1":
                    return true;
                case "0":
                    return false;
                default:
                    throw new FormatException("Invalid flag.");
            }
        }

        private static int ParseInt(string text)
        {
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                throw new FormatException("Invalid number.");
            }
            return value;
        }

        private static PrintState ParsePrintState(string text)
        {
            switch (text)
            {
                case "Countdown":
                    return PrintState.Countdown;
                case "Printing":
                    return PrintState.Printing;
                default:
                    return PrintState.Idle;
            }
        }

        /// <summary>Times are FILETIME ticks in UTC, where 0 means "never".</summary>
        private static DateTime? ParseTime(string text)
        {
            if (!ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong ticks))
            {
                throw new FormatException("Invalid time.");
            }
            if (ticks == 0 || ticks > (ulong)DateTime.MaxValue.ToFileTimeUtc())
            {
                return null;
            }
            return DateTime.FromFileTimeUtc((long)ticks);
        }

        private static PrintFailure ParseFailure(ServiceMessage response, string timeKey)
        {
            DateTime? time = ParseTime(Required(response, timeKey));
            string reasonName = Required(response, timeKey + "Reason");
            if (!time.HasValue)
            {
                return null;
            }

            // Names this version does not know count as a general printer error.
            FailureReason reason = Enum.IsDefined(typeof(FailureReason), reasonName)
                ? (FailureReason)Enum.Parse(typeof(FailureReason), reasonName)
                : FailureReason.PrinterError;
            return new PrintFailure(time.Value, reason);
        }

        /// <summary>Returns the response, or null if the exchange failed or timed out.</summary>
        private static async Task<ServiceMessage> SendAsync(ServiceMessage request, TimeSpan timeout)
        {
            byte[] requestBytes = request.Serialize();
            using (var cancellation = new CancellationTokenSource())
            using (var pipe = new NamedPipeClientStream(".", PipeName, PipeAccess, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Identification, HandleInheritability.None))
            {
                Task<byte[]> exchange = ExchangeAsync(pipe, requestBytes, cancellation.Token);
                Task finished = await Task.WhenAny(exchange, Task.Delay(timeout, cancellation.Token)).ConfigureAwait(false);
                cancellation.Cancel();
                if (finished != exchange)
                {
                    // Disposing the pipe aborts the pending read or write; the exchange then fails,
                    // and its exception is observed here so that it is not reported as unhandled.
                    _ = exchange.ContinueWith(t => t.Exception, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    return null;
                }

                try
                {
                    return ServiceMessage.Parse(await exchange.ConfigureAwait(false));
                }
                catch (Exception e) when (IsCommunicationError(e))
                {
                    return null;
                }
            }
        }

        private static async Task<byte[]> ExchangeAsync(NamedPipeClientStream pipe, byte[] request, CancellationToken cancellation)
        {
            await ConnectAsync(pipe, cancellation).ConfigureAwait(false);
            pipe.ReadMode = PipeTransmissionMode.Message;
            await pipe.WriteAsync(request, 0, request.Length).ConfigureAwait(false);

            var buffer = new byte[16 * 1024];
            using (var response = new MemoryStream())
            {
                do
                {
                    int read = await pipe.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new EndOfStreamException("The service closed the connection.");
                    }
                    if (response.Length + read > MaxResponseBytes)
                    {
                        throw new IOException("The response is too large.");
                    }
                    response.Write(buffer, 0, read);
                }
                while (!pipe.IsMessageComplete);
                return response.ToArray();
            }
        }

        // NamedPipeClientStream.Connect(timeout) busy-waits while the pipe does not exist (the
        // service is not running), so each attempt is made with a zero timeout, which waits at most
        // the pipe's default 50 ms when the service is busy with another client.
        private static async Task ConnectAsync(NamedPipeClientStream pipe, CancellationToken cancellation)
        {
            DateTime deadline = DateTime.UtcNow + ConnectTimeout;
            for (;;)
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    pipe.Connect(0);
                    cancellation.ThrowIfCancellationRequested();
                    return;
                }
                catch (Exception e) when ((e is TimeoutException || e is IOException) && DateTime.UtcNow < deadline)
                {
                    // Not running yet, or serving another client.
                }
                await Task.Delay(ConnectRetryDelay, cancellation).ConfigureAwait(false);
            }
        }

        private static bool IsCommunicationError(Exception e)
        {
            return e is IOException || e is TimeoutException || e is OperationCanceledException ||
                e is UnauthorizedAccessException || e is ObjectDisposedException || e is InvalidOperationException;
        }
    }
}
